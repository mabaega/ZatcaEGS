using Microsoft.AspNetCore.Mvc;

namespace ZatcaEGS.Controllers
{
    public class DisclaimerController : Controller
    {
        //[HttpGet]
        //public IActionResult Index()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public IActionResult Agree()
        //{
        //    HttpContext.Session.SetString("disclaimerSeen", "true");
        //    return RedirectToAction("Index", "Wizard");
        //}

        [HttpGet]
        public IActionResult Index()
        {
            // Periksa apakah cookie 'disclaimerSeen' ada
            var disclaimerSeen = Request.Cookies["disclaimerSeen"];
            if (disclaimerSeen == "true")
            {
                return RedirectToAction("Index", "Wizard");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Agree()
        {
            // Set cookie 'disclaimerSeen' dengan nilai "true"
            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(1) // Atur waktu kadaluarsa cookie (misal: 30 hari)
            };
            Response.Cookies.Append("disclaimerSeen", "true", options);

            return RedirectToAction("Index", "Wizard");
        }
    }
}
