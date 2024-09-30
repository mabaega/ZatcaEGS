using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;
using Zatca.eInvoice;
using Zatca.eInvoice.Models;
using ZatcaEGS.Helpers;
using ZatcaEGS.Models;

namespace ZatcaEGS.Controllers
{
    public class RelayController : Controller
    {
        private readonly HttpClient _httpClient = new();

        [HttpPost("relay")]
        public async Task<IActionResult> ProcessFormData([FromForm] Dictionary<string, string> formData)
        {
            try
            {
                var relayData = new RelayData(formData);

                var certInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(relayData.CertInfoString);

                if (certInfo == null)
                {
                    return View("Disclaimer", new SetupViewModel
                    {
                        Referrer = relayData.Referrer,
                        BusinessDetails = relayData.BusinessDetails,
                        Api = formData.GetValueOrDefault("Api"),
                        Token = formData.GetValueOrDefault("Token"),
                    });
                }

                //Check Expired Certificate
                try
                {
                    byte[] certificateBytes = Convert.FromBase64String(certInfo.PCSIDBinaryToken);
                    X509Certificate2 cert = new X509Certificate2(certificateBytes);
                    DateTime expirationDate = cert.NotAfter;
                    Console.WriteLine($"Certificate Valid from : {cert.NotBefore} to {cert.NotAfter}");
                    if (DateTime.Now > expirationDate)
                    {
                        //Goto Renewal Page
                        Console.WriteLine("Sertifikat sudah kedaluwarsa.");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }


                bool hasTokenSecret = relayData.HasTokenSecret;

                if (hasTokenSecret && !string.IsNullOrEmpty(certInfo.ApiSecret))
                {
                    var businessDetails = relayData.BusinessDetails;
                    businessDetails = JsonParser.RemoveJsonField(businessDetails, ManagerCustomField.TokenInfoGuid);

                    var svm = new SetupViewModel
                    {
                        Referrer = relayData.Referrer,
                        BusinessDetails = relayData.BusinessDetails,
                        BusinessDetailsJson = JsonConvert.SerializeObject(businessDetails),
                        Api = formData.GetValueOrDefault("Api"),
                        Token = formData.GetValueOrDefault("Token"),
                    };

                    TempData["SetupViewModel"] = JsonConvert.SerializeObject(svm);
                    return RedirectToAction("UpdateBusinessData", "Setup");
                }

                if (!string.IsNullOrEmpty(relayData.ApprovalStatus) && !relayData.ApprovalStatus.Contains("REJECTED"))
                {
                    var relayViewModel = new RelayViewModel
                    {
                        ZatcaUUID = relayData.ZatcaUUID,
                        Base64QrCode = relayData.Base64QrCode,
                        ReferrerLink = relayData.Referrer,
                        ShowSetupLink = false,
                    };

                    return View("Info", relayViewModel);
                }


                string zatcaUUID = await GenerateZatcaUUID(relayData);
                relayData.ZatcaUUID = zatcaUUID;

                var invoiceObject = await GenerateInvoiceObject(relayData);

                var signedInvoiceResult = await SignInvoice(invoiceObject, certInfo);

                ManagerInvoice managerInvoice = relayData.ManagerInvoice;
                int icv = relayData.LastICV + 1;

                var amount = invoiceObject.LegalMonetaryTotal.TaxExclusiveAmount.NumericValue;
                var totalAmount = invoiceObject.LegalMonetaryTotal.TaxInclusiveAmount.NumericValue;
                var taxAmount = totalAmount - amount;

                ApprovedInvoice approvedInvoice = new()
                {
                    ManagerUUID = relayData.Key,

                    ZatcaUUID = zatcaUUID,
                    InvoiceHash = signedInvoiceResult.InvoiceHash,
                    Base64Invoice = signedInvoiceResult.Base64SignedInvoice,

                    InvoiceType = invoiceObject.InvoiceTypeCode?.Value,
                    InvoiceSubType = invoiceObject.InvoiceTypeCode?.Name,
                    Reference = managerInvoice?.Reference,
                    IssueDate = relayData.DateCreated ?? managerInvoice?.IssueDate.ToString("yyyy-MM-dd"),
                    PartyName = managerInvoice?.InvoiceParty?.Name,
                    CurrencyCode = managerInvoice?.InvoiceParty?.Currency?.Code ?? "SAR",
                    Amount = amount,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,

                    Base64QrCode = signedInvoiceResult.Base64QrCode,
                    XmlFileName = signedInvoiceResult.XmlFileName,
                    Referrer = relayData.Referrer,
                    CallBack = relayData.Callback,
                    ICV = relayData.LastICV + 1,

                    Timestamp = DateTime.Now,
                    EditData = relayData.Data,

                    EnvironmentType = certInfo.EnvironmentType,

                    CertificateInfo = relayData.CertInfoString
                };

                return View("Index", approvedInvoice);

            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Error processing form data");
                var errorViewModel = new ErrorViewModel
                {
                    ErrorMessage = "Please review the Device Settings in the EGS application to ensure it matches your business data setup.",
                    ReferrerLink = formData.GetValueOrDefault("Referrer")
                };
                return View("Error", errorViewModel);
            }
        }


        private static async Task<string> GenerateZatcaUUID(RelayData relayData)
        {
            if (string.IsNullOrEmpty(relayData.ApprovalStatus) || !relayData.ApprovalStatus.Contains("REJECTED"))
            {
                return relayData.Key;
            }

            return await Task.Run(() =>
            {
                var sNumber = relayData.ZatcaUUID.Substring(relayData.Key.Length - 12, 12);
                if (int.TryParse(sNumber, out int numericValue))
                {
                    sNumber = (numericValue + 1).ToString("D12");
                }
                else
                {
                    sNumber = "000000000001";
                }
                return string.Concat(relayData.Key.AsSpan(0, relayData.Key.Length - 12), sNumber);
            });
        }

        private static async Task<Invoice> GenerateInvoiceObject(RelayData relayData)
        {
            var mapper = new RelayToInvoiceMapper(relayData);
            return await Task.Run(() => mapper.GenerateInvoiceObject());
        }

        private static async Task<SignedInvoiceResult> SignInvoice(Invoice invoiceObject, CertificateInfo certInfo)
        {
            var invoiceGenerator = new InvoiceGenerator(
                invoiceObject,
                Encoding.UTF8.GetString(Convert.FromBase64String(certInfo.PCSIDBinaryToken)),
                certInfo.EcSecp256k1Privkeypem
            );

            // Execute GetSignedInvoiceResult asynchronously using Task.Run
            return await Task.Run(() => invoiceGenerator.GetSignedInvoiceResult());
        }

        public async Task<IActionResult> AjaxComplianceCheck([FromForm] ApprovedInvoice model)
        {
            try
            {
                // We Use SanBox Portal to Validate Invoice
                string SDK_CCSIDBinaryToken = "TUlJQ05EQ0NBZHFnQXdJQkFnSUdBWkc0alp2a01Bb0dDQ3FHU000OUJBTUNNQlV4RXpBUkJnTlZCQU1NQ21WSmJuWnZhV05wYm1jd0hoY05NalF3T1RBek1UVTBNalE0V2hjTk1qa3dPVEF5TWpFd01EQXdXakJ2TVFzd0NRWURWUVFHRXdKVFFURVBNQTBHQTFVRUN3d0dVbWw1WVdSb01TWXdKQVlEVlFRS0RCMU5ZWGhwYlhWdElGTndaV1ZrSUZSbFkyZ2dVM1Z3Y0d4NUlFeFVSREVuTUNVR0ExVUVBd3dlVFU1SExURXdNVEF3TVRBd01EQXRNems1T1RrNU9UazVPREF3TURBek1GWXdFQVlIS29aSXpqMENBUVlGSzRFRUFBb0RRZ0FFT1BwdCtHenozcjFWTVBTZ1pZSHQxQkQvcGQyYzVrbEp1VmJHbkwycGtuS1d0b1dyejUvVUlGQ2JnaUN5anpLVTB2WEExVG9nQ3Q5VXVDcnozVUpTbktPQnZqQ0J1ekFNQmdOVkhSTUJBZjhFQWpBQU1JR3FCZ05WSFJFRWdhSXdnWitrZ1p3d2daa3hPekE1QmdOVkJBUU1NakV0UlVkVGZESXRUVTVIZkRNdE1HRXpOalV6TlRFdE1qZzVNUzAwTnpCakxUZzBZelF0TlRRMlpUSmtOakV4TkRZMU1SOHdIUVlLQ1pJbWlaUHlMR1FCQVF3UE16azVPVGs1T1RrNU9EQXdNREF6TVEwd0N3WURWUVFNREFReE1UQXdNUTR3REFZRFZRUWFEQVV5TXpNek16RWFNQmdHQTFVRUR3d1JVM1Z3Y0d4NUlHRmpkR2wyYVhScFpYTXdDZ1lJS29aSXpqMEVBd0lEU0FBd1JRSWhBTDJITnNhaVdENEJLYkwxc2pqeWdqUlpSRFdZekxNclJ4dEtlemYvdTVyY0FpQTVWbjh4REdRV1JXNm1LQnRROHBtNS9jS2hIZW9ZTmpQUkNWdjF3UTNVVFE9PQ==";
                string SDK_CCSIDSecret = "X+lO9bFc4PfAth8jb22vxcsKaiDAFsrgE7PI5q6+txk=";
                string SDK_ComplianceCheckUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance/invoices";

                var certInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(model.CertificateInfo);
                var zatcaRequestApi = CreateZatcaRequestApi(model);

                var jsonContent = JsonConvert.SerializeObject(zatcaRequestApi);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{SDK_CCSIDBinaryToken}:{SDK_CCSIDSecret}")));

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(SDK_ComplianceCheckUrl, content);

                var resultContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                apiResponse ??= new ServerResult();

                apiResponse.RequestUri = SDK_ComplianceCheckUrl;
                apiResponse.RequestType = "Invoice Compliant Check";
                apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

                var complianceCheckResult = new
                {
                    serverResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                    approvalStatus = model.ApprovalStatus,
                    invoiceSubType = model.InvoiceSubType
                };

                return Json(complianceCheckResult);  // Kembalikan respon sebagai JSON

            }
            catch (Exception ex)
            {
                return Json(new { serverResult = ex.Message });
            }
        }



        [HttpPost("clearance")]
        public async Task<IActionResult> Clearance([FromForm] ApprovedInvoice model)
        {
            return await ProcessInvoice(model, true);
        }

        [HttpPost("reporting")]
        public async Task<IActionResult> Reporting([FromForm] ApprovedInvoice model)
        {
            return await ProcessInvoice(model, false);
        }

        private async Task<IActionResult> ProcessInvoice(ApprovedInvoice model, bool IsClearance)
        {
            try
            {
                var certInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(model.CertificateInfo);
                var zatcaRequestApi = CreateZatcaRequestApi(model);

                var response = await SendHttpRequest(zatcaRequestApi, certInfo, IsClearance);

                var resultContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    await HandleSuccessfulResponse(model, certInfo, response, resultContent, IsClearance);
                }
                else
                {
                    HandleUnsuccessfulResponse(model, certInfo, response, resultContent, IsClearance);
                }

                return View("Index", model);
            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Error processing invoice");
                model.ServerResult = "An error occurred while processing the invoice. Please try again later.";
                return View("Index", model);
            }
        }

        private static ZatcaRequestApi CreateZatcaRequestApi(ApprovedInvoice model)
        {
            return new ZatcaRequestApi
            {
                uuid = model.ZatcaUUID,
                invoiceHash = model.InvoiceHash,
                invoice = model.Base64Invoice
            };
        }

        private async Task<HttpResponseMessage> SendHttpRequest(ZatcaRequestApi zatcaRequestApi,
            CertificateInfo certInfo, bool IsClearance)
        {

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("Clearance-Status", IsClearance ? "1" : "0");
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{certInfo.PCSIDBinaryToken}:{certInfo.PCSIDSecret}")));

            var jsonContent = JsonConvert.SerializeObject(zatcaRequestApi);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var url = IsClearance ? certInfo.ClearanceUrl : certInfo.ReportingUrl;

            return await _httpClient.PostAsync(url, content);
        }

        private async Task HandleSuccessfulResponse(ApprovedInvoice model, CertificateInfo certInfo,
            HttpResponseMessage response, string resultContent, bool IsClearance)
        {
            var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent) ?? new ServerResult();

            if (IsClearance && apiResponse.ClearedInvoice != null)
            {
                await ProcessClearedInvoice(model, apiResponse);
            }

            UpdateModelWithApiResponse(model, apiResponse, certInfo, response, IsClearance);

            PrepareTempData(model, certInfo);
        }

        private static async Task ProcessClearedInvoice(ApprovedInvoice model, ServerResult apiResponse)
        {
            var clearedInvoiceXml = Encoding.UTF8.GetString(Convert.FromBase64String(apiResponse.ClearedInvoice));
            //model.Base64Invoice = apiResponse.ClearedInvoice;

            XmlSerializer serializer = new(typeof(Invoice));

            using StringReader reader = new(clearedInvoiceXml);

            var clearedInvoice = (Invoice)await Task.Run(() => serializer.Deserialize(reader));

            var qrCodeNode = clearedInvoice?.AdditionalDocumentReference?
                .FirstOrDefault(docRef => docRef.ID.Value == "QR")?.Attachment?.EmbeddedDocumentBinaryObject;

            if (qrCodeNode != null)
            {
                model.Base64QrCode = qrCodeNode.Value;
            }
        }

        private static void UpdateModelWithApiResponse(ApprovedInvoice model, ServerResult apiResponse,
            CertificateInfo certInfo, HttpResponseMessage response, bool IsClearance)
        {
            apiResponse.RequestType = IsClearance ? "Invoice Clearance" : "Invoice Reporting";
            apiResponse.RequestUri = IsClearance ? certInfo.ClearanceUrl : certInfo.ReportingUrl;
            apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

            model.RequestType = apiResponse.RequestType;
            model.StatusCode = apiResponse.StatusCode;
            model.ApprovalStatus = IsClearance ? apiResponse.ClearanceStatus : apiResponse.ReportingStatus;
            model.EnvironmentType = certInfo.EnvironmentType;

            model.ServerResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                (model.ApprovalStatus != null && model.ApprovalStatus.Contains("NOT")))
            {
                model.ApprovalStatus = "REJECTED";
            }

            model.Timestamp = DateTime.Now;
        }

        private void PrepareTempData(ApprovedInvoice model, CertificateInfo certInfo)
        {

            string approvalStatus = model.ApprovalStatus.Contains("NOT") ? "REJECTED" : model.ApprovalStatus;

            model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.ApprovedInvoiceGuid, approvalStatus);
            model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.ZatcaUUIDGuid, model.ZatcaUUID);

            model.EditData = JsonParser.ModifyStringInEditData(model.EditData, "BusinessDetails", ManagerCustomField.LastIcvGuid, model.ICV.ToString());
            model.EditData = JsonParser.ModifyStringInEditData(model.EditData, "BusinessDetails", ManagerCustomField.LastPihGuid, model.InvoiceHash);

            if (!approvalStatus.Equals("REJECTED"))
            {
                model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.QrCodeGuid, model.Base64QrCode);
            }

            TempData.Clear();

            string sanitizedReference = new(model.Reference.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            string fileName = $"{model.IssueDate}_{sanitizedReference}_{model.ZatcaUUID}.txt";

            string fileContent = GenerateInvoiceInfo(model);

            TempData["StringFileContent"] = fileContent;
            TempData["StringFileName"] = model.XmlFileName.Replace(".xml", $"_{(int)model.EnvironmentType}.txt");


            var businessDetailsPayload = JsonParser.FindValueByKey(JObject.Parse(model.EditData), "BusinessDetails");

            var apiReference = new
            {
                ApiUrl = $"{certInfo.ApiEndpoint}/business-details-form/38cf4712-6e95-4ce1-b53a-bff03edad273",
                SecretKey = certInfo.ApiSecret,
                Payload = businessDetailsPayload
            };

            TempData["ApiReference"] = apiReference;

            var invoiceData = JsonParser.FindValueByKey(JObject.Parse(model.EditData), model.ManagerUUID)?.ToString();

            var apiInvoice = new
            {
                ApiUrl = UrlHelper.ConstructInvoiceApiUrl(model.Referrer, model.ManagerUUID),
                SecretKey = certInfo.ApiSecret,
                Payload = invoiceData
            };

            TempData["ApiInvoice"] = apiInvoice;
        }

        private static void HandleUnsuccessfulResponse(ApprovedInvoice model, CertificateInfo certInfo,
            HttpResponseMessage response, string resultContent, bool IsClearance)
        {
            var apiBadResponse = JsonConvert.DeserializeObject<ApiBadResponse>(resultContent);
            apiBadResponse.RequestUri = IsClearance ? certInfo.ClearanceUrl : certInfo.ReportingUrl;
            apiBadResponse.RequestType = IsClearance ? "Invoice Clearance" : "Invoice Reporting";
            apiBadResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";
            model.ServerResult = JsonConvert.SerializeObject(apiBadResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private static string GenerateInvoiceInfo(ApprovedInvoice model)
        {
            string InvoiceInfo = $"ManagerUUID: \n{model.ManagerUUID}\n\n";
            InvoiceInfo += $"PartyName: \n{model.PartyName}\n\n";
            InvoiceInfo += $"ReferenceNumber: \n{model.Reference}\n\n";
            InvoiceInfo += $"IssueDate: \n{model.IssueDate}\n\n\n";

            InvoiceInfo += $"InvoiceType: \n{model.InvoiceType}\n\n";
            InvoiceInfo += $"InvoiceSubType: \n{model.InvoiceSubType}\n\n\n";

            InvoiceInfo += $"CurrencyCode: \n{model.CurrencyCode}\n\n";
            InvoiceInfo += $"Amount: \n{model.Amount}\n\n";
            InvoiceInfo += $"TaxAmount: \n{model.TaxAmount}\n\n";
            InvoiceInfo += $"TotalAmount: \n{model.TotalAmount}\n\n\n";

            InvoiceInfo += $"ApprovalStatus: \n{model.ApprovalStatus}\n\n";
            InvoiceInfo += $"EnvironmentType: \n{model.EnvironmentType}\n\n\n";

            InvoiceInfo += $"ICV: \n{model.ICV}\n\n";
            InvoiceInfo += $"InvoiceHash: \n{model.InvoiceHash}\n\n";
            InvoiceInfo += $"ZatcaUUID: \n{model.ZatcaUUID}\n\n";
            InvoiceInfo += $"Base64Invoice: \n{model.Base64Invoice}\n\n\n";

            var formattedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(model.ServerResult), Formatting.Indented);
            InvoiceInfo += $"ServerResult: \n{formattedJson}\n\n\n";

            InvoiceInfo += $"TimeStamp: \n{model.Timestamp}\n";

            return InvoiceInfo;
        }
    }

}




