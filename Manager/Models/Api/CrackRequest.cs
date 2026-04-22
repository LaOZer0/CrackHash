namespace Manager.Models.Api;

public class CrackRequest
{
    public string Hash { get; set; } = string.Empty;
    public int MaxLength { get; set; }
}