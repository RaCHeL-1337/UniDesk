using Microsoft.AspNetCore.Mvc;

namespace ASP.Controllers
{
    public class TicketsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
