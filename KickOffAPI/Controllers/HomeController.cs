using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(new { message = "connected to API" });
}