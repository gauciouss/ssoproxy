using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ssoproxy.Models;
using ssoproxymgr.Services;

namespace ssoproxymgr.Controllers;

[Authorize]
[ApiController]
[Route("api/proxy")]
public class ProxyManagerApiController : ControllerBase
{
    private readonly ReverseProxyManagerService _service;

    public ProxyManagerApiController(ReverseProxyManagerService service)
    {
        _service = service;
    }

    [HttpGet("config")]
    public async Task<ActionResult<ReverseProxyConfigPayload>> GetConfig()
    {
        return Ok(await _service.LoadCurrentConfigAsync());
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] ReverseProxyConfigPayload payload)
    {
        await _service.SaveConfigAsync(payload ?? new ReverseProxyConfigPayload());
        return Ok(new { success = true, message = "Saved to SQL." });
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] ReverseProxyConfigPayload payload)
    {
        await _service.PublishAsync(payload ?? new ReverseProxyConfigPayload());
        return Ok(new { success = true, message = "Saved and published to ssoproxy." });
    }
}
