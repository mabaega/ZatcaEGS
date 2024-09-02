using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Zatca.EGS.Controllers
{
    public class DisclaimerController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Agree()
        {
            HttpContext.Session.SetString("disclaimerSeen", "true");
            return RedirectToAction("Index", "Wizard");
        }
    }
}