
namespace ZatcaEGS.Models
{
    public class SetupViewModel
    {
        public string Referrer { get; set; }
        public string BusinessDetails { get; set; }
        public string BusinessDetailsJson { get; set; }
        public string Api { get; set; }
        public string Token { get; set; }
        public bool IsFileReady { get; set; }
        public string FileContent { get; set; } // Store as Base64 string instead of IFormFile
        public string Filename { get; set; }
        public CertificateInfo CertificateInfo { get; set; }
    }
}
