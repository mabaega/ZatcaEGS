namespace ZatcaEGS.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string ErrorMessage { get; set; } // Properti baru untuk pesan error
        public string ReferrerLink { get; set; } // Properti baru untuk link referrer
    }
}
