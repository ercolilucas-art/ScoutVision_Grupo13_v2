
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScoutVision.Web.Data;
using ScoutVision.Web.Models;

namespace ScoutVision.Web.Controllers;

public class EquiposController : Controller
{
    private readonly Database _database;

    public EquiposController(Database database)
    {
        _database = database;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        await using var connection =
            await _database.OpenConnectionAsync();

        const string sql = """
            SELECT
                t.id,
                t.name,
                COUNT(p.id)::integer AS cantidad_jugadores
            FROM team AS t
            LEFT JOIN player AS p
                ON p.team_id = t.id
            GROUP BY t.id, t.name
            ORDER BY t.name;
            """;

        var equipos = new List<EquipoResumenViewModel>();

        await using var command =
            new NpgsqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            equipos.Add(new EquipoResumenViewModel
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                CantidadJugadores = reader.GetInt32(2)
            });
        }

        return View(equipos);
    }
}
