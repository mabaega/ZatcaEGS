namespace EGS.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string ErrorMessage { get; set; } 
        public string ReferrerLink { get; set; } 
        public bool ShowSetupLink { get; set; }
    }
}
