using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace ssoproxymgr.Controllers;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public AccountController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Proxy");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        var expectedUsername = _configuration["ManagerAuth:Username"];
        var expectedPassword = _configuration["ManagerAuth:Password"];

        if (string.Equals(username, expectedUsername, StringComparison.Ordinal) &&
            string.Equals(password, expectedPassword, StringComparison.Ordinal))
        {            
            var proxyToken = await GetSsoProxyTokenAsync();
            _logger.Debug("ssoproxy token: {}", proxyToken);
            if (string.IsNullOrWhiteSpace(proxyToken))
            {
                ViewData["ReturnUrl"] = returnUrl;
                ViewBag.ErrorMessage = "登入成功，但無法取得 ssoproxy API Token。";
                return View();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("SsoproxyAccessToken", proxyToken)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            var redirectUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action("Index", "/") ?? "/"
                : returnUrl;

            return LocalRedirect(redirectUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewBag.ErrorMessage = "帳號或密碼錯誤。";
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task<string?> GetSsoProxyTokenAsync()
    {
        var ssoproxyApiBaseUrl = _configuration["SsoproxyApiBaseUrl"] ?? "http://localhost";
        var username = _configuration["ManagerAuth:Username"] ?? "admin";
        var password = _configuration["ManagerAuth:Password"] ?? "admin123";

        var endpoints = new[]
        {
            //$"{ssoproxyApiBaseUrl.TrimEnd('/')}/api/Auth",
            $"{ssoproxyApiBaseUrl.TrimEnd('/')}/api/Auth/token",
            //$"{ssoproxyApiBaseUrl.TrimEnd('/')}/api/Token"
        };

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        foreach (var endpoint in endpoints)
        {
            try
            {
                using var response = await client.PostAsJsonAsync(endpoint, new { Username = username, Password = password });
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var payload = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(payload);

                if (doc.RootElement.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.Object)
                {
                    if (body.TryGetProperty("Token", out var token) && token.ValueKind == JsonValueKind.String)
                    {
                        return token.GetString();
                    }

                    if (body.TryGetProperty("token", out var lowerToken) && lowerToken.ValueKind == JsonValueKind.String)
                    {
                        return lowerToken.GetString();
                    }
                }

                if (doc.RootElement.TryGetProperty("Token", out var rootToken) && rootToken.ValueKind == JsonValueKind.String)
                {
                    return rootToken.GetString();
                }

                if (doc.RootElement.TryGetProperty("token", out var rootLowerToken) && rootLowerToken.ValueKind == JsonValueKind.String)
                {
                    return rootLowerToken.GetString();
                }
            }
            catch
            {
                // Ignore and continue to the next endpoint.
            }
        }

        return null;
    }
}
