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
    private readonly ILoginService _loginService;

    public AuthController(ITokenService tokenService, ILoginService loginService)
    {
        _tokenService = tokenService;
        _loginService = loginService;
    }

    /// <summary>
    /// 使用者登入，驗證帳號密碼並產生 Token。
    /// 1. 從請求的 body 中取得使用者帳號與密碼。
    /// 2. 呼叫 LoginService 進行帳號密碼驗證，若驗證成功則產生 Token。
    /// 3. 將 Token 與其有效期限回傳給前端。
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [HttpPost("token")]
    public async Task<ResponseEntity> CreateToken([FromBody] TokenRequest request)
    {
        var profiler = new Profiler();

        try
        {
            if (request is null)
            {
                return new ResponseEntity(
                    body: null,
                    code: GDExCode.InvalidInput.Code,
                    message: GDExCode.InvalidInput.Message,
                    execTime: profiler.ExecutionTime);
            }

            var loginResult = await _loginService.AuthenticateAsync(request.Username, request.Password);
            var response = new ResponseEntity(
                body: new { Token = loginResult.Token, ExpiresInMinutes = loginResult.ExpiresInMinutes },
                code: GDExCode.Success.Code,
                message: GDExCode.Success.Message,
                execTime: profiler.ExecutionTime);

            _logger.Info(profiler.StopLog("Issued token for user {Username}"), request.Username);
            return response;
        }
        catch (GDException ex)
        {
            _logger.Error(ex, profiler.StopLog("Failed to issue token for user {Username}: {Message}"), request?.Username ?? "unknown", ex.Message);
            return new ResponseEntity(
                body: null,
                code: ex.ErrorCode ?? ex.ExceptionCode?.Code ?? GDExCode.UnknownError.Code,
                message: ex.Message,
                execTime: profiler.ExecutionTime);
        }
    }
    

    /// <summary>
    /// 登出使用者，從 Redis 中移除對應的 Token。
    /// 1. 從 Authorization Header 或 Cookie 中取得 Token。
    /// 2. 從 Redis 中移除 Token 對應的使用者資訊。
    /// </summary>
    /// <returns></returns>
    [HttpPost("logout")]
    public async Task<ResponseEntity> Logout()
    {
        var profiler = new Profiler();

        try
        {            
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) && Request.Cookies.TryGetValue("sso_token", out var cookieToken))
            {
                authHeader = $"Bearer {cookieToken}";
            }

            _logger.Info(profiler.StartLog("Logging out user. AuthHeader: {AuthHeader}"), authHeader);

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                profiler.Stop();
                return new ResponseEntity(
                    body: null,
                    code: GDExCode.Unauthorized.Code,
                    message: GDExCode.Unauthorized.Message,
                    execTime: profiler.ExecutionTime);
            }

            string token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            await _tokenService.RemoveTokenAsync(token);

            _logger.Info(profiler.StopLog("User logged out successfully. Token: {Token}"), token);
            return new ResponseEntity(
                body: null,
                code: GDExCode.Success.Code,
                message: GDExCode.Success.Message,
                execTime: profiler.ExecutionTime);
        }
        catch (GDException ex)
        {
            _logger.Error(ex, profiler.StopLog("Logout failed: {Message}"), ex.Message);
            return new ResponseEntity(
                body: null,
                code: ex.ErrorCode ?? ex.ExceptionCode?.Code ?? GDExCode.UnknownError.Code,
                message: ex.Message,
                execTime: profiler.ExecutionTime);
        }
    }   
        

    /// <summary>
    /// 取得目前 Token 對應的使用者資訊。
    /// </summary>
    [HttpGet("Me")]
    public async Task<ResponseEntity> Me()
    {
        var profiler = new Profiler();

        // 1. 從 Authorization Header 或 Cookie 中取得 Token
        // 2. 驗證 Token 是否有效，並取得對應的使用者資訊
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
