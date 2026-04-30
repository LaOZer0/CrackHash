using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Worker.Models;
using Worker.Models.Xml;
using Worker.Services;

namespace Worker.Controllers;

[ApiController]
[Route("internal/api/worker/hash/crack")]
public class TaskController : ControllerBase
{
    private readonly IHashCracker _cracker;
    private readonly IManagerClient _managerClient;
    private readonly IConfiguration _config;
    private readonly ILogger<TaskController> _logger;

    public TaskController(
        IHashCracker cracker,
        IManagerClient managerClient,
        IConfiguration config,
        ILogger<TaskController> logger)
    {
        _cracker = cracker;
        _managerClient = managerClient;
        _config = config;
        _logger = logger;
    }

    [HttpPost("task")]
    public async Task<IActionResult> ProcessTask()
    {
        // 1️⃣ Быстро читаем и парсим XML
        using var reader = new StreamReader(Request.Body);
        var xml = await reader.ReadToEndAsync();
    
        var serializer = new XmlSerializer(typeof(CrackHashManagerRequest), 
            "http://ccfit.nsu.ru/schema/crack-hash-request");
    
        CrackHashManagerRequest? request;
        using (var stringReader = new StringReader(xml))
        {
            request = serializer.Deserialize(stringReader) as CrackHashManagerRequest;
        }
    
        if (request == null)
            return BadRequest("Invalid XML");
    
        // 2️⃣ 🔥 СРАЗУ возвращаем 200 ОК менеджеру — НЕ ждём завершения перебора!
        //    Обработка продолжится в фоне после отправки ответа
        _ = Task.Run(() => ProcessTaskInBackground(request));
    
        return Ok(); // ← Менеджер получает ответ за ~100мс, не ждёт 24 минуты
    }

// 🔹 Фоновая обработка — не блокирует HTTP-поток
    private async Task ProcessTaskInBackground(CrackHashManagerRequest request)
    {
        try
        {
            var task = new WorkerTask
            {
                RequestId = request.RequestId,
                PartNumber = request.PartNumber,
                PartCount = request.PartCount,
                Hash = request.Hash,
                MaxLength = request.MaxLength,
                Alphabet = request.Alphabet.Symbols.Select(s => s[0]).ToArray()
            };
        
            var results = await _cracker.CrackAsync(task);
        
            var managerUrl = _config["ManagerUrl"] ?? "http://manager:8080";
            await _managerClient.SendResultAsync(managerUrl, request.RequestId, request.PartNumber, results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background processing failed for {RequestId}", request.RequestId);
            // Можно отправить пустой ответ или игнорировать — менеджер учтёт это как FailedPart по таймауту
        }
    }
}