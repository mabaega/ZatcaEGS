using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zatca.eInvoice.Helpers;
using ZatcaEGS.Helpers;

namespace ZatcaEGS.Models
{
    //public enum EnvironmentType
    //{
    //    NonProduction,
    //    Simulation,
    //    Production
    //}

    public class DeviceSetup
    {
        [Key]
        public int RowId { get; set; }

        //[Required(ErrorMessage = "Apie Access EndPoint is required.")]
        //[Display(Name = "Api EndPoint")]
        //public string ApiEndPoint { get; set; }


        [Required(ErrorMessage = "Api Access Token is required.")]
        [Display(Name = "Api Access Token")]
        public string AccessToken { get; set; }


        [Required(ErrorMessage = "Invoice Subtype GUID is required.")]
        [Display(Name = "Invoice Subtype GUID")]
        [GuidValidation(ErrorMessage = "Invoice Subtype GUID must be a valid GUID.")]
        public string InvoiceSubTypeGuid { get; set; }

        [Required(ErrorMessage = "Item Tax Category GUID is required.")]
        [Display(Name = "Item Tax Category GUID")]
        [GuidValidation(ErrorMessage = "Item Tax Category GUID must be a valid GUID.")]
        public string ItemTaxCategoryGuid { get; set; }

        [Required(ErrorMessage = "Payment Means Code GUID is required.")]
        [Display(Name = "Payment Means Code GUID")]
        [GuidValidation(ErrorMessage = "Payment Means Code GUID must be a valid GUID.")]
        public string PaymentMeansCodeGuid { get; set; }

        [Required(ErrorMessage = "Instruction Note GUID is required.")]
        [Display(Name = "Instruction Note GUID")]
        [GuidValidation(ErrorMessage = "Instruction Note GUID must be a valid GUID.")]
        public string InstructionNoteGuid { get; set; }

        [Required(ErrorMessage = "Party Tax Info GUID is required.")]
        [Display(Name = "Party Tax Info GUID")]
        [GuidValidation(ErrorMessage = "Party Tax Info GUID must be a valid GUID.")]
        public string PartyTaxInfoGuid { get; set; }

        [Required(ErrorMessage = "QR Code GUID is required.")]
        [Display(Name = "QR Code GUID")]
        [GuidValidation(ErrorMessage = "QR Code GUID must be a valid GUID.")]
        public string QrCodeGuid { get; set; }


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

        [Required(ErrorMessage = "CSR Type is required.")]
        [Display(Name = "CSR Type")]
        public EnvironmentType EnvironmentType { get; set; }

        //[Required(ErrorMessage = "CSR Common Name is required.")]
        [Display(Name = "CSR Common Name")]
        public string CsrCommonName { get; set; }

        //[Required(ErrorMessage = "CSR Serial Number is required.")]
        [Display(Name = "CSR Serial Number")]
        public string CsrSerialNumber { get; set; }

        //[Required(ErrorMessage = "CSR Organization Identifier is required.")]
        [Display(Name = "CSR Organization Identifier")]
        public string CsrOrganizationIdentifier { get; set; }

        //[Required(ErrorMessage = "CSR Organization Unit Name is required.")]
        [Display(Name = "CSR Organization Unit Name")]
        public string CsrOrganizationUnitName { get; set; }

        //[Required(ErrorMessage = "CSR Organization Name is required.")]
        [Display(Name = "CSR Organization Name")]
        public string CsrOrganizationName { get; set; }

        //[Required(ErrorMessage = "CSR Country Name is required.")]
        [Display(Name = "CSR Country Name")]
        public string CsrCountryName { get; set; }

        //[Required(ErrorMessage = "CSR Invoice Type is required.")]
        [Display(Name = "CSR Invoice Type")]
        public string CsrInvoiceType { get; set; }

        //[Required(ErrorMessage = "CSR Location Address is required.")]
        [Display(Name = "CSR Location Address")]
        public string CsrLocationAddress { get; set; }

        //[Required(ErrorMessage = "CSR Industry Business Category is required.")]
        [Display(Name = "CSR Industry Business Category")]
        public string CsrIndustryBusinessCategory { get; set; }

        //[Required(ErrorMessage = "CSR is required.")]
        [Display(Name = "GeneratedCSR")]
        public string GeneratedCSR { get; set; }

        //[Required(ErrorMessage = "EC Secp256k1 Private Key (PEM) is required.")]
        [Display(Name = "EC Secp256k1 Private Key (PEM)")]
        public string EcSecp256k1Privkeypem { get; set; }


        // Step 4 Get Compliance CSID and PCSID

        //[Required(ErrorMessage = "Compliance CSID URL is required.")]
        [Display(Name = "Compliance CSID URL")]
        public string ComplianceCSIDUrl { get; set; }

        //[Required(ErrorMessage = "Production CSID URL is required.")]
        [Display(Name = "Production CSID URL")]
        public string ProductionCSIDUrl { get; set; }

        //[Required(ErrorMessage = "Reporting URL is required.")]
        [Display(Name = "Reporting URL")]
        public string ReportingUrl { get; set; }

        //[Required(ErrorMessage = "Clearance URL is required.")]
        [Display(Name = "Clearance URL")]
        public string ClearanceUrl { get; set; }

        // [Required(ErrorMessage = "Compliance Check URL is required.")]
        [Display(Name = "Compliance Check URL")]
        public string ComplianceCheckUrl { get; set; }


        // [Required(ErrorMessage = "CCSID Compliance Request ID is required.")]
        [Display(Name = "CCSID Compliance Request ID")]
        public string CCSIDComplianceRequestId { get; set; }

        // [Required(ErrorMessage = "CCSID Binary Token is required.")]
        [Display(Name = "CCSID Binary Token")]
        public string CCSIDBinaryToken { get; set; }

        //[Required(ErrorMessage = "CCSID Secret is required.")]
        [Display(Name = "CCSID Secret")]
        public string CCSIDSecret { get; set; }

        //[Required(ErrorMessage = "PCSID Binary Token is required.")]
        [Display(Name = "PCSID Binary Token")]
        public string PCSIDBinaryToken { get; set; }

        //[Required(ErrorMessage = "PCSID Secret is required.")]
        [Display(Name = "PCSID Secret")]
        public string PCSIDSecret { get; set; }

        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        [Display(Name = "Registered Date")]
        public DateTime RegisteredDate { get; set; }

        public string BusinessDatabaseGuid() => "38cf4712-6e95-4ce1-b53a-bff03edad273";
        public string BaseCurrencyGuid() => "39dde4fc-7af8-44cc-8572-3b1ff4cfb918";
        public bool IsRegistered() => !string.IsNullOrEmpty(CCSIDComplianceRequestId) && !string.IsNullOrEmpty(CCSIDBinaryToken) && !string.IsNullOrEmpty(CCSIDSecret) && !string.IsNullOrEmpty(PCSIDBinaryToken) && !string.IsNullOrEmpty(PCSIDSecret);

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
