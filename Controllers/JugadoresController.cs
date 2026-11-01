
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScoutVision.Web.Data;
using ScoutVision.Web.Models;
using System.Globalization;

namespace ScoutVision.Web.Controllers;

public class JugadoresController : Controller
{
    private readonly Database _database;

    public JugadoresController(Database database)
    {
        _database = database;
    }

    // ============================================================
    // NIVEL 1 - LISTADO GENERAL DE JUGADORES
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? busqueda,
        int equipoId = 0)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        var model = new JugadoresIndexViewModel
        {
            Busqueda = busqueda?.Trim() ?? string.Empty,
            EquipoId = equipoId
        };

        await using var connection =
            await _database.OpenConnectionAsync();

        const string equiposSql = """
            SELECT id, name
            FROM team
            ORDER BY name;
            """;

        await using (var command =
            new NpgsqlCommand(equiposSql, connection))
        await using (var reader =
            await command.ExecuteReaderAsync())
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

        const string jugadoresSql = """
            SELECT
                p.id,
                p.full_name,
                p.position,
                p.age,
                t.name AS equipo,
                COALESCE(est.partidos, 0) AS partidos,
                COALESCE(r.goals, 0) AS goles,
                COALESCE(r.assists, 0) AS asistencias,
                COALESCE(r.minutes_played, 0) AS minutos,
                r.performance_index
            FROM player AS p
            INNER JOIN team AS t
                ON t.id = p.team_id
            LEFT JOIN player_performance AS r
                ON r.player_id = p.id
                AND r.season = 2026
            LEFT JOIN (
                SELECT
                    player_id,
                    COUNT(DISTINCT match_id)::integer AS partidos
                FROM player_match_stats
                GROUP BY player_id
            ) AS est
                ON est.player_id = p.id
            WHERE
                (@equipo_id = 0 OR t.id = @equipo_id)
                AND (
                    @busqueda = ''
                    OR p.full_name ILIKE '%' || @busqueda || '%'
                    OR t.name ILIKE '%' || @busqueda || '%'
                )
            ORDER BY t.name, p.full_name;
            """;

        await using (var command =
            new NpgsqlCommand(jugadoresSql, connection))
        {
            command.Parameters.AddWithValue(
                "equipo_id", model.EquipoId);

            command.Parameters.AddWithValue(
                "busqueda", model.Busqueda);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                model.Jugadores.Add(new JugadorFilaViewModel
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Posicion = reader.GetString(2),

                    Edad = reader.IsDBNull(3)
                        ? null
                        : reader.GetInt32(3),

                    Equipo = reader.GetString(4),
                    Partidos = reader.GetInt32(5),
                    Goles = reader.GetInt32(6),
                    Asistencias = reader.GetInt32(7),
                    Minutos = reader.GetInt32(8),

                    Indice = reader.IsDBNull(9)
                        ? null
                        : reader.GetDecimal(9)
                });
            }
        }

        return View(model);
    }

    // ============================================================
    // NIVEL 2 - PERFIL INDIVIDUAL DEL JUGADOR
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> Perfil(int id)
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
                p.age,
                COALESCE(est.partidos, 0),
                COALESCE(r.goals, 0),
                COALESCE(r.assists, 0),
                COALESCE(r.minutes_played, 0),
                r.performance_index
            FROM player AS p
            INNER JOIN team AS t
                ON t.id = p.team_id
            LEFT JOIN player_performance AS r
                ON r.player_id = p.id
                AND r.season = 2026
            LEFT JOIN (
                SELECT
                    player_id,
                    COUNT(DISTINCT match_id)::integer AS partidos
                FROM player_match_stats
                GROUP BY player_id
            ) AS est
                ON est.player_id = p.id
            WHERE p.id = @id;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", id);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound("Jugador no encontrado.");

        ViewBag.Nombre = reader.GetString(0);
        ViewBag.Equipo = reader.GetString(1);
        ViewBag.Posicion = reader.GetString(2);

        ViewBag.Edad = reader.IsDBNull(3)
            ? "Sin registrar"
            : reader.GetInt32(3).ToString();

        ViewBag.Partidos = reader.GetInt32(4);
        ViewBag.Goles = reader.GetInt32(5);
        ViewBag.Asistencias = reader.GetInt32(6);
        ViewBag.Minutos = reader.GetInt32(7);

        ViewBag.Indice = reader.IsDBNull(8)
            ? "Sin datos"
            : reader.GetDecimal(8).ToString("0.00");

        await reader.CloseAsync();

        const string partidosSql = """
            SELECT
                fm.id,
                fm.match_date,
                local.name AS equipo_local,
                visitante.name AS equipo_visitante,
                fm.home_goals,
                fm.away_goals,
                COALESCE(pms.minutes_played, 0),
                COALESCE(pms.goals, 0),
                COALESCE(pms.assists, 0),
                COALESCE(pms.shots, 0),
                pms.performance_index
            FROM player_match_stats AS pms
            INNER JOIN football_match AS fm
                ON fm.id = pms.match_id
            INNER JOIN team AS local
                ON local.id = fm.home_team_id
            INNER JOIN team AS visitante
                ON visitante.id = fm.away_team_id
            WHERE pms.player_id = @id
            ORDER BY fm.match_date DESC, fm.id DESC;
            """;

        var partidos = new List<PartidoJugadorViewModel>();

        await using (var partidosCommand =
            new NpgsqlCommand(partidosSql, connection))
        {
            partidosCommand.Parameters.AddWithValue("id", id);

            await using var partidosReader =
                await partidosCommand.ExecuteReaderAsync();

            while (await partidosReader.ReadAsync())
            {
                partidos.Add(new PartidoJugadorViewModel
                {
                    PartidoId = partidosReader.GetInt32(0),
                    Fecha = partidosReader.GetDateTime(1),
                    EquipoLocal = partidosReader.GetString(2),
                    EquipoVisitante = partidosReader.GetString(3),
                    GolesLocal = partidosReader.GetInt32(4),
                    GolesVisitante = partidosReader.GetInt32(5),
                    Minutos = partidosReader.GetInt32(6),
                    Goles = partidosReader.GetInt32(7),
                    Asistencias = partidosReader.GetInt32(8),
                    Remates = partidosReader.GetInt32(9),

                    IndiceRendimiento = partidosReader.IsDBNull(10)
                        ? null
                        : partidosReader.GetDecimal(10)
                });
            }
        }

        ViewBag.PartidosJugador = partidos;

        return View();
    }

    // ============================================================
    // NIVEL 3 - DETALLE INDIVIDUAL DE UN PARTIDO
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> DetallePartido(
        int id,
        int partidoId = 0)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        await using var connection =
            await _database.OpenConnectionAsync();

        const string sql = """
            SELECT
                p.full_name,
                fm.id,
                fm.match_date,
                local.name AS equipo_local,
                visitante.name AS equipo_visitante,
                COALESCE(fm.home_goals, 0),
                COALESCE(fm.away_goals, 0),
                COALESCE(pms.minutes_played, 0),
                COALESCE(pms.goals, 0),
                COALESCE(pms.assists, 0),
                COALESCE(pms.shots, 0),
                COALESCE(pms.passes_completed, 0),
                COALESCE(pms.passes_attempted, 0),
                pms.performance_index
            FROM player_match_stats AS pms
            INNER JOIN player AS p
                ON p.id = pms.player_id
            INNER JOIN football_match AS fm
                ON fm.id = pms.match_id
            INNER JOIN team AS local
                ON local.id = fm.home_team_id
            INNER JOIN team AS visitante
                ON visitante.id = fm.away_team_id
            WHERE
                pms.player_id = @id
                AND (
                    @partidoId = 0
                    OR fm.id = @partidoId
                )
            ORDER BY fm.match_date DESC, fm.id DESC
            LIMIT 1;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("partidoId", partidoId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return NotFound(
                "No se encontraron estadísticas para este jugador y partido.");
        }

        // Información del jugador y del encuentro.
        ViewBag.JugadorId = id;
        ViewBag.PartidoId = reader.GetInt32(1);
        ViewBag.Jugador = reader.GetString(0);

        ViewBag.Fecha =
            reader.GetDateTime(2).ToString("dd/MM/yyyy");

        ViewBag.EquipoLocal = reader.GetString(3);
        ViewBag.EquipoVisitante = reader.GetString(4);

        ViewBag.GolesLocal = reader.GetInt32(5);
        ViewBag.GolesVisitante = reader.GetInt32(6);

        // Estadísticas individuales.
        ViewBag.Minutos = reader.GetInt32(7);
        ViewBag.Goles = reader.GetInt32(8);
        ViewBag.Asistencias = reader.GetInt32(9);
        ViewBag.Remates = reader.GetInt32(10);

        int pasesCompletados = reader.GetInt32(11);
        int pasesIntentados = reader.GetInt32(12);

        ViewBag.PasesCompletados = pasesCompletados;
        ViewBag.PasesIntentados = pasesIntentados;

        // Precisión de pases expresada como porcentaje.
        if (pasesIntentados > 0)
        {
            decimal precision =
                (decimal)pasesCompletados / pasesIntentados * 100;

            ViewBag.PrecisionPases =
                precision.ToString(
                    "0.0",
                    CultureInfo.GetCultureInfo("es-AR")) + "%";
        }
        else
        {
            ViewBag.PrecisionPases = "—";
        }

        // Índice de rendimiento.
        ViewBag.Indice = reader.IsDBNull(13)
            ? "Sin datos"
            : reader.GetDecimal(13).ToString("0.00");

        // Mostramos la vista Razor en lugar de devolver JSON.
        return View();
    }
}
