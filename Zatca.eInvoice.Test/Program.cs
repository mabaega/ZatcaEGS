using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Zatca.eInvoice;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;
using Zatca.eInvoice.Test;

//This code demonstrates how to use the Zatca.e Invoice Library.
public class ZatcaService
{
    private const string ComplianceCSIDUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance";
    private const string ProductionCSIDUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/production/csids";
    private const string ComplianceCheckUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance/invoices";
    private const string ReportingUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/reporting/single";
    private const string ClearanceUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/clearance/single";

    private readonly HttpClient _httpClient;

    public ZatcaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OnboardingResultDto> OnboardingDevice()
    {
        var onboardingResult = new OnboardingResultDto();

        try
        {
            // Step 1: Create CSR
            var csrGenerationDto = new CsrGenerationDto
            {
                CommonName = "TST-886431145-399999999900003",
                SerialNumber = "1-TST|2-TST|3-ed22f1d8-e6a2-1118-9b58-d9a8f11e445f",
                OrganizationIdentifier = "399999999900003",
                OrganizationUnitName = "Riyadh Branch",
                OrganizationName = "Maximum Speed Tech Supply LTD",
                CountryName = "SA",
                InvoiceType = "1100",
                LocationAddress = "RRRD2929",
                IndustryBusinessCategory = "Supply activities"
            };

            var csrGenerator = new CsrGenerator();
            var (generatedCsr, privateKey, errorMessages) = csrGenerator.GenerateCsrAndPrivateKey(csrGenerationDto, EnvironmentType.NonProduction, false);

            onboardingResult.GeneratedCsr = generatedCsr;
            onboardingResult.PrivateKey = privateKey;

            // Step 2: Get CCSID
            const string otp = "12345";
            var jsonContent = JsonConvert.SerializeObject(new { csr = generatedCsr });

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("OTP", otp);
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(ComplianceCSIDUrl, content);

            response.EnsureSuccessStatusCode();

            var resultContent = await response.Content.ReadAsStringAsync();
            var zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

            onboardingResult.CCSIDBinaryToken = zatcaResult.BinarySecurityToken;
            onboardingResult.CCSIDComplianceRequestId = zatcaResult.RequestID;
            onboardingResult.CCSIDSecret = zatcaResult.Secret;

            // Step 3: Get PCSID
            jsonContent = JsonConvert.SerializeObject(new { compliance_request_id = zatcaResult.RequestID });

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{zatcaResult.BinarySecurityToken}:{zatcaResult.Secret}")));

            content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(ProductionCSIDUrl, content);

            response.EnsureSuccessStatusCode();

            resultContent = await response.Content.ReadAsStringAsync();
            zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

            onboardingResult.PCSIDBinaryToken = zatcaResult.BinarySecurityToken;
            onboardingResult.PCSIDSecret = zatcaResult.Secret;

            return onboardingResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during onboarding: {ex.Message}");
            // Log full exception details
            throw;
        }
    }


    public async Task<SignedInvoiceResult> CreateSignedInvoice(string CSIDBinaryToken, string EcSecp256k1Privkeypem)
    {
        try
        {
            var invoiceObject = new Invoice
            {
                ProfileID = "reporting:1.0",
                ID = new ID("SME00010"),
                UUID = "8e6000cf-1a98-4174-b3e7-b5d5954bc10d",
                IssueDate = "2022-08-17",
                IssueTime = "17:41:08",
                InvoiceTypeCode = new InvoiceTypeCode(InvoiceType.TaxInvoice, "0200000"),
                DocumentCurrencyCode = "SAR",
                TaxCurrencyCode = "SAR",
                Note = new Note() { LanguageID = "ar", Value = "ABC" },

                AdditionalDocumentReference =
                [
                    new() {
                    ID = new ID("ICV"),
                    UUID = "10"
                },
                new() {
                    ID = new ID("PIH"),
                    Attachment = new Attachment
                    {
                        EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObject
                        {
                            Value = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==",
                            MimeCode = "text/plain"
                        }
                    }
                },
            ],

                AccountingSupplierParty = new AccountingSupplierParty
                {
                    Party = new()
                    {
                        PartyIdentification = new PartyIdentification
                        {
                            ID = new ID("CRN", null, "1010010000")
                        },
                        PostalAddress = new PostalAddress
                        {
                            StreetName = "الامير سلطان | Prince Sultan",
                            BuildingNumber = "2322",
                            CitySubdivisionName = "المربع | Al-Murabba",
                            CityName = "الرياض | Riyadh",
                            PostalZone = "23333",
                            Country = new Country("SA")
                        },
                        PartyTaxScheme = new PartyTaxScheme
                        {
                            CompanyID = "399999999900003",
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("VAT")
                            }
                        },
                        PartyLegalEntity = new PartyLegalEntity("شركة توريد التكنولوجيا بأقصى سرعة المحدودة | Maximum Speed Tech Supply LTD")
                    }
                },

                AccountingCustomerParty = new AccountingCustomerParty
                {
                    Party = new()
                    {
                        PostalAddress = new PostalAddress
                        {
                            StreetName = "صلاح الدين | Salah Al-Din",
                            BuildingNumber = "1111",
                            CitySubdivisionName = "المروج | Al-Murooj",
                            CityName = "الرياض | Riyadh",
                            PostalZone = "12222",
                            Country = new Country("SA")
                        },
                        PartyTaxScheme = new PartyTaxScheme
                        {
                            CompanyID = "399999999800003",
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("VAT")
                            }
                        },
                        PartyLegalEntity = new PartyLegalEntity("شركة نماذج فاتورة المحدودة | Fatoora Samples LTD")
                    }
                },

                PaymentMeans = new PaymentMeans { PaymentMeansCode = "10" },

                AllowanceCharge = new AllowanceCharge
                {
                    ChargeIndicator = false,
                    AllowanceChargeReason = "discount",
                    Amount = new Amount("SAR", 0.00),
                    TaxCategory =
                    [
                        new() {
                        ID = new ID("UN/ECE 5305", "6", "S"),
                        Percent = 15,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("UN/ECE 5153", "6", "VAT")
                        }
                    },
                    new() {
                        ID = new ID("UN/ECE 5305", "6", "S"),
                        Percent = 15,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("UN/ECE 5153", "6", "VAT")
                        }
                    }
                    ]
                },

                TaxTotal =
                [
                    new() {
                    TaxAmount = new Amount("SAR", 30.15),
                },
                new() {
                    TaxAmount = new Amount("SAR", 30.15),
                    TaxSubtotal =
                    [
                        new() {
                            TaxableAmount = new Amount("SAR", 201.00),
                            TaxAmount = new Amount("SAR", 30.15),
                            TaxCategory = new TaxCategory
                            {
                                ID = new ID("UN/ECE 5305", "6", "S"),
                                Percent = 15.00,
                                TaxScheme = new TaxScheme
                                {
                                    ID = new ID("UN/ECE 5153", "6", "VAT")
                                }
                            }
                        }
                    ]
                }
                ],

                LegalMonetaryTotal = new LegalMonetaryTotal
                {
                    LineExtensionAmount = new Amount("SAR", 201.00),
                    TaxExclusiveAmount = new Amount("SAR", 201.00),
                    TaxInclusiveAmount = new Amount("SAR", 231.15),
                    AllowanceTotalAmount = new Amount("SAR", 0.00),
                    PrepaidAmount = new Amount("SAR", 0.00),
                    PayableAmount = new Amount("SAR", 231.15),
                },

                InvoiceLine =
                [
                    new() {
                    ID = new ID("1"),
                    InvoicedQuantity = new InvoicedQuantity("PCE", 33.000000),
                    LineExtensionAmount = new Amount("SAR", 99.00),
                    TaxTotal = new TaxTotal
                    {
                        TaxAmount = new Amount("SAR", 14.85),
                        RoundingAmount = new Amount("SAR", 113.85)
                    },
                    Item = new Item
                    {
                        Name = "كتاب",
                        ClassifiedTaxCategory = new ClassifiedTaxCategory
                        {
                            ID = new ID("S"),
                            Percent = 15.00,
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("VAT")
                            }
                        }
                    },
                    Price = new Price
                    {
                        PriceAmount = new Amount("SAR", 3.00),
                        AllowanceCharge = new AllowanceCharge
                        {
                            ChargeIndicator = true,
                            AllowanceChargeReason = "discount",
                            Amount = new Amount("SAR", 0.00)
                        }
                    }
                },
                new() {
                    ID = new ID("2"),
                    InvoicedQuantity = new InvoicedQuantity("PCE", 3.000000),
                    LineExtensionAmount = new Amount("SAR", 102.00),
                    TaxTotal = new TaxTotal
                    {
                        TaxAmount = new Amount("SAR", 15.30),
                        RoundingAmount = new Amount("SAR", 117.30)
                    },
                    Item = new Item
                    {
                        Name = "قلم",
                        ClassifiedTaxCategory = new ClassifiedTaxCategory
                        {
                            ID = new ID("S"),
                            Percent = 15.00,
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("VAT")
                            }
                        }
                    },
                    Price = new Price
                    {
                        PriceAmount = new Amount("SAR", 34.00),
                        AllowanceCharge = new AllowanceCharge
                        {
                            ChargeIndicator = true,
                            AllowanceChargeReason = "discount",
                            Amount = new Amount("SAR", 0.00)
                        }
                    }
                }
                ]
            };

            InvoiceGenerator ig = new(
                invoiceObject,
                Encoding.UTF8.GetString(Convert.FromBase64String(CSIDBinaryToken)),
                EcSecp256k1Privkeypem
            );

            ig.GetSignedInvoiceXML(out string invoiceHash, out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out ZatcaRequestApi requestApi);

            return new SignedInvoiceResult
            {
                Base64SignedInvoice = base64SignedInvoice,
                Base64QrCode = base64QrCode,
                XmlFileName = XmlFileName,
                RequestApi = JsonConvert.SerializeObject(requestApi)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating signed invoice: {ex.Message}");
            throw;
        }
    }

    public async Task<ServerResult> ComplianceCheck(string ccsidBinaryToken, string ccsidSecret, string requestApi)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ccsidBinaryToken}:{ccsidSecret}")));

            var content = new StringContent(requestApi, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ComplianceCheckUrl, content);

            var resultContent = await response.Content.ReadAsStringAsync();
            var serverResult = JsonConvert.DeserializeObject<ServerResult>(resultContent);

            return serverResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during compliance check: {ex.Message}");
            throw;
        }
    }
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

public class OnboardingResultDto
{
    public string GeneratedCsr { get; set; }
    public string PrivateKey { get; set; }
    public string CCSIDComplianceRequestId { get; set; }
    public string CCSIDBinaryToken { get; set; }
    public string CCSIDSecret { get; set; }
    public string PCSIDBinaryToken { get; set; }
    public string PCSIDSecret { get; set; }
}

public class SignedInvoiceResult
{
    public string Base64SignedInvoice { get; set; }
    public string Base64QrCode { get; set; }
    public string XmlFileName { get; set; }
    public string RequestApi { get; set; }
}

public class ZatcaRequestApi
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("invoiceHash")]
    public string InvoiceHash { get; set; }

    [JsonProperty("invoice")]
    public string Invoice { get; set; }
}

public class ServerResult
{
    [JsonProperty("requestType")]
    public string RequestType { get; set; }

    [JsonProperty("statusCode")]
    public string StatusCode { get; set; }

    [JsonProperty("clearanceStatus")]
    public string ClearanceStatus { get; set; }

    [JsonProperty("reportingStatus")]
    public string ReportingStatus { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("uuid")]
    public string UUID { get; set; }

    [JsonProperty("reasonPhrase")]
    public string ReasonPhrase { get; set; }

    [JsonProperty("isSuccessStatusCode")]
    public string IsSuccessStatusCode { get; set; }

    [JsonProperty("validationResults")]
    public ValidationResult ValidationResults { get; set; }

    [JsonProperty("errorMessages")]
    public List<DetailInfo> ErrorMessages { get; set; }

    [JsonProperty("errors")]
    public List<DetailInfo> Errors { get; set; }

    [JsonProperty("warningMessages")]
    public List<DetailInfo> WarningMessages { get; set; }

    [JsonProperty("warnings")]
    public List<DetailInfo> Warnings { get; set; }

    [JsonProperty("clearedInvoice")]
    public string ClearedInvoice { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("invoiceHash")]
    public string InvoiceHash { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("qrBuyertStatus")]
    public string QrBuyerStatus { get; set; }

    [JsonProperty("qrSellertStatus")]
    public string QrSellerStatus { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

}

public class DetailInfo
{

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

}

public class ValidationResult
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("infoMessages")]
    public List<DetailInfo> InfoMessages { get; set; }

    [JsonProperty("warningMessages")]
    public List<DetailInfo> WarningMessages { get; set; }

    [JsonProperty("errorMessages")]
    public List<DetailInfo> ErrorMessages { get; set; }

}
class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        using (var httpClient = new HttpClient())
        {
            var zatcaService = new ZatcaService(httpClient);
            try
            {
                // Step 1: Onboarding
                //var result = await zatcaService.OnboardingDevice();

                //Console.WriteLine("Onboarding Result: \n\n");
                //Console.WriteLine($"Generated CSR:\n{result.GeneratedCsr} \n");
                //Console.WriteLine($"Private Key:\n{result.PrivateKey} \n");
                //Console.WriteLine($"CCSID Compliance Request ID:\n{result.CCSIDComplianceRequestId} \n");
                //Console.WriteLine($"CCSID Binary Token:\n{result.CCSIDBinaryToken}  \n");
                //Console.WriteLine($"CCSID Secret:\n{result.CCSIDSecret} \n");
                //Console.WriteLine($"PCSID Binary Token:\n{result.PCSIDBinaryToken} \n");
                //Console.WriteLine($"PCSID Secret:\n{result.PCSIDSecret}  \n\n");

                //// Step 2: Create Signed Invoice
                ////var signedInvoiceResult = await zatcaService.CreateSignedInvoice(result.PCSIDBinaryToken, result.PrivateKey);
                //var signedInvoiceResult = await zatcaService.CreateSignedInvoice(result.CCSIDBinaryToken, result.PrivateKey);

                //Console.WriteLine("Signed Invoice Result:\n\n");
                ////Console.WriteLine($"Base64 Signed Invoice:\n{signedInvoiceResult.Base64SignedInvoice} \n");
                //Console.WriteLine($"Base64 QR Code:\n{signedInvoiceResult.Base64QrCode} \n");
                //Console.WriteLine($"XML File Name:\n{signedInvoiceResult.XmlFileName} \n");
                ////Console.WriteLine($"Request API:\n{signedInvoiceResult.RequestApi} \n");


                //// Step 3: Compliance Check
                //var complianceResult = await zatcaService.ComplianceCheck(
                //    result.CCSIDBinaryToken,
                //    result.CCSIDSecret,
                //    signedInvoiceResult.RequestApi
                //);

                //// Convert the result to JSON, removing null values
                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore,
                //    Formatting = Formatting.Indented
                //};
                //var jsonResult = JsonConvert.SerializeObject(complianceResult, settings);

                //// Display the formatted JSON result
                //Console.WriteLine("\nCompliance Check Result (JSON):");
                //Console.WriteLine(jsonResult);


                //// Decode Base64QrCode

                //Console.WriteLine("\nDecoding Generated QRCode:");
                //Console.WriteLine();
                string qrCodeContent = "AR1NYXhpbXVtIFNwZWVkIFRlY2ggU3VwcGx5IExURAIPMzk5OTk5OTk5OTAwMDAzAxMyMDI0LTA4LTIzVDAwOjAwOjAwBAcxNjU2LjAwBQYyMTYuMDAGLGV2QkZwOEI5TmpLVElVQi9JSTJqNXpRei9xVm5uV2drTFQzRkZYYTBacDg9B2BNRVVDSUVRN3dTKzF0WmRNbDQ4YWVQUWF4MDNKQlFqWENrbVpHNnNpb2FWRU9hZkFBaUVBMVE5bXpxZlZPMi9oRUhmR3lBVEpldzdSNnpOUDJGUm1TbFJQajNRT3FQUT0IWDBWMBAGByqGSM49AgEGBSuBBAAKA0IABKFgimtEmvRSBK0zr9LgJAtVSCl8VPZz6cdr5X+MoTHo8vHNNlyW5Q6u7T8naPJqtGoTjJjaPIMJ4u17dSk/VHgJRzBFAiEAsT+JyGadZcJQpRtxrfJyLyirBou8V0dWNCu94j26oBsCID2ELgzyOAwEAM9LOZ3a6I8kDqApHcsTTdTvl6psL+tc"; //signedInvoiceResult.Base64QrCode;
                var decodedContent = QrCodeDecoder.DecodeQRCode(qrCodeContent);
                QrCodeDecoder.PrintDecodedContent(decodedContent);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}