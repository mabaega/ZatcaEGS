using Newtonsoft.Json;
using Zatca.eInvoice.Helpers;
using ZatcaEGS.Helpers;

namespace ZatcaEGS.Models
{
    public class RelayData
    {
        public string Referrer { get; set; }
        public string Key { get; set; }
        public string Data { get; set; }
        public string Callback { get; set; }
        public string InvoiceJson { get; set; }
        public ManagerInvoice ManagerInvoice { get; set; }
        public string CertInfoString { get; set; }
        public PartyTaxInfo PartyInfo { get; set; }
        public string ApprovalStatus { get; set; }
        public string Base64QrCode { get; set; }
        public string ZatcaUUID { get; set; }
        public EnvironmentType EnvironmentType { get; set; }
        public int LastICV { get; set; } = 0;
        public string LastPIH { get; set; } = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";

        public string DateCreated { get; set; }
        public string BaseCurrency { get; set; }
        public string BusinessDetails { get; set; }

        public RelayData() { }

        public RelayData(Dictionary<string, string> formData)
        {
            Referrer = formData.GetValueOrDefault("Referrer");
            Key = formData.GetValueOrDefault("Key");
            Callback = formData.GetValueOrDefault("Callback");
            Data = formData.GetValueOrDefault("Data");

            string DataString = JsonParser.UpdateJsonGuidValue(Data, ManagerCustomField.ZatcaUUIDGuid);

            var (BaseCurrency, BusinessDetails, dynamicParts) = JsonParser.ParseJson(DataString);

            var AccessToken = JsonParser.FindStringByGuid(BusinessDetails, ManagerCustomField.TokenInfoGuid);

            var icv = JsonParser.FindStringByGuid(BusinessDetails, ManagerCustomField.LastIcvGuid) ?? LastICV.ToString();

            LastICV = int.TryParse(icv, out int icvNumber) ? icvNumber : 0;
            LastPIH = JsonParser.FindStringByGuid(BusinessDetails, ManagerCustomField.TokenInfoGuid) ?? LastPIH;

            var certString = JsonParser.FindStringByGuid(BusinessDetails, ManagerCustomField.CertificateInfoGuid);

            if (!string.IsNullOrEmpty(certString))
            {
                CertificateInfo certificateInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(certString);

                if (certificateInfo != null)
                {
                    certificateInfo.ApiSecret = AccessToken;
                    certificateInfo.ApiEndpoint = UrlHelper.GetApiEndpoint(Referrer);
                    EnvironmentType = certificateInfo.EnvironmentType;

                    CertInfoString = ObjectCompressor.SerializeToBase64String(certificateInfo);
                }
            }

            // Retrieve the JSON string associated with a specific GUID
            InvoiceJson = dynamicParts.GetValueOrDefault(Key);

            if (InvoiceJson != null)
            {
                InvoiceJson = InvoiceJson.Replace("Customer", "InvoiceParty")
                                             .Replace("Supplier", "InvoiceParty")
                                             .Replace("SalesInvoice", "RefInvoice")
                                             .Replace("PurchaseInvoice", "RefInvoice")
                                             .Replace("SalesUnitPrice", "UnitPrice")
                                             .Replace("PurchaseUnitPrice", "UnitPrice");

                // Merge json
                InvoiceJson = JsonParser.ReplaceGuidValuesInJson(InvoiceJson, dynamicParts);
                InvoiceJson = JsonParser.ReplaceGuidValuesInJson(InvoiceJson, dynamicParts);

                ManagerInvoice = JsonConvert.DeserializeObject<ManagerInvoice>(InvoiceJson);

                PartyInfo = new PartyTaxInfo()
                {
                    StreetName = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.StreetName, "RefInvoice"),
                    BuildingNumber = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.BuildingNumber, "RefInvoice"),
                    CitySubdivisionName = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.CitySubdivisionName, "RefInvoice"),
                    CityName = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.CityName, "RefInvoice"),
                    PostalZone = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.PostalZone, "RefInvoice"),
                    CountryIdentificationCode = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.CountryIdentificationCode, "RefInvoice"),
                    CompanyID = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.CompanyID, "RefInvoice"),
                    TaxSchemeID = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.TaxSchemeID, "RefInvoice"),
                    RegistrationName = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.RegistrationName, "RefInvoice"),
                    CurrencyCode = JsonParser.FindStringByGuid(InvoiceJson, "Code", "RefInvoice")
                };

                Base64QrCode = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.QrCodeGuid, "RefInvoice");
                DateCreated = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.DateCreatedGuid, "RefInvoice");

                if (!string.IsNullOrEmpty(Base64QrCode))
                {
                    ApprovalStatus = JsonParser.FindStringByGuid(InvoiceJson, ManagerCustomField.ApprovedInvoiceGuid, "RefInvoice");

                    ZatcaUUID = JsonParser.FindStringByGuid(InvoiceJson, Key, ManagerCustomField.ZatcaUUIDGuid);

                    if (!string.IsNullOrEmpty(ZatcaUUID) && ZatcaUUID.Contains('#'))
                    {
                        ZatcaUUID = ZatcaUUID.Replace("#", "");
                    }
                }
            }
            else
            {
                //Console.WriteLine($"GUID {Key} not found in dynamicParts.");
            }

        }
    }

    public class Currency
    {
        public string Code { get; set; } = "SAR";
        public string Name { get; set; }
        public string Symbol { get; set; }
    }

    public class CustomFields2
    {
        public Dictionary<string, string> Strings { get; set; } = [];
        public Dictionary<string, decimal> Decimals { get; set; } = [];
        public Dictionary<string, DateTime?> Dates { get; set; } = [];
        public Dictionary<string, bool> Booleans { get; set; } = [];
        public Dictionary<string, List<string>> StringArrays { get; set; } = [];
    }
    public class InvoiceParty
    {
        public string Name { get; set; }
        public Currency Currency { get; set; }
        public CustomFields2 CustomFields2 { get; set; }
    }

    public class LineItem
    {
        public string ItemCode { get; set; }
        public string Name { get; set; }
        public string ItemName { get; set; }
        public string UnitName { get; set; }
        public bool HasDefaultLineDescription { get; set; } = false;
        public string DefaultLineDescription { get; set; }
        public CustomFields2 CustomFields2 { get; set; }
    }

    public class TaxCode
    {
        public string Name { get; set; }
        public int TaxRate { get; set; }
        public double Rate { get; set; } = 0;
    }

    public class Line
    {
        public LineItem Item { get; set; }
        public string LineDescription { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }
        public CustomFields2 CustomFields2 { get; set; }
        public double Qty { get; set; } = 0;
        public double UnitPrice { get; set; } = 0;
        public double DiscountAmount { get; set; } = 0;
        public TaxCode TaxCode { get; set; }
    }

    public class RefInvoice
    {
        public string Reference { get; set; }
    }

    public class ManagerInvoice
    {
        public DateTime IssueDate { get; set; }
        public DateTime DueDateDate { get; set; }

        public string Reference { get; set; }
        public RefInvoice RefInvoice { get; set; }
        public InvoiceParty InvoiceParty { get; set; }
        public string BillingAddress { get; set; }
        public double ExchangeRate { get; set; } = 1;
        public string Description { get; set; }
        public List<Line> Lines { get; set; }
        public bool HasLineNumber { get; set; } = false;
        public bool HasLineDescription { get; set; } = false;
        public bool Discount { get; set; } = false;
        public bool AmountsIncludeTax { get; set; } = false;
        public CustomFields2 CustomFields2 { get; set; }
    }
}