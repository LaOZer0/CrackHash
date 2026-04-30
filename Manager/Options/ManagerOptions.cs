namespace Manager.Options;

public class ManagerOptions
{
    /// <summary>
    /// Алфавит для генерации строк
    /// </summary>
    public string Alphabet { get; set; } = "abcdefghijklmnopqrstuvwxyz0123456789";
        
    /// <summary>
    /// Таймаут ожидания ответа от воркера (в минутах)
    /// </summary>
    public int TaskTimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Таймаут проверки здоровья воркера (в секундах)
    /// </summary>
    public int HealthCheckTimeoutSeconds { get; set; } = 2;

    /// <summary>
    /// Список URL воркеров (разделены запятыми)
    /// </summary>
    public string WorkerUrls { get; set; } = "http://worker:8080";
}