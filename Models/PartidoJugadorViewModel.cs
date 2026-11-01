
namespace ScoutVision.Web.Models;

public class PartidoJugadorViewModel
{
    public int PartidoId { get; set; }

    public DateTime Fecha { get; set; }

    public string EquipoLocal { get; set; } = string.Empty;

    public string EquipoVisitante { get; set; } = string.Empty;

    public int GolesLocal { get; set; }

    public int GolesVisitante { get; set; }

    public int Minutos { get; set; }

    public int Goles { get; set; }

    public int Asistencias { get; set; }

    public int Remates { get; set; }

    public decimal? IndiceRendimiento { get; set; }
}
