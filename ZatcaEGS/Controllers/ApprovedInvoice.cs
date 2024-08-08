using Microsoft.AspNetCore.Mvc;
using ZatcaEGS.Helpers;
using ZatcaEGS.Models;

namespace ZatcaEGS.Controllers
{
    public class ApprovedInvoiceController : Controller
    {
        private readonly AppDbContext _context;

        public ApprovedInvoiceController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var models = _context.ApprovedInvoices.OrderByDescending(e => e.Timestamp).ToList();
            foreach (var item in models)
            {
                item.ServerResult = DocumentFormatter.ExcludeClearanceInvoice(item.ServerResult);
            }
            return View(models);
        }
    }
}
