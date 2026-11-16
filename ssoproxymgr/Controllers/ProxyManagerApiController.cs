using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ssoproxymgr.Models;
using ssoproxymgr.Services;

namespace ssoproxymgr.Controllers;

[Authorize]
[ApiController]
[Route("api/proxy")]
public class ProxyManagerApiController : ControllerBase
{
    private readonly ReverseProxyManagerService _service;

    private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public ProxyManagerApiController(ReverseProxyManagerService service)
    {
        _service = service;
    }

    [HttpGet("config")]
    public async Task<ActionResult<ReverseProxyConfigPayload>> GetConfig()
    {
        _logger.Trace("Loading config from SQL...");
        var result = Ok(await _service.LoadCurrentConfigAsync());
        _logger.Info("Loaded config from SQL.");
        return result;
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] ReverseProxyConfigPayload payload)
    {
        _logger.Trace("Saving config to SQL...");
        await _service.SaveConfigAsync(payload ?? new ReverseProxyConfigPayload());
        _logger.Info("Saved config to SQL.");
        return Ok(new { success = true, message = "Saved to SQL." });
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] ReverseProxyConfigPayload payload)
    {
        _logger.Trace("Publishing config...");
        await _service.PublishAsync(payload ?? new ReverseProxyConfigPayload());
        var _ok = Ok(new { success = true, message = "Published to reverse proxy." });
        _logger.Info("Published config.");
        return _ok;
    }
}
