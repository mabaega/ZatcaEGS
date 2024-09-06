using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using ZatcaEGS.Helpers;
using ZatcaEGS.Models;
using Zatca.eInvoice;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;

namespace ZatcaEGS.Controllers
{
    public class WizardController : Controller
    {
        private readonly CsrGenerator _csrGenerator;
        private readonly HttpClient _httpClient = new();

        public WizardController()
        {
            _csrGenerator = new CsrGenerator();
        }

        public IActionResult Index()
        {
            var currentUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";

            var existingModel = new CertificateInfo()
            {
                ApiEndpoint = "",
                ApiSecret = "",

                IdentificationID = "1010010000",
                IdentificationScheme = "CRN",
                StreetName = "Prince Sultan",
                BuildingNumber = "2322",
                CitySubdivisionName = "Al-Murabba",
                CityName = "Riyadh",
                PostalZone = "23333",
                CountryIdentificationCode = "SA",
                CompanyID = "399999999900003",
                TaxSchemeID = "VAT",
                RegistrationName = "Maximum Speed Tech Supply LTD",
                BusinessCategory = "Supply activities",
                EnvironmentType = EnvironmentType.NonProduction,
                RelayURL = UrlHelper.RelayUrl(currentUrl)
            };

            return View(existingModel);
        }


        [HttpPost]
        public IActionResult Finish(CertificateInfo model)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(model.PCSIDBinaryToken))
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        // File 1: cert.pem
                        var certEntry = archive.CreateEntry("cert.pem");
                        using (var writer = new StreamWriter(certEntry.Open()))
                        {
                            byte[] decodedBytes = Convert.FromBase64String(model.PCSIDBinaryToken);
                            string decodedToken = Encoding.UTF8.GetString(decodedBytes);
                            writer.Write(decodedToken);
                        }

                        // File 2: ec-secp256k1-priv-key.pem
                        var keyEntry = archive.CreateEntry("ec-secp256k1-priv-key.pem");
                        using (var writer = new StreamWriter(keyEntry.Open()))
                        {
                            writer.Write(model.EcSecp256k1Privkeypem);
                        }

                        // File 3: Original certificate info
                        var infoEntry = archive.CreateEntry($"{model.CsrCommonName}_{model.EnvironmentType}.txt");
                        using (var writer = new StreamWriter(infoEntry.Open()))
                        {
                            writer.WriteLine("Manager Certificate Info:");
                            writer.WriteLine(ObjectCompressor.SerializeToBase64String(model));
                            writer.WriteLine("\nOnboarding Device Info:");
                            foreach (var property in model.GetType().GetProperties())
                            {
                                if (property.Name != "ApiSecret" && property.Name != "ApiEndpoint")
                                {
                                    writer.WriteLine($"{property.Name}:");
                                    writer.WriteLine(property.GetValue(model));
                                    writer.WriteLine();
                                }
                            }
                        }
                    }

                    return File(memoryStream.ToArray(), MediaTypeNames.Application.Zip, $"{model.CsrCommonName}_{model.EnvironmentType}.zip");
                }
            }
            return View("Index", model);
        }


        //[HttpPost]
        //public IActionResult Finish(CertificateInfo model)
        //{
        //    if (ModelState.IsValid && !string.IsNullOrEmpty(model.PCSIDBinaryToken))
        //    {
        //        string certificateInfo = $"\nManager Certificate Info:\n\n";
        //        certificateInfo += $"{ObjectCompressor.SerializeToBase64String(model)}\n\n";

        //        certificateInfo += $"\nOnboarding Device Info:\n\n";
        //        foreach (var property in model.GetType().GetProperties())
        //        {
        //            if (property.Name != "ApiSecret" && property.Name != "ApiEndpoint")
        //            {
        //                certificateInfo += $"{property.Name}: \n{property.GetValue(model)}\n\n";
        //            }
        //        }

        //        byte[] certificateBytes = Encoding.UTF8.GetBytes(certificateInfo);
        //        var contentType = "application/octet-stream";
        //        var fileName = $"{model.CsrCommonName}_{model.EnvironmentType}.txt";

        //        return File(certificateBytes, contentType, fileName);
        //    }

        //    return View("Index", model);
        //}


        [HttpPost("generatecf")]
        public async Task<IActionResult> GenerateCustomFieldAsync([FromBody] AccessTokenDto token)
        {
            try
            {
                string ApiUrl = token.ApiEndpoint;
                string apiKey = token.ApiSecret;

                // Load JSON data from resources
                byte[] jsonDataBytes = ZatcaEGS.Properties.Resources.cfData;
                string jsonData = Encoding.UTF8.GetString(jsonDataBytes);

                try
                {
                    JObject jsonObject = JObject.Parse(jsonData);
                    JArray jsonDataArray = (JArray)jsonObject["jsondata"];

                    StringBuilder result = new StringBuilder();

                    foreach (JObject item in jsonDataArray.Cast<JObject>())
                    {
                        string apiPath = item.Value<string>("apipath");

                        if (item.ContainsKey("data"))
                        {
                            JObject dataObject = (JObject)item["data"];
                            string jsonPayload = dataObject.ToString(Newtonsoft.Json.Formatting.None);

                            string key = dataObject.Value<string>("Key");

                            string fullPath = $"{ApiUrl}{apiPath}/{key}";

                            string nameValue = dataObject.Value<string>("Name");
                            if (nameValue == null)
                            {
                                nameValue = dataObject.Value<string>("Description");
                            }

                            var responseMessage = await SendDataToApi(fullPath, jsonPayload, apiKey);

                            result.AppendLine(nameValue + " " + responseMessage);

                            await Task.Delay(100); // Use Task.Delay for async delay
                        }
                    }
                    return Ok(result.ToString());
                }
                catch (Exception ex)
                {
                    return BadRequest("Error generating Custom Field: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Error generating Custom Field: " + ex.Message);
            }
        }

        internal static async Task<string> SendDataToApi(string fullPath, string jsonPayload, string apiKey)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {

                    httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    // Send PUT request to the full URL
                    var response = await httpClient.PutAsync(fullPath, content);

                    if (response.IsSuccessStatusCode)
                    {
                        return $"Successfully generated!";
                    }
                    else
                    {
                        return $"Failed with Status code: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
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
                        csrData.CommonName = string.Concat(csrData.CommonName.AsSpan(0, csrData.CommonName.Length - 15), "399999999800003");
                    }

                    var (csr, privateKey, errorMessages) = _csrGenerator.GenerateCsrAndPrivateKey(csrData, environmentType, false);
                    return Ok(new { Csr = csr, PrivateKey = privateKey, ErrorMessages = errorMessages });
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("Error generating CSR: " + ex.Message);
                    return BadRequest("Error generating CSR: " + ex.Message);
                }
            }
            else
            {
                //Console.WriteLine("Invalid CSR data: " + string.Join(", ", errors));
                return BadRequest(new { Errors = errors });
            }
        }

        [HttpPost("getccsid")]
        public async Task<IActionResult> GetCCSID([FromForm] CertificateInfo model, [FromForm] string OTP)
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

                return Ok(ccsidResult);
            }
            catch
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("getpcsid")]
        public async Task<IActionResult> GetPCSID([FromForm] CertificateInfo model)
        {
            try
            {

                //Invoice Compliance Check
                ComplianceTest ct = new ComplianceTest(model, model.CCSIDBinaryToken, model.EcSecp256k1Privkeypem);
                string invoiceHash = null;
                int iICV = 0;

                //10000 Clearance Standard

                if (model.CsrInvoiceType.StartsWith('1'))
                {
                    iICV += 1;
                    ZatcaRequestApi InvDebitNote = ct.GetRequestApi("DN-202408-0001", "PCH-202408-0001", InvoiceType.TaxInvoiceDebitNote, "0100000", iICV, "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==");
                    invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvDebitNote, model.CCSIDBinaryToken, model.CCSIDSecret);

                    if (!string.IsNullOrEmpty(invoiceHash))
                    {
                        iICV += 1;
                        ZatcaRequestApi InvSales = ct.GetRequestApi("INV-202408-0001", null, InvoiceType.TaxInvoice, "0100000", iICV, invoiceHash);
                        invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvSales, model.CCSIDBinaryToken, model.CCSIDSecret);
                        if (!string.IsNullOrEmpty(invoiceHash))
                        {
                            iICV += 1;
                            ZatcaRequestApi InvCreditNote = ct.GetRequestApi("CN-202408-0001", "INV-202408-0001", InvoiceType.TaxInvoiceCreditNote, "0100000", iICV, invoiceHash);
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
                    ZatcaRequestApi InvDebitNote = ct.GetRequestApi("DN-202408-0001", "PCH-202408-0001", InvoiceType.TaxInvoiceDebitNote, "0200000", iICV, invoiceHash);
                    invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvDebitNote, model.CCSIDBinaryToken, model.CCSIDSecret);

                    if (!string.IsNullOrEmpty(invoiceHash))
                    {
                        iICV += 1;
                        ZatcaRequestApi InvSales = ct.GetRequestApi("INV-202408-0001", null, InvoiceType.TaxInvoice, "0200000", iICV, invoiceHash);
                        invoiceHash = await PostComplianceCheck(model.ComplianceCheckUrl, InvSales, model.CCSIDBinaryToken, model.CCSIDSecret);
                        if (!string.IsNullOrEmpty(invoiceHash))
                        {
                            iICV += 1;
                            ZatcaRequestApi InvCreditNote = ct.GetRequestApi("CN-202408-0001", "INV-202408-0001", InvoiceType.TaxInvoiceCreditNote, "0200000", iICV, invoiceHash);
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

                return Ok(pcsidResult);
            }

            catch
            {
                return StatusCode(500, "Internal server error");
            }
        }

        public async Task<string> PostComplianceCheck(string ComplianceCheckUrl, ZatcaRequestApi RequestApi, string CCSIDBinaryToken, string CCSIDSecret)
        {
            try
            {

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{CCSIDBinaryToken}:{CCSIDSecret}")));

                var jsonContent = JsonConvert.SerializeObject(RequestApi);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ComplianceCheckUrl, content);

                var resultContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);


                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };

                var jsonResult = JsonConvert.SerializeObject(apiResponse, settings);
                //Console.WriteLine(jsonResult);

                if (apiResponse.ClearanceStatus == "CLEARED" || apiResponse.ReportingStatus == "REPORTED")
                {
                    return RequestApi.invoiceHash;
                }

                return null;
            }
            catch
            {
                //Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}