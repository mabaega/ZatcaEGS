namespace ZatcaEGS.Models
{
    public class PartyTaxInfo
    {
        // If the tax exemption reason code (BT-121) is equal to VATEX-SA-EDU or VATEX-SA-HEA, then the other buyer ID (BT-46) is mandatory  and must be national ID (BT-46-1 = NAT)

        //public string IdentificationScheme { get; set; } = "";
        //public string IdentificationID { get; set; } = "";

        // Postal
        public string StreetName { get; set; } = "";
        public string BuildingNumber { get; set; } = "";
        public string CitySubdivisionName { get; set; } = "";
        public string CityName { get; set; } = "";
        public string PostalZone { get; set; } = "";
        public string CountryIdentificationCode { get; set; } = "SA";

        // PartyTaxScheme
        public string CompanyID { get; set; } = "";
        public string TaxSchemeID { get; set; } = "VAT";

        // PartyLegalEntity
        public string RegistrationName { get; set; } = "";
        public string CurrencyCode { get; set; } = "SAR";
    }
}