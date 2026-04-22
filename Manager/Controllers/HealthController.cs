using Microsoft.AspNetCore.Mvc;

namespace Manager.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Get() => Ok(new { status = "alive" });
}