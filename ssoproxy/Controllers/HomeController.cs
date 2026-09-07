using System.Diagnostics;
using gd.Util;
using Microsoft.AspNetCore.Mvc;
using NLog;
using ssoproxy.Models;
using ssoproxy.Services;
using ssoproxy.Services.Auth;

namespace ssoproxy.Controllers;

public class HomeController : Controller
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ILoginService _loginService;
    private readonly IConfiguration _configuration;

    public HomeController(ILoginService loginService, IConfiguration configuration)
    {
        _loginService = loginService;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("/Home/SsoLogin")]
    public IActionResult SsoLogin(string? returnUrl)
    {
        ViewData["ReturnUrl"] = NormalizeReturnUrl(returnUrl);
        return View();
    }

    [HttpPost("/Login/SubmitSsoLogin")]
    public async Task<IActionResult> SubmitSsoLogin(string username, string password, string captchaValue, string captchaInput, string? returnUrl)
    {
        Profiler profiler = new Profiler();
        _logger.Trace(profiler.StartLog("Processing SSO login request. username={Username}, returnUrl={ReturnUrl}"), username, returnUrl);
        // 1. 後端進行圖形驗證碼二次校驗
        if (string.IsNullOrEmpty(captchaInput) || !captchaInput.Equals(captchaValue, StringComparison.OrdinalIgnoreCase))
        {            
            TempData["ErrorMessage"] = "圖形驗證碼比對錯誤，請重新輸入！";
            _logger.Info(profiler.StopLog("[SSO_LOGIN_CAPTCHA_ERROR] CAPTCHA mismatch for user {Username} (captchaInput: {CaptchaInput}, captchaValue: {CaptchaValue}); please try again."), username, captchaInput, captchaValue);
            return RedirectToAction("SsoLogin", new { returnUrl });
        }

        // 2. 使用 LoginService 進行帳號密碼驗證與 token 產生
        var result = await _loginService.AuthenticateAsync(username, password);

        if (result.Success)
        {            
            TempData["Token"] = result.Token;
            Response.Cookies.Append("sso_token", result.Token!, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(result.ExpiresInMinutes),
                Path = "/",
                Domain = GetCookieDomain()
            });
            var targetUrl = NormalizeReturnUrl(returnUrl);
            _logger.Info(profiler.StopLog("[SSO_LOGIN_SUCCESS] User {Username} logged in successfully; token created, target URL: {TargetUrl}"), username, targetUrl);
            return Redirect(targetUrl);
        }
        _logger.Warn(profiler.StopLog("[SSO_LOGIN_FAILURE] User {Username} login failed: {ErrorMessage}"), username, result.ErrorMessage);
        TempData["ErrorMessage"] = result.ErrorMessage ?? "帳號或密碼錯誤！";
        return RedirectToAction("SsoLogin", new { returnUrl });
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {        
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (returnUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            returnUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        if (!returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return $"/{returnUrl}";
        }

        return returnUrl;
    }

    private string? GetCookieDomain()
    {
        var configuredDomain = _configuration["SsoCookieDomain"]?.Trim();
        if (string.IsNullOrWhiteSpace(configuredDomain))
        {
            return null;
        }

        var domain = configuredDomain.TrimStart('.');
        var host = Request.Host.Host;
        return host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase)
            ? configuredDomain
            : null;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
