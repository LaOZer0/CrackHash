using System.Text;
using System.Xml.Serialization;
using Manager.Models.Xml;

namespace Manager.Services;

public class WorkerClient : IWorkerClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorkerClient> _logger;

    public WorkerClient(
        IHttpClientFactory httpClientFactory,
        ILogger<WorkerClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendTaskAsync(string workerUrl, CrackHashManagerRequest request)
    {
        var endpoint = $"{workerUrl}/internal/api/worker/hash/crack/task";
            
        var serializer = new XmlSerializer(typeof(CrackHashManagerRequest), 
            "http://ccfit.nsu.ru/schema/crack-hash-request");
            
        using var client = _httpClientFactory.CreateClient();
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, request);
        var xml = stringWriter.ToString();
            
        var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            
        try
        {
            var response = await client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Task sent to {Endpoint}", endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send task to {Endpoint}", endpoint);
            throw;
        }
    }
}