using System.ComponentModel.DataAnnotations;
using Zatca.eInvoice.Helpers;

namespace ZatcaEGS.Models
{
    public class CertificateInfo
    {
        [Display(Name = "Api Endpoint")]
        public string ApiEndpoint { get; set; } = "";

        [Display(Name = "Api Secret")]
        public string ApiSecret { get; set; } = "";

        // Step 2 Business Info
        [Required(ErrorMessage = "Business Identification ID is required.")]
        [Display(Name = "Identification ID")]
        public string IdentificationID { get; set; }

        [Required(ErrorMessage = "Identification Scheme is required.")]
        [Display(Name = "Identification Scheme")]
        public string IdentificationScheme { get; set; }

        [Required(ErrorMessage = "Street Name is required.")]
        [Display(Name = "Street Name")]
        public string StreetName { get; set; }

        [Required(ErrorMessage = "Building Number is required.")]
        [Display(Name = "Building Number")]
        public string BuildingNumber { get; set; }

        [Required(ErrorMessage = "City Subdivision Name is required.")]
        [Display(Name = "City Subdivision Name")]
        public string CitySubdivisionName { get; set; }

        [Required(ErrorMessage = "City Name is required.")]
        [Display(Name = "City Name")]
        public string CityName { get; set; }

        [Required(ErrorMessage = "Postal Zone is required.")]
        [Display(Name = "Postal Zone")]
        public string PostalZone { get; set; }

        [Required(ErrorMessage = "Country Identification Code is required.")]
        [Display(Name = "Country Identification Code")]
        public string CountryIdentificationCode { get; set; } = "SA";

        [Required(ErrorMessage = "Company ID is required.")]
        [Display(Name = "Company ID")]
        public string CompanyID { get; set; }

        [Required(ErrorMessage = "Tax Scheme ID is required.")]
        [Display(Name = "Tax Scheme ID")]
        public string TaxSchemeID { get; set; }

        [Required(ErrorMessage = "Registration Name is required.")]
        [Display(Name = "Registration Name")]
        public string RegistrationName { get; set; }

        [Required(ErrorMessage = "Business Category Name is required.")]
        [Display(Name = "Business Category")]
        public string BusinessCategory { get; set; }

        // Step 3 Generate CSR and Private Key

        [Display(Name = "CSR Common Name")]
        public string CsrCommonName { get; set; }

        [Display(Name = "CSR Serial Number")]
        public string CsrSerialNumber { get; set; }

        [Display(Name = "CSR Organization Identifier")]
        public string CsrOrganizationIdentifier { get; set; }

        [Display(Name = "CSR Organization Unit Name")]
        public string CsrOrganizationUnitName { get; set; }

        [Display(Name = "CSR Organization Name")]
        public string CsrOrganizationName { get; set; }

        [Display(Name = "CSR Country Name")]
        public string CsrCountryName { get; set; }

        [Display(Name = "CSR Invoice Type")]
        public string CsrInvoiceType { get; set; }

        [Display(Name = "CSR Location Address")]
        public string CsrLocationAddress { get; set; }

        [Display(Name = "CSR Industry Business Category")]
        public string CsrIndustryBusinessCategory { get; set; }

        [Required(ErrorMessage = "CSR Type is required.")]
        [Display(Name = "CSR Type")]
        public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.NonProduction;

        [Display(Name = "GeneratedCSR")]
        public string GeneratedCSR { get; set; }

        [Display(Name = "EC Secp256k1 Private Key (PEM)")]
        public string EcSecp256k1Privkeypem { get; set; }

        // Step 4 Get Compliance CSID and PCSID

        [Display(Name = "CCSID Compliance Request ID")]
        public string CCSIDComplianceRequestId { get; set; }

        [Display(Name = "CCSID Binary Token")]
        public string CCSIDBinaryToken { get; set; }

        [Display(Name = "CCSID Secret")]
        public string CCSIDSecret { get; set; }

        [Display(Name = "PCSID Binary Token")]
        public string PCSIDBinaryToken { get; set; }

        [Display(Name = "PCSID Secret")]
        public string PCSIDSecret { get; set; }

        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        [Display(Name = "Registered Date")]
        public DateTime RegisteredDate { get; set; }

        [Display(Name = "Relay URL")]

        public string RelayURL { get; set; }

        public string ReportingUrl => GetUrl("invoices/reporting/single");
        public string ClearanceUrl => GetUrl("invoices/clearance/single");
        public string ComplianceCSIDUrl => GetUrl("compliance");
        public string ComplianceCheckUrl => GetUrl("compliance/invoices");
        public string ProductionCSIDUrl => GetUrl("production/csids");
        private string GetUrl(string endpoint)
        {
            string environment = EnvironmentType switch
            {
                EnvironmentType.NonProduction => "developer-portal",
                EnvironmentType.Simulation => "simulation",
                EnvironmentType.Production => "core",
                _ => "developer-portal"
            };

            return $"https://gw-fatoora.zatca.gov.sa/e-invoicing/{environment}/{endpoint}";
        }

        public bool IsRegistered() => !string.IsNullOrEmpty(CCSIDComplianceRequestId) && !string.IsNullOrEmpty(CCSIDBinaryToken) && !string.IsNullOrEmpty(CCSIDSecret) && !string.IsNullOrEmpty(PCSIDBinaryToken) && !string.IsNullOrEmpty(PCSIDSecret);
    }

    public class AccessTokenDto
    {
        public string ApiEndpoint { get; set; }
        public string ApiSecret { get; set; }
    }
    public class CCSIDResultDto
    {
        public string CCSIDComplianceRequestId { get; set; }
        public string CCSIDBinaryToken { get; set; }
        public string CCSIDSecret { get; set; }
    }

    public class PCSIDResultDto
    {
        public string PCSIDBinaryToken { get; set; }
        public string PCSIDSecret { get; set; }
        public DateTime RegisteredDate { get; set; }
    }

    public class ZatcaResultDto
    {
        public string RequestID { get; set; }
        public string TokenType { get; set; }
        public string DispositionMessage { get; set; }
        public string BinarySecurityToken { get; set; }
        public string Secret { get; set; }
        public List<string> Errors { get; set; }
    }
}
