using Microsoft.AspNetCore.Mvc;

namespace DingxinErp.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
