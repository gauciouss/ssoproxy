using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ssoproxy.Models;
using ssoproxy.Services.Auth;
using gd.Core;
using gd.Util;
using NLog;

namespace ssoproxy.Controllers;

/// <summary>
/// 提供目前已驗證使用者的身分資訊。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>
    /// 取得目前 Token 對應的使用者資訊。
    /// </summary>
    [HttpGet("Me")]
    public async Task<ResponseEntity> Me()
    {
        var profiler = new Profiler();

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) && Request.Cookies.TryGetValue("sso_token", out var cookieToken))
        {
            authHeader = $"Bearer {cookieToken}";
        }

        if (string.IsNullOrWhiteSpace(authHeader))
        {
            profiler.Stop();
            return new ResponseEntity(
                body: null,
                code: GDExCode.Unauthorized.Code,
                message: GDExCode.Unauthorized.Message,
                execTime: profiler.ExecutionTime);
        }

        var userInfoJson = await _tokenService.ValidateTokenAsync(authHeader);
        if (string.IsNullOrWhiteSpace(userInfoJson))
        {
            profiler.Stop();
            return new ResponseEntity(
                body: null,
                code: GDExCode.Unauthorized.Code,
                message: GDExCode.Unauthorized.Message,
                execTime: profiler.ExecutionTime);
        }

        try
        {
            using var document = JsonDocument.Parse(userInfoJson);
            var userInfo = document.RootElement.Clone();

            _logger.Info(profiler.StopLog("Retrieved authenticated user information successfully: {UserInfo}"), userInfo);

            return new ResponseEntity(
                body: userInfo,
                code: GDExCode.Success.Code,
                message: GDExCode.Success.Message,
                execTime: profiler.ExecutionTime);
        }
        catch (JsonException)
        {
            _logger.Error(profiler.StopLog("Failed to parse authenticated user information from token data: {UserInfoJson}"), userInfoJson);
            return new ResponseEntity(
                body: null,
                code: GDExCode.UnexpectedDataFormat.Code,
                message: GDExCode.UnexpectedDataFormat.Message,
                execTime: profiler.ExecutionTime);
        }
    }
}
