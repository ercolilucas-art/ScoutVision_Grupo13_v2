using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScoutVision.Web.Data;
using ScoutVision.Web.Models;

namespace ScoutVision.Web.Controllers;

public class SemaforizacionController : Controller
{
    private readonly Database _database;

    public SemaforizacionController(Database database)
    {
        _database = database;
    }

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        var players = new List<PlayerTrafficLightViewModel>();

        await using var connection = await _database.OpenConnectionAsync();

        const string sql = """
            SELECT
                p.id,
                p.full_name,
                p.position,
                t.name AS team,
                pr.performance_index,
                CASE
                    WHEN pr.performance_index >= 75 THEN 'Alto'
                    WHEN pr.performance_index >= 50 THEN 'Medio'
                    ELSE 'Bajo'
                END AS status
            FROM player p
            INNER JOIN team t ON t.id = p.team_id
            INNER JOIN player_performance pr ON pr.player_id = p.id
            WHERE pr.season = 2026
            ORDER BY pr.performance_index DESC;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            players.Add(new PlayerTrafficLightViewModel
            {
                Id = reader.GetInt32(0),
                Player = reader.GetString(1),
                Position = reader.GetString(2),
                Team = reader.GetString(3),
                PerformanceIndex = reader.GetDecimal(4),
                Status = reader.GetString(5)
            });
        }

        return View(players);
    }
}
