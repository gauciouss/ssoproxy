using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ssoproxymgr.Controllers;

[Authorize]
public class ProxyController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
