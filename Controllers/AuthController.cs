using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScoutVision.Web.Data;
using ScoutVision.Web.Models;

namespace ScoutVision.Web.Controllers;

public class AuthController : Controller
{
    private readonly Database _database;

    public AuthController(Database database)
    {
        _database = database;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("UserId") is not null)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim().ToLowerInvariant();
        var username = model.Username.Trim();

        await using var connection = await _database.OpenConnectionAsync();

        const string existsSql = """
            SELECT EXISTS (
                SELECT 1
                FROM app_user
                WHERE LOWER(email) = LOWER(@email)
                   OR LOWER(username) = LOWER(@username)
            );
            """;

        await using (var existsCommand = new NpgsqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("email", email);
            existsCommand.Parameters.AddWithValue("username", username);

            var exists = (bool)(await existsCommand.ExecuteScalarAsync() ?? false);
            if (exists)
            {
                ModelState.AddModelError(string.Empty, "El correo o el nombre de usuario ya están registrados.");
                return View(model);
            }
        }

        const string insertSql = """
            INSERT INTO app_user (email, username, password_hash, is_active)
            VALUES (@email, @username, crypt(@password, gen_salt('bf', 10)), TRUE)
            RETURNING id;
            """;

        await using (var insertCommand = new NpgsqlCommand(insertSql, connection))
        {
            insertCommand.Parameters.AddWithValue("email", email);
            insertCommand.Parameters.AddWithValue("username", username);
            insertCommand.Parameters.AddWithValue("password", model.Password);
            await insertCommand.ExecuteScalarAsync();
        }

        TempData["RegisterSuccess"] = "Cuenta creada correctamente. Ya podés iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetInt32("UserId") is not null)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await using var connection = await _database.OpenConnectionAsync();

        const string sql = """
            SELECT id, username
            FROM app_user
            WHERE LOWER(username) = LOWER(@username)
              AND is_active = TRUE
              AND password_hash = crypt(@password, password_hash)
            LIMIT 1;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("username", model.Username.Trim());
        command.Parameters.AddWithValue("password", model.Password);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(model);
        }

        HttpContext.Session.SetInt32("UserId", reader.GetInt32(0));
        HttpContext.Session.SetString("Username", reader.GetString(1));

        TempData["LoginSuccess"] = "Credenciales validadas correctamente.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
