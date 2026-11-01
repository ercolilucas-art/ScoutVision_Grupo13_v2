
namespace ScoutVision.Web.Models;

// Resumen de un partido para el listado general.

public class PartidoResumenViewModel
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    public string EquipoLocal { get; set; } = string.Empty;

    public string EquipoVisitante { get; set; } = string.Empty;

    public int? GolesLocal { get; set; }

    public int? GolesVisitante { get; set; }

    public int JugadoresConEstadisticas { get; set; }
}

// Modelo del listado general con filtros.

public class PartidosIndexViewModel
{
    public int EquipoId { get; set; }

    public List<EquipoFiltroViewModel> Equipos { get; set; } = new();

    public List<PartidoResumenViewModel> Partidos { get; set; } = new();

    public int TotalParticipaciones =>
        Partidos.Sum(p => p.JugadoresConEstadisticas);
}

// Estadísticas de un futbolista en un partido.

public class EstadisticaPartidoViewModel
{
    public int JugadorId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Posicion { get; set; } = string.Empty;

    public int Minutos { get; set; }

    public int Goles { get; set; }

    public int Asistencias { get; set; }

    public int Remates { get; set; }

    public int PasesCompletados { get; set; }

    public int PasesIntentados { get; set; }

    public decimal? IndiceRendimiento { get; set; }

    public decimal? PrecisionPases =>
        PasesIntentados > 0
            ? Math.Round(
                PasesCompletados * 100m / PasesIntentados, 1)
            : null;
}

// Detalle completo del encuentro.

public class PartidoDetalleViewModel
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    public string EquipoLocal { get; set; } = string.Empty;

    public string EquipoVisitante { get; set; } = string.Empty;

    public int? GolesLocal { get; set; }

    public int? GolesVisitante { get; set; }

    public List<EstadisticaPartidoViewModel> Estadisticas
    { get; set; } = new();
}
