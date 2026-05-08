using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TCS.Web.Controllers;

[Authorize]
public class TrainingController : Controller
{
    public IActionResult Index() => View();
}
