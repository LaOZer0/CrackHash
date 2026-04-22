namespace Worker.Models;

public class WorkerTask
{
    public string RequestId { get; set; } = string.Empty;
    public int PartNumber { get; set; }
    public int PartCount { get; set; }
    public string Hash { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public char[] Alphabet { get; set; } = Array.Empty<char>();
}