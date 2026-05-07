using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TCS.Web.Controllers;

[Authorize]
public class TrainingHeaderController : Controller
{
    public IActionResult Index() => View();
}
