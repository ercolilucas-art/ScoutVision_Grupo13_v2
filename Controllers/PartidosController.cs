
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScoutVision.Web.Data;
using ScoutVision.Web.Models;

namespace ScoutVision.Web.Controllers;

public class PartidosController : Controller
{
    private readonly Database _database;

    public PartidosController(Database database)
    {
        _database = database;
    }

    // LISTADO GENERAL DE PARTIDOS

    [HttpGet]
    public async Task<IActionResult> Index(int equipoId = 0)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        var model = new PartidosIndexViewModel
        {
            EquipoId = equipoId
        };

        await using var connection =
            await _database.OpenConnectionAsync();

        // Equipos para el filtro

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

        // Partidos y cantidad de jugadores con estadísticas

        const string partidosSql = """
            SELECT
                fm.id,
                fm.match_date,
                local.name,
                visitante.name,
                fm.home_goals,
                fm.away_goals,
                COUNT(DISTINCT pms.player_id)::integer
                    AS jugadores_con_datos
            FROM football_match AS fm
            INNER JOIN team AS local
                ON local.id = fm.home_team_id
            INNER JOIN team AS visitante
                ON visitante.id = fm.away_team_id
            LEFT JOIN player_match_stats AS pms
                ON pms.match_id = fm.id
            WHERE (
                @equipoId = 0
                OR fm.home_team_id = @equipoId
                OR fm.away_team_id = @equipoId
            )
            GROUP BY
                fm.id,
                fm.match_date,
                local.name,
                visitante.name,
                fm.home_goals,
                fm.away_goals
            ORDER BY fm.match_date DESC, fm.id DESC;
            """;

        await using (var command =
            new NpgsqlCommand(partidosSql, connection))
        {
            command.Parameters.AddWithValue(
                "equipoId", equipoId);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                model.Partidos.Add(new PartidoResumenViewModel
                {
                    Id = reader.GetInt32(0),
                    Fecha = reader.GetDateTime(1),
                    EquipoLocal = reader.GetString(2),
                    EquipoVisitante = reader.GetString(3),

                    GolesLocal = reader.IsDBNull(4)
                        ? null
                        : reader.GetInt32(4),

                    GolesVisitante = reader.IsDBNull(5)
                        ? null
                        : reader.GetInt32(5),

                    JugadoresConEstadisticas =
                        reader.GetInt32(6)
                });
            }
        }

        return View(model);
    }

    // DETALLE DE UN PARTIDO

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        await using var connection =
            await _database.OpenConnectionAsync();

        const string partidoSql = """
            SELECT
                fm.id,
                fm.match_date,
                local.name,
                visitante.name,
                fm.home_goals,
                fm.away_goals
            FROM football_match AS fm
            INNER JOIN team AS local
                ON local.id = fm.home_team_id
            INNER JOIN team AS visitante
                ON visitante.id = fm.away_team_id
            WHERE fm.id = @id;
            """;

        var model = new PartidoDetalleViewModel();

        await using (var command =
            new NpgsqlCommand(partidoSql, connection))
        {
            command.Parameters.AddWithValue("id", id);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound("Partido no encontrado.");

            model.Id = reader.GetInt32(0);
            model.Fecha = reader.GetDateTime(1);
            model.EquipoLocal = reader.GetString(2);
            model.EquipoVisitante = reader.GetString(3);

            model.GolesLocal = reader.IsDBNull(4)
                ? null
                : reader.GetInt32(4);

            model.GolesVisitante = reader.IsDBNull(5)
                ? null
                : reader.GetInt32(5);
        }

        // Estadísticas de futbolistas en el encuentro

        const string estadisticasSql = """
            SELECT
                p.id,
                p.full_name,
                p.position,
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
            WHERE pms.match_id = @id
            ORDER BY p.full_name;
            """;

        await using (var command =
            new NpgsqlCommand(estadisticasSql, connection))
        {
            command.Parameters.AddWithValue("id", id);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                model.Estadisticas.Add(
                    new EstadisticaPartidoViewModel
                    {
                        JugadorId = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Posicion = reader.GetString(2),
                        Minutos = reader.GetInt32(3),
                        Goles = reader.GetInt32(4),
                        Asistencias = reader.GetInt32(5),
                        Remates = reader.GetInt32(6),
                        PasesCompletados = reader.GetInt32(7),
                        PasesIntentados = reader.GetInt32(8),

                        IndiceRendimiento = reader.IsDBNull(9)
                            ? null
                            : reader.GetDecimal(9)
                    });
            }
        }

        return View(model);
    }
}
