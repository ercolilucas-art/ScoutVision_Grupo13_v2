
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScoutVision.Web.Data;
using ScoutVision.Web.Models;
using System.Text;

namespace ScoutVision.Web.Controllers;

public class ReportesController : Controller
{
    private readonly Database _database;

    public ReportesController(Database database)
    {
        _database = database;
    }

    // ==========================================================
    // DASHBOARD DE REPORTES
    // ==========================================================

    [HttpGet]
    public async Task<IActionResult> Index(int equipoId = 0)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        var model = new ReportesIndexViewModel
        {
            EquipoId = equipoId
        };

        await using var connection =
            await _database.OpenConnectionAsync();

        // 1. Equipos disponibles para el filtro.

        const string equiposSql = """
            SELECT id, name
            FROM team
            ORDER BY name;
            """;

        await using (var cmd =
            new NpgsqlCommand(equiposSql, connection))
        await using (var reader =
            await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                model.Equipos.Add(new EquipoFiltroViewModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1)
                });
            }
        }

        // 2. Indicadores generales.
        // Los partidos se limitan a 2026.
        // Las participaciones respetan el equipo elegido.

        const string resumenSql = """
            SELECT
                (
                    SELECT COUNT(*)::integer
                    FROM team t
                    WHERE (@equipo_id = 0 OR t.id = @equipo_id)
                ) AS equipos,

                (
                    SELECT COUNT(*)::integer
                    FROM player p
                    WHERE (@equipo_id = 0 OR p.team_id = @equipo_id)
                ) AS jugadores,

                (
                    SELECT COUNT(*)::integer
                    FROM football_match fm
                    WHERE fm.match_date >= DATE '2026-01-01'
                      AND fm.match_date < DATE '2027-01-01'
                      AND (
                          @equipo_id = 0
                          OR fm.home_team_id = @equipo_id
                          OR fm.away_team_id = @equipo_id
                      )
                ) AS partidos,

                (
                    SELECT COUNT(*)::integer
                    FROM player_match_stats pms
                    INNER JOIN player p
                        ON p.id = pms.player_id
                    INNER JOIN football_match fm
                        ON fm.id = pms.match_id
                    WHERE fm.match_date >= DATE '2026-01-01'
                      AND fm.match_date < DATE '2027-01-01'
                      AND (
                          @equipo_id = 0
                          OR p.team_id = @equipo_id
                      )
                ) AS participaciones;
            """;

        await using (var cmd =
            new NpgsqlCommand(resumenSql, connection))
        {
            cmd.Parameters.AddWithValue("equipo_id", equipoId);

            await using var reader =
                await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                model.TotalEquipos = reader.GetInt32(0);
                model.TotalJugadores = reader.GetInt32(1);
                model.TotalPartidos = reader.GetInt32(2);
                model.TotalParticipaciones = reader.GetInt32(3);
            }
        }

        // 3. Ranking de jugadores por rendimiento.

        const string rankingSql = """
            SELECT
                p.id,
                p.full_name,
                t.name,
                p.position,
                COALESCE(r.goals, 0),
                COALESCE(r.assists, 0),
                COALESCE(r.minutes_played, 0),
                r.performance_index,
                COALESCE(pm.partidos, 0)
            FROM player p
            INNER JOIN team t
                ON t.id = p.team_id
            LEFT JOIN player_performance r
                ON r.player_id = p.id
                AND r.season = 2026
            LEFT JOIN (
                SELECT
                    pms.player_id,
                    COUNT(DISTINCT pms.match_id)::integer AS partidos
                FROM player_match_stats pms
                INNER JOIN football_match fm
                    ON fm.id = pms.match_id
                WHERE fm.match_date >= DATE '2026-01-01'
                  AND fm.match_date < DATE '2027-01-01'
                GROUP BY pms.player_id
            ) pm
                ON pm.player_id = p.id
            WHERE (@equipo_id = 0 OR t.id = @equipo_id)
            ORDER BY
                r.performance_index DESC NULLS LAST,
                p.full_name
            LIMIT 10;
            """;

        await using (var cmd =
            new NpgsqlCommand(rankingSql, connection))
        {
            cmd.Parameters.AddWithValue("equipo_id", equipoId);

            await using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                model.Ranking.Add(new ReporteJugadorViewModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Equipo = reader.GetString(2),
                    Posicion = reader.GetString(3),
                    Goles = reader.GetInt32(4),
                    Asistencias = reader.GetInt32(5),
                    Minutos = reader.GetInt32(6),

                    Indice = reader.IsDBNull(7)
                        ? null
                        : reader.GetDecimal(7),

                    Partidos = reader.GetInt32(8)
                });
            }
        }

        // 4. Rendimiento promedio por equipo.

        const string rendimientoSql = """
            SELECT
                t.id,
                t.name,
                COUNT(DISTINCT p.id)::integer,
                COALESCE(SUM(r.goals), 0)::integer,
                ROUND(AVG(r.performance_index), 2)
            FROM team t
            LEFT JOIN player p
                ON p.team_id = t.id
            LEFT JOIN player_performance r
                ON r.player_id = p.id
                AND r.season = 2026
            WHERE (@equipo_id = 0 OR t.id = @equipo_id)
            GROUP BY t.id, t.name
            ORDER BY
                AVG(r.performance_index) DESC NULLS LAST,
                t.name;
            """;

        await using (var cmd =
            new NpgsqlCommand(rendimientoSql, connection))
        {
            cmd.Parameters.AddWithValue("equipo_id", equipoId);

            await using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                model.RendimientoEquipos.Add(
                    new ReporteEquipoViewModel
                    {
                        Id = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Jugadores = reader.GetInt32(2),
                        Goles = reader.GetInt32(3),

                        IndicePromedio = reader.IsDBNull(4)
                            ? null
                            : reader.GetDecimal(4)
                    });
            }
        }

        // 5. Cantidad de partidos registrados por mes.

        const string actividadSql = """
            SELECT
                TO_CHAR(fm.match_date, 'YYYY-MM') AS periodo,
                COUNT(*)::integer AS cantidad
            FROM football_match fm
            WHERE fm.match_date >= DATE '2026-01-01'
              AND fm.match_date < DATE '2027-01-01'
              AND (
                  @equipo_id = 0
                  OR fm.home_team_id = @equipo_id
                  OR fm.away_team_id = @equipo_id
              )
            GROUP BY TO_CHAR(fm.match_date, 'YYYY-MM')
            ORDER BY periodo;
            """;

        await using (var cmd =
            new NpgsqlCommand(actividadSql, connection))
        {
            cmd.Parameters.AddWithValue("equipo_id", equipoId);

            await using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                model.ActividadMensual.Add(
                    new ReporteMesViewModel
                    {
                        Periodo = reader.GetString(0),
                        CantidadPartidos = reader.GetInt32(1)
                    });
            }
        }

        return View(model);
    }

    // ==========================================================
    // EXPORTACIÓN DEL RANKING A CSV
    // ==========================================================

    [HttpGet]
    public async Task<IActionResult> ExportarCsv(int equipoId = 0)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        await using var connection =
            await _database.OpenConnectionAsync();

        const string sql = """
            SELECT
                p.full_name,
                t.name,
                p.position,
                COALESCE(r.goals, 0),
                COALESCE(r.assists, 0),
                COALESCE(r.minutes_played, 0),
                r.performance_index
            FROM player p
            INNER JOIN team t
                ON t.id = p.team_id
            LEFT JOIN player_performance r
                ON r.player_id = p.id
                AND r.season = 2026
            WHERE (@equipo_id = 0 OR t.id = @equipo_id)
            ORDER BY
                r.performance_index DESC NULLS LAST,
                p.full_name;
            """;

        var csv = new StringBuilder();

        csv.AppendLine(
            "Jugador;Equipo;Posicion;Goles;Asistencias;Minutos;Indice");

        await using var cmd =
            new NpgsqlCommand(sql, connection);

        cmd.Parameters.AddWithValue("equipo_id", equipoId);

        await using var reader =
            await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            string nombre = CampoCsv(reader.GetString(0));
            string equipo = CampoCsv(reader.GetString(1));
            string posicion = CampoCsv(reader.GetString(2));

            string goles = reader.GetInt32(3).ToString();
            string asistencias = reader.GetInt32(4).ToString();
            string minutos = reader.GetInt32(5).ToString();

            string indice = reader.IsDBNull(6)
                ? ""
                : reader.GetDecimal(6).ToString(
                    "0.00",
                    System.Globalization.CultureInfo.GetCultureInfo("es-AR"));

            csv.AppendLine(
                $"{nombre};{equipo};{posicion};" +
                $"{goles};{asistencias};{minutos};{indice}");
        }

        // UTF-8 con BOM para facilitar la apertura en Excel.
        var encoding = new UTF8Encoding(true);

        byte[] contenido = encoding.GetPreamble()
            .Concat(encoding.GetBytes(csv.ToString()))
            .ToArray();

        return File(
            contenido,
            "text/csv; charset=utf-8",
            "ScoutVision_Reporte_2026.csv");
    }

    private static string CampoCsv(string valor)
    {
        // Evita que valores de texto se interpreten como fórmulas.
        if (!string.IsNullOrEmpty(valor) &&
            (valor[0] == '=' ||
             valor[0] == '+' ||
             valor[0] == '-' ||
             valor[0] == '@'))
        {
            valor = "'" + valor;
        }

        return "\"" + valor.Replace("\"", "\"\"") + "\"";
    }
}
