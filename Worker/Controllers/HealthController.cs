using Microsoft.AspNetCore.Mvc;

namespace Worker.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Get() => Ok(new { status = "alive" });
}