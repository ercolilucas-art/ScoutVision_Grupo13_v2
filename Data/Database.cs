using Npgsql;

namespace ScoutVision.Web.Data;

public class Database
{
    private readonly string _connectionString;

    public Database(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ScoutVisionDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión ScoutVisionDb.");
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
