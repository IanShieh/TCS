using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TCS.Web.Controllers;

[Authorize]
public class LicenseController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Create() => View();
    public IActionResult Edit(string licenseType) => View((object)licenseType);
}
