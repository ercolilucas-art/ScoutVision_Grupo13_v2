
namespace ScoutVision.Web.Models;

public class ReportesIndexViewModel
{
    public int EquipoId { get; set; }

    public List<EquipoFiltroViewModel> Equipos { get; set; } = new();

    public int TotalEquipos { get; set; }
    public int TotalJugadores { get; set; }
    public int TotalPartidos { get; set; }
    public int TotalParticipaciones { get; set; }

    public List<ReporteJugadorViewModel> Ranking { get; set; } = new();

    public List<ReporteEquipoViewModel> RendimientoEquipos { get; set; } = new();

    public List<ReporteMesViewModel> ActividadMensual { get; set; } = new();

    public int MaxPartidosMes =>
        ActividadMensual.Count == 0
            ? 1
            : Math.Max(1, ActividadMensual.Max(m => m.CantidadPartidos));
}

public class ReporteJugadorViewModel
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Equipo { get; set; } = string.Empty;
    public string Posicion { get; set; } = string.Empty;

    public int Partidos { get; set; }
    public int Goles { get; set; }
    public int Asistencias { get; set; }
    public int Minutos { get; set; }

    public decimal? Indice { get; set; }
}

public class ReporteEquipoViewModel
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int Jugadores { get; set; }
    public int Goles { get; set; }

    public decimal? IndicePromedio { get; set; }
}

public class ReporteMesViewModel
{
    public string Periodo { get; set; } = string.Empty;

    public int CantidadPartidos { get; set; }
}

