namespace ZatcaEGS.Models
{
    public class PartyTaxInfo
    {
        //PartyIdentification
        //public string ID { get; set; } = "1010010000";

        //[JsonPropertyName("schemeID")]
        //public string SchemeID { get; set; } = "CRN";

        // Postal
        public string StreetName { get; set; } = "الامير سلطان | Prince Sultan";
        public string BuildingNumber { get; set; } = "2322";
        public string CitySubdivisionName { get; set; } = "المربع | Al-Murabba";
        public string CityName { get; set; } = "الرياض | Riyadh";
        public string PostalZone { get; set; } = "23333";
        public string CountryIdentificationCode { get; set; } = "SA";

        // PartyTaxScheme
        public string CompanyID { get; set; } = "399999999800003";
        public string TaxSchemeID { get; set; } = "VAT";

        // PartyLegalEntity
        public string RegistrationName { get; set; } = "شركة توريد التكنولوجيا بأقصى سرعة المحدودة | Maximum Speed Tech Supply LTD";
    }
}