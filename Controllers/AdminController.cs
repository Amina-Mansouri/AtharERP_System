using Microsoft.AspNetCore.Mvc;

namespace AtharERP_System.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
