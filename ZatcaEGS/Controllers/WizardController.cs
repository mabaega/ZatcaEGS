using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.Design;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Zatca.eInvoice.Models;
using ZatcaEGS.Models;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice;

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
            var existingModel = _context.DeviceSetups.OrderBy(x => x.RowId).FirstOrDefault() ?? new DeviceSetup()
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
            return View(existingModel);
        }


        [HttpPost]
        public async Task<IActionResult> Finish(DeviceSetup model)
        {
            if (ModelState.IsValid)
            {
                var existingModel = _context.DeviceSetups.Where(x => x.RowId == model.RowId).FirstOrDefault();

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
        public IActionResult GenerateCSR([FromBody] CsrGenerationDto csrData)
        {
            if (csrData.IsValid(out var errors))
            {
                try
                {
                    var (csr, privateKey, errorMessages) = _csrGenerator.GenerateCsrAndPrivateKey(csrData, EnvironmentType.NonProduction, false);
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

                var onboardingResult = new OnboardingResultDto
                {
                    CCSIDBinaryToken = zatcaResult.BinarySecurityToken,
                    CCSIDComplianceRequestId = zatcaResult.RequestID,
                    CCSIDSecret = zatcaResult.Secret
                };

                // Get PCSID

                jsonContent = JsonConvert.SerializeObject(new { compliance_request_id = zatcaResult.RequestID });

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{zatcaResult.BinarySecurityToken}:{zatcaResult.Secret}")));

                content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync(model.ProductionCSIDUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Error getting PCSID: " + response.ReasonPhrase);
                }

                resultContent = await response.Content.ReadAsStringAsync();
                zatcaResult = JsonConvert.DeserializeObject<ZatcaResultDto>(resultContent);

                onboardingResult.PCSIDBinaryToken = zatcaResult.BinarySecurityToken;
                onboardingResult.PCSIDSecret = zatcaResult.Secret;
                onboardingResult.RegisteredDate = DateTime.Now;

                model.CCSIDBinaryToken = onboardingResult.CCSIDBinaryToken;
                model.CCSIDComplianceRequestId = onboardingResult.CCSIDComplianceRequestId;
                model.CCSIDSecret = onboardingResult.CCSIDSecret;
                model.PCSIDBinaryToken = onboardingResult.PCSIDBinaryToken;
                model.PCSIDSecret = onboardingResult.PCSIDSecret;
                model.RegisteredDate = onboardingResult.RegisteredDate;

                try
                {
                    DeviceSetup existingModel = _context.DeviceSetups.Where(x => x.RowId == model.RowId).FirstOrDefault();

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

                return Ok(onboardingResult);
            }
            catch
            {
                return StatusCode(500, "Internal server error");
            }
        }
    }
}