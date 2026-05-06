using Microsoft.AspNetCore.Mvc;

namespace DingxinErp.Web.Controllers;

/// <summary>
/// 範例作業 MVC 頁面 Controller — 提供 View 頁面
/// </summary>
public class SamplePageController : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/Sample/Index.cshtml");
    }
}
