using Microsoft.AspNetCore.Mvc;

namespace EAPD7111_PART2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("Home/Error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
