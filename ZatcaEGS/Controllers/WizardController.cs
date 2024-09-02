using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Zatca.eInvoice.Models;
using ZatcaEGS.Models;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice;
using ZatcaEGS.Helpers;


namespace ZatcaEGS.Controllers
{
    public class WizardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly CsrGenerator _csrGenerator;
        private readonly HttpClient _httpClient = new();

        public WizardController(AppDbContext context)
        {
            _context = context;
            _csrGenerator = new CsrGenerator();
        }

        public IActionResult Index()
        {
            var existingModel = _context.DeviceSetups.OrderBy(x => x.RowId).FirstOrDefault();

            if (existingModel == null)
            {
                existingModel = new DeviceSetup()
                {
                    AccessToken = "Cg5aYXRjYSBlSW52b2ljZRISCdLOzb499qRIEa2z2cVji+v8GhIJP/nFt1cZq0IRluVQdksmfSs=",
                    //BusinessDatabaseGuid = "38cf4712-6e95-4ce1-b53a-bff03edad273",
                    //BaseCurrencyGuid = "39dde4fc-7af8-44cc-8572-3b1ff4cfb918",
                    InvoiceSubTypeGuid = "e1050215-e02a-4de0-9d55-cf0dba27a0d6",
                    ItemTaxCategoryGuid = "6862773d-f847-486e-824b-0b42aaf0cf17",
                    PaymentMeansCodeGuid = "df844e4d-2ccc-4e46-9e3b-ac51a80868a2",
                    InstructionNoteGuid = "5bde867b-c278-4fd5-b149-e0c796ec0ddb",
                    PartyTaxInfoGuid = "93f79973-5346-4c6b-b912-90ea9bbf69c2",
                    QrCodeGuid = "3c4c1fb3-3b0e-4f2e-9eeb-2d466c496e2f",

                    IdentificationID = "1010010000",
                    IdentificationScheme = "CRN",
                    StreetName = "الامير سلطان | Prince Sultan",
                    BuildingNumber = "2322",
                    CitySubdivisionName = "المربع | Al-Murabba",
                    CityName = "الرياض | Riyadh",
                    PostalZone = "23333",
                    CountryIdentificationCode = "SA",
                    CompanyID = "399999999900003",
                    TaxSchemeID = "VAT",
                    RegistrationName = "شركة توريد التكنولوجيا بأقصى سرعة المحدودة | Maximum Speed Tech Supply LTD",
                    BusinessCategory = "Supply activities",

                    ComplianceCSIDUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance",
                    ProductionCSIDUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/production/csids",
                    ComplianceCheckUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance/invoices",
                    ReportingUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/reporting/single",
                    ClearanceUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/clearance/single"
                };

                try
                {
                    _context.Add(existingModel);
                    _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw;
                }
            };

            return View(existingModel);
        }


        [HttpPost]
        public async Task<IActionResult> Finish(DeviceSetup model)
        {
            if (ModelState.IsValid)
            {
                var existingModel = _context.DeviceSetups.FirstOrDefault(o => o.RowId == model.RowId);

                if (existingModel != null)
                {
                    _context.Entry(existingModel).CurrentValues.SetValues(model);
                }
                else
                {
                    _context.Add(model);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            return View("Index", model);
        }


        [HttpPost("generatecsr")]
        public IActionResult GenerateCSR([FromBody] CsrGenerationDto csrData, [FromQuery] EnvironmentType environmentType)
        {
            if (csrData.IsValid(out var errors))
            {
                try
                {

                    if (environmentType == EnvironmentType.NonProduction)
                    {
                        csrData.OrganizationIdentifier = "399999999800003";
                        csrData.CommonName = csrData.CommonName.Substring(0, csrData.CommonName.Length - 15) + "399999999800003";
                    }

                    var (csr, privateKey, errorMessages) = _csrGenerator.GenerateCsrAndPrivateKey(csrData, environmentType, false);
                    return Ok(new { Csr = csr, PrivateKey = privateKey, ErrorMessages = errorMessages });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error generating CSR: " + ex.Message);
                    return BadRequest("Error generating CSR: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Invalid CSR data: " + string.Join(", ", errors));
                return BadRequest(new { Errors = errors });
            }
        }

        [HttpPost("getccsid")]
        public async Task<IActionResult> GetCCSID([FromForm] DeviceSetup model, [FromForm] string OTP)
        {
            try
            {
                // Get CCSID
                string jsonContent = JsonConvert.SerializeObject(new { csr = model.GeneratedCSR });
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("OTP", OTP);
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(model.ComplianceCSIDUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Error getting CCSID: " + response.ReasonPhrase);
                }

                var resultContent = await response.Content.ReadAsStringAsync();
                var zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

                var ccsidResult = new CCSIDResultDto
                {
                    CCSIDBinaryToken = zatcaResult.BinarySecurityToken,
                    CCSIDComplianceRequestId = zatcaResult.RequestID,
                    CCSIDSecret = zatcaResult.Secret
                };

                model.CCSIDBinaryToken = ccsidResult.CCSIDBinaryToken;
                model.CCSIDComplianceRequestId = ccsidResult.CCSIDComplianceRequestId;
                model.CCSIDSecret = ccsidResult.CCSIDSecret;

                try
                {
                    DeviceSetup existingModel = _context.DeviceSetups.FirstOrDefault(o => o.RowId == model.RowId);

                    if (existingModel != null)
                    {
                        _context.Entry(existingModel).CurrentValues.SetValues(model);
                    }
                    else
                    {
                        _context.Add(model);
                    }

                    await _context.SaveChangesAsync();

                }
                catch
                {
                    throw;
                }


                return Ok(ccsidResult);
            }
            catch
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("getpcsid")]
        public async Task<IActionResult> GetPCSID([FromForm] DeviceSetup model)
        {
            try
            {

                //Invoice Compliance Check
                ComplianceTest ct = new ComplianceTest(_context, model.CCSIDBinaryToken, model.EcSecp256k1Privkeypem);
                string invoiceHash = null;
                int iICV = 0;

                //10000 Clearance Standard

                if (model.CsrInvoiceType.StartsWith("1"))
                {
                    iICV += 1;
                    string InvDebitNote = ct.GetRequestApi("DN-202408-0001", "PCH-202408-0001", InvoiceType.TaxInvoiceDebitNote, "0100000", iICV, "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==");
                    invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvDebitNote, model.CCSIDBinaryToken, model.CCSIDSecret);

                    if (!string.IsNullOrEmpty(invoiceHash))
                    {
                        iICV += 1;
                        string InvSales = ct.GetRequestApi("INV-202408-0001", null, InvoiceType.TaxInvoice, "0100000", iICV, invoiceHash);
                        invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvSales, model.CCSIDBinaryToken, model.CCSIDSecret);
                        if (!string.IsNullOrEmpty(invoiceHash))
                        {
                            iICV += 1;
                            string InvCreditNote = ct.GetRequestApi("CN-202408-0001", "INV-202408-0001", InvoiceType.TaxInvoiceCreditNote, "0100000", iICV, invoiceHash);
                            invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvCreditNote, model.CCSIDBinaryToken, model.CCSIDSecret);
                            if (string.IsNullOrEmpty(invoiceHash))
                            {
                                return null;
                            }
                        }
                    }
                }

                //01000 || 11000  Reporting Simplified 

                if (model.CsrInvoiceType.Substring(1, 1) == "1")
                {

                    if (string.IsNullOrEmpty(invoiceHash))
                    {
                        invoiceHash = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";
                        iICV = 0;
                    };

                    iICV += 1;
                    string InvDebitNote = ct.GetRequestApi("DN-202408-0001", "PCH-202408-0001", InvoiceType.TaxInvoiceDebitNote, "0200000", iICV, invoiceHash);
                    invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvDebitNote, model.CCSIDBinaryToken, model.CCSIDSecret);

                    if (!string.IsNullOrEmpty(invoiceHash))
                    {
                        iICV += 1;
                        string InvSales = ct.GetRequestApi("INV-202408-0001", null, InvoiceType.TaxInvoice, "0200000", iICV, invoiceHash);
                        invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvSales, model.CCSIDBinaryToken, model.CCSIDSecret);
                        if (!string.IsNullOrEmpty(invoiceHash))
                        {
                            iICV += 1;
                            string InvCreditNote = ct.GetRequestApi("CN-202408-0001", "INV-202408-0001", InvoiceType.TaxInvoiceCreditNote, "0200000", iICV, invoiceHash);
                            invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvCreditNote, model.CCSIDBinaryToken, model.CCSIDSecret);
                            if (string.IsNullOrEmpty(invoiceHash))
                            {
                                return null;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(invoiceHash))
                {
                    return null;
                }

                // Get PCSID

                string jsonContent = JsonConvert.SerializeObject(new { compliance_request_id = model.CCSIDComplianceRequestId });

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{model.CCSIDBinaryToken}:{model.CCSIDSecret}")));

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(model.ProductionCSIDUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Error getting PCSID: " + response.ReasonPhrase);
                }

                string resultContent = await response.Content.ReadAsStringAsync();
                ZatcaResultDto zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

                var pcsidResult = new PCSIDResultDto
                {
                    PCSIDBinaryToken = zatcaResult.BinarySecurityToken,
                    PCSIDSecret = zatcaResult.Secret,
                    RegisteredDate = DateTime.Now,
                };

                model.PCSIDBinaryToken = pcsidResult.PCSIDBinaryToken;
                model.PCSIDSecret = pcsidResult.PCSIDSecret;
                model.RegisteredDate = pcsidResult.RegisteredDate;

                try
                {
                    DeviceSetup existingModel = _context.DeviceSetups.FirstOrDefault(o => o.RowId == model.RowId);

                    if (existingModel != null)
                    {
                        _context.Entry(existingModel).CurrentValues.SetValues(model);
                    }
                    else
                    {
                        _context.Add(model);
                    }

                    await _context.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    throw;
                }

                return Ok(pcsidResult);
            }

            catch
            {
                return StatusCode(500, "Internal server error");
            }
        }

        public async Task<string> PostComplianceCheck(string ComplianceCheckUrl, string RequestApi, string CCSIDBinaryToken, string CCSIDSecret)
        {
            try
            {

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{CCSIDBinaryToken}:{CCSIDSecret}")));

                var content = new StringContent(RequestApi, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ComplianceCheckUrl, content);

                var resultContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);


                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                var jsonResult = JsonConvert.SerializeObject(apiResponse, settings);
                Console.WriteLine(jsonResult);

                if (apiResponse.ClearanceStatus == "CLEARED" || apiResponse.ReportingStatus == "REPORTED")
                {
                    var requestJson = JsonConvert.DeserializeObject<ZatcaRequestApi>(RequestApi);


                    return requestJson.InvoiceHash;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

    }
}