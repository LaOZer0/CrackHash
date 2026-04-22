namespace Manager.Models;

public enum TaskStatus { Queued, InProgress, Ready, Error }

public class RequestState
{
    public string RequestId { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public TaskStatus Status { get; set; }
    public int AssignedWorkerCount { get; set; }
    public HashSet<int> CompletedParts { get; set; } = new();
    public List<string> Results { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public int Progress => AssignedWorkerCount > 0 
        ? (int)((double)CompletedParts.Count / AssignedWorkerCount * 100) 
        : 0;

    public string? EstimatedTimeRemaining
    {
        get
        {
            if (!StartedAt.HasValue || Progress == 0 || Progress >= 100) 
                return null;
                
            var elapsed = DateTime.UtcNow - StartedAt.Value;
            var rate = elapsed.TotalSeconds / Progress;
            var remaining = rate * (100 - Progress);
            return TimeSpan.FromSeconds(remaining).ToString(@"mm\:ss");
        }
    }
}