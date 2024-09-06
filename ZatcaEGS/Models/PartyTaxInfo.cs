namespace ZatcaEGS.Models
{
    public class PartyTaxInfo
    {
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