 using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ZatcaEGS.Models;

namespace ZatcaEGS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _dbContext;
        public HomeController(AppDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            DeviceSetup _deviceSetup = _dbContext.DeviceSetups.OrderBy(x => x.RowId).FirstOrDefault() ?? new DeviceSetup();
            return View(_deviceSetup);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
