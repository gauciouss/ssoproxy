using Microsoft.AspNetCore.Mvc;
using ssoproxy.Models;
using ssoproxy.Services;
using gd.Core;
using gd.Util;
using NLog;

namespace ssoproxy.Controllers;

/// <summary>
/// 提供 SSO 相關的 API Token 取得接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ILoginService _loginService;

    public TokenController(ILoginService loginService)
    {
        _loginService = loginService;
    }

    /// <summary>
    /// 透過 API 登入並產生 token。
    /// </summary>
    /// <param name="request">包含帳號與密碼的登入請求。</param>
    /// <returns>若登入成功，回傳統一的 ResponseEntity；失敗則回傳相同格式的錯誤資訊。</returns>
    [HttpPost]
    public async Task<ResponseEntity> Post([FromBody] TokenRequest request)
    {
        var profiler = new Profiler();
        ResponseEntity result;

        try
        {
            var loginResult = await _loginService.AuthenticateAsync(request.Username, request.Password);
            _logger.Info("[SSO_API_TOKEN_SUCCESS] User {Username} token created", request.Username);

            result = new ResponseEntity(
                body: new { Token = loginResult.Token, ExpiresInMinutes = loginResult.ExpiresInMinutes },
                code: GDExCode.Success.Code,
                message: "Token 取得成功",
                execTime: 0);
        }
        catch (GDException ex)
        {
            result = new ResponseEntity(
                body: null,
                code: ex.ErrorCode ?? ex.ExceptionCode?.Code ?? "99-999",
                message: ex.Message,
                execTime: 0);

            profiler.Stop();
            _logger.Error(ex, profiler.StopLog("[SSO_API_TOKEN_ERROR] User {Username} token creation failed: {ErrorMessage}"), request.Username, ex.Message);
        }

        profiler.Stop();
        result.Header.ExecTime = profiler.ExecutionTime;
        return result;
    }
}
