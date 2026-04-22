using System.Text;
using System.Xml.Serialization;
using Worker.Models.Xml;

namespace Worker.Services;

public class ManagerClient : IManagerClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ManagerClient> _logger;

        public ManagerClient(
            IHttpClientFactory httpClientFactory,
            ILogger<ManagerClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task SendResultAsync(
            string managerUrl, 
            string requestId, 
            int partNumber, 
            List<string> answers)
        {
            var endpoint = $"{managerUrl}/internal/api/manager/hash/crack/request";
            
            var response = new CrackHashWorkerResponse
            {
                RequestId = requestId,
                PartNumber = partNumber,
                Answers = new Answers { Words = answers }
            };
            
            var serializer = new XmlSerializer(typeof(CrackHashWorkerResponse), 
                "http://ccfit.nsu.ru/schema/crack-hash-response");
            
            using var client = _httpClientFactory.CreateClient();
            using var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, response);
            var xml = stringWriter.ToString();
            
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            
            try
            {
                var httpMethod = new HttpMethod("PATCH");
                var request = new HttpRequestMessage(httpMethod, endpoint) { Content = content };
                var httpResponse = await client.SendAsync(request);
                httpResponse.EnsureSuccessStatusCode();
                
                _logger.LogDebug("Result sent to manager: {RequestId}, part {PartNumber}, {Count} answers",
                    requestId, partNumber, answers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send result to manager");
                throw;
            }
        }
    }