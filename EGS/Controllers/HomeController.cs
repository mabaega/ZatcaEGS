 using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using EGS.Models;

namespace EGS.Controllers
{
    public class HomeController : Controller
    {
        //private readonly AppDbContext _dbContext;
        //public HomeController(AppDbContext dbContext) 
        //{
        //    _dbContext = dbContext;
        //}

        public IActionResult Index()
        {
            //CertificateInfo _certInfo = _dbContext.DeviceSetups.OrderBy(x => x.RowId).FirstOrDefault() ?? new CertificateInfo();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
