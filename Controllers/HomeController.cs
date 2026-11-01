using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScoutVision.Web.Data;
using ScoutVision.Web.Models;

namespace ScoutVision.Web.Controllers;

public class HomeController : Controller
{
    private readonly Database _database;

    public HomeController(Database database)
    {
        _database = database;
    }

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Auth");

        await using var connection = await _database.OpenConnectionAsync();

        const string sql = """
            SELECT
                (SELECT COUNT(*)::int FROM player) AS players,
                (SELECT COUNT(*)::int FROM team) AS teams,
                (SELECT COUNT(*)::int FROM football_match) AS matches;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        var model = new HomeViewModel
        {
            Username = HttpContext.Session.GetString("Username") ?? "Analista",
            Players = reader.GetInt32(0),
            Teams = reader.GetInt32(1),
            Matches = reader.GetInt32(2)
        };

        return View(model);
    }

    public IActionResult Error() => View();
}
