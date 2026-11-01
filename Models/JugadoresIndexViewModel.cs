namespace ScoutVision.Web.Models;

public sealed class EquipoFiltroViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class JugadorFilaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Posicion { get; set; } = string.Empty;
    public int? Edad { get; set; }
    public string Equipo { get; set; } = string.Empty;
    public int Partidos { get; set; }
    public int Goles { get; set; }
    public int Asistencias { get; set; }
    public int Minutos { get; set; }
    public decimal? Indice { get; set; }

    public string Estado => Indice is null ? "Sin datos" :
        Indice >= 75 ? "Alto" : Indice >= 50 ? "Medio" : "Bajo";

    public string ClaseEstado => Indice is null ? "status-neutral" :
        Indice >= 75 ? "status-high" : Indice >= 50 ? "status-medium" : "status-low";
}

public sealed class JugadoresIndexViewModel
{
    public string Busqueda { get; set; } = string.Empty;
    public int EquipoId { get; set; }
    public List<EquipoFiltroViewModel> Equipos { get; set; } = new();
    public List<JugadorFilaViewModel> Jugadores { get; set; } = new();
    public int CantidadEquipos => Jugadores.Select(j => j.Equipo).Distinct().Count();
    public decimal? PromedioIndice => Jugadores.Any(j => j.Indice.HasValue)
        ? Math.Round(Jugadores.Where(j => j.Indice.HasValue).Average(j => j.Indice!.Value), 1)
        : null;
}
