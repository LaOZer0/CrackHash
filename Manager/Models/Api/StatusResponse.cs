namespace Manager.Models.Api;

public class StatusResponse
{
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? EstimatedTimeRemaining { get; set; }
    public List<string>? Data { get; set; }
}