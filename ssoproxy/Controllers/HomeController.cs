using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NLog;
using ssoproxy.Models;
using ssoproxy.Services;

namespace ssoproxy.Controllers;

public class HomeController : Controller
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ILoginService _loginService;

    public HomeController(ILoginService loginService)
    {
        _loginService = loginService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult SsoLogin()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SubmitSsoLogin(string username, string password, string captchaValue, string captchaInput)
    {
        // 1. 後端進行圖形驗證碼二次校驗
        if (string.IsNullOrEmpty(captchaInput) || !captchaInput.Equals(captchaValue, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "圖形驗證碼比對錯誤，請重新輸入！";
            return RedirectToAction("SsoLogin");
        }

        // 2. 使用 LoginService 進行帳號密碼驗證與 token 產生
        var result = await _loginService.AuthenticateAsync(username, password);

        if (result.Success)
        {
            _logger.Info("[SSO_LOGIN_SUCCESS] 使用者 {Username} 成功登入系統，Token 已產生", username);
            TempData["Token"] = result.Token;
            return RedirectToAction("Index");
        }

        TempData["ErrorMessage"] = result.ErrorMessage ?? "帳號或密碼錯誤！";
        return RedirectToAction("SsoLogin");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
