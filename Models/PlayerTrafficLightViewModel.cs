namespace ScoutVision.Web.Models;

public class PlayerTrafficLightViewModel
{
    public int Id { get; set; }
    public string Player { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public decimal PerformanceIndex { get; set; }
    public string Status { get; set; } = string.Empty;

    public string StatusCss => Status switch
    {
        "Alto" => "status-high",
        "Medio" => "status-medium",
        _ => "status-low"
    };
}
