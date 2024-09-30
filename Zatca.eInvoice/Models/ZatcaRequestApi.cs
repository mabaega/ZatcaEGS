
namespace Zatca.eInvoice.Models
{
    public class SignedInvoiceResult
    {
        public string InvoiceHash { get; set; }
        public string Base64SignedInvoice { get; set; }
        public string Base64QrCode { get; set; }
        public string XmlFileName { get; set; }
        public ZatcaRequestApi RequestApi { get; set; }
    }
    public class ZatcaRequestApi
    {
        public string invoiceHash { get; set; }
        public string uuid { get; set; }
        public string invoice { get; set; }
    }
}
