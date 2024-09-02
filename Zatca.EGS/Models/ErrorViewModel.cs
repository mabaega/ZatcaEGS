namespace Zatca.EGS.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string ErrorMessage { get; set; } 
        public string ReferrerLink { get; set; } 
        public bool ShowSetupLink { get; set; }
    }

    public class RelayViewModel
    {
        public string ZatcaUUID { get; set; }
        public string Base64QrCode { get; set; }
        public string ReferrerLink { get; set; }
        public bool ShowSetupLink { get; set; }
    }
    public class CertificateViewModel
    {
        public string ReferrerLink { get; set; }
        public bool ShowSetupLink { get; set; }
    }
    
}
