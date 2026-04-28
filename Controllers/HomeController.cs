using Microsoft.AspNetCore.Mvc;

namespace AudioClassificationAPI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
