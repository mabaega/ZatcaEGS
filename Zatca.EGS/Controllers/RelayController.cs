using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using Zatca.EGS.Helpers;
using Zatca.EGS.Models;
using Zatca.eInvoice;
using Zatca.eInvoice.Models;

namespace Zatca.EGS.Controllers
{
    [ApiController]
    public class RelayController : Controller
    {
        private readonly HttpClient _httpClient = new();

        [HttpPost("relay")]
        public async Task<IActionResult> ProcessFormData([FromForm] Dictionary<string, string> formData)
        {
            try
            {
                var relayData = new RelayData(formData);

                var approvalStatus = relayData.ApprovalStatus;

                if (!string.IsNullOrEmpty(approvalStatus) && !approvalStatus.Contains("REJECTED"))
                {
                    var RelayModel = new RelayViewModel
                    {
                        ZatcaUUID = relayData.ZatcaUUID,
                        Base64QrCode = relayData.Base64QrCode,
                        ReferrerLink = relayData.Referrer,
                        ShowSetupLink = false,
                    };

                    return View("Info", RelayModel);
                }


                var _certInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(relayData.CertInfoString);


                if (_certInfo == null)
                {
                    var CertificateViewModel = new CertificateViewModel
                    {
                        ReferrerLink = relayData.Referrer,
                        ShowSetupLink = true,
                    };

                    return View("Certificate", CertificateViewModel);
                }


                (string icv, string pih) = await ZatcaReference.GetReferenceFolder(_certInfo.ApiEndpoint, _certInfo.ApiSecret);

                if(string.IsNullOrEmpty(icv) || string.IsNullOrEmpty(pih))
                {
                    var errorModel = new ErrorViewModel
                    {
                        ErrorMessage = $"Can't read LastICV and LastPIH from Business Data!! \nMake sure AccessToken In Busines Detail is still valid, and ZatcaReference in Folders Tab has value.",
                        ReferrerLink = formData.GetValueOrDefault("Referrer")
                    };
                    return View("Error", errorModel);
                }

                relayData.LastICV = int.TryParse(icv, out int icvNumber) ? icvNumber : relayData.LastICV;
                relayData.LastPIH = pih;
                

                string zatcaUUID = relayData.Key;

                if (!string.IsNullOrEmpty(approvalStatus) && approvalStatus.Contains("REJECTED"))
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

                    zatcaUUID = string.Concat(zatcaUUID.AsSpan(0, relayData.Key.Length - 12), sNumber);
                }

                relayData.ZatcaUUID = zatcaUUID;

                var mapper = new RelayToInvoiceMapper(relayData);
                Invoice invoiceObject = mapper.GenerateInvoiceObject();

                InvoiceGenerator ig = new(
                    invoiceObject,
                    Encoding.UTF8.GetString(Convert.FromBase64String(_certInfo.PCSIDBinaryToken)),
                    _certInfo.EcSecp256k1Privkeypem
                );

                ig.GetSignedInvoiceXML(out string InvoiceHash, out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out ZatcaRequestApi requestApi);

                ManagerInvoice managerInvoice = relayData.ManagerInvoice;
                int ICV = relayData.LastICV + 1;

                var amount = invoiceObject.LegalMonetaryTotal.TaxExclusiveAmount.NumericValue;
                var totalAmount = invoiceObject.LegalMonetaryTotal.TaxInclusiveAmount.NumericValue;
                var taxAmount = totalAmount - amount;

                ApprovedInvoice approvedInvoice = new()
                {
                    ManagerUUID = relayData.Key,

                    ZatcaUUID = zatcaUUID,
                    InvoiceHash = InvoiceHash,
                    Base64SignedInvoice = base64SignedInvoice,

                    InvoiceType = invoiceObject.InvoiceTypeCode?.Value,
                    InvoiceSubType = invoiceObject.InvoiceTypeCode?.Name,
                    Reference = managerInvoice?.Reference,
                    IssueDate = managerInvoice?.IssueDate.ToString("yyyy-MM-dd"),
                    PartyName = managerInvoice?.InvoiceParty?.Name,
                    CurrencyCode = managerInvoice?.InvoiceParty?.Currency?.Code ?? "SAR",
                    Amount = amount,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,

                    Base64QrCode = base64QrCode,
                    XmlFileName = XmlFileName,
                    Referrer = relayData.Referrer,
                    CallBack = relayData.Callback,
                    ICV = ICV,

                    Timestamp = DateTime.Now,
                    EditData = relayData.Data,

                    EnvironmentType = _certInfo.EnvironmentType,

                    CertificateInfo = relayData.CertInfoString
                };

                return View("Index", approvedInvoice);

            }
            catch
            {
                var errorModel = new ErrorViewModel
                {
                    ErrorMessage = @"Please review the Device Settings in the EGS application to ensure it matches your business data setup.",
                    ReferrerLink = formData.GetValueOrDefault("Referrer")
                };
                return View("Error", errorModel);
            }
        }


        [HttpPost("compliance-check")]
        public async Task<IActionResult> ComplianceCheck([FromForm] ApprovedInvoice model)
        {
            try
            {
                var _certInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(model.CertificateInfo) ?? throw new InvalidOperationException("CertificateInfo is not available. Make sure to call ProcessFormData first.");

                // We Use SanBox Portal to Validate Invoice

                string SDK_CCSIDBinaryToken = "TUlJQ05EQ0NBZHFnQXdJQkFnSUdBWkc0alp2a01Bb0dDQ3FHU000OUJBTUNNQlV4RXpBUkJnTlZCQU1NQ21WSmJuWnZhV05wYm1jd0hoY05NalF3T1RBek1UVTBNalE0V2hjTk1qa3dPVEF5TWpFd01EQXdXakJ2TVFzd0NRWURWUVFHRXdKVFFURVBNQTBHQTFVRUN3d0dVbWw1WVdSb01TWXdKQVlEVlFRS0RCMU5ZWGhwYlhWdElGTndaV1ZrSUZSbFkyZ2dVM1Z3Y0d4NUlFeFVSREVuTUNVR0ExVUVBd3dlVFU1SExURXdNVEF3TVRBd01EQXRNems1T1RrNU9UazVPREF3TURBek1GWXdFQVlIS29aSXpqMENBUVlGSzRFRUFBb0RRZ0FFT1BwdCtHenozcjFWTVBTZ1pZSHQxQkQvcGQyYzVrbEp1VmJHbkwycGtuS1d0b1dyejUvVUlGQ2JnaUN5anpLVTB2WEExVG9nQ3Q5VXVDcnozVUpTbktPQnZqQ0J1ekFNQmdOVkhSTUJBZjhFQWpBQU1JR3FCZ05WSFJFRWdhSXdnWitrZ1p3d2daa3hPekE1QmdOVkJBUU1NakV0UlVkVGZESXRUVTVIZkRNdE1HRXpOalV6TlRFdE1qZzVNUzAwTnpCakxUZzBZelF0TlRRMlpUSmtOakV4TkRZMU1SOHdIUVlLQ1pJbWlaUHlMR1FCQVF3UE16azVPVGs1T1RrNU9EQXdNREF6TVEwd0N3WURWUVFNREFReE1UQXdNUTR3REFZRFZRUWFEQVV5TXpNek16RWFNQmdHQTFVRUR3d1JVM1Z3Y0d4NUlHRmpkR2wyYVhScFpYTXdDZ1lJS29aSXpqMEVBd0lEU0FBd1JRSWhBTDJITnNhaVdENEJLYkwxc2pqeWdqUlpSRFdZekxNclJ4dEtlemYvdTVyY0FpQTVWbjh4REdRV1JXNm1LQnRROHBtNS9jS2hIZW9ZTmpQUkNWdjF3UTNVVFE9PQ==";
                string SDK_CCSIDSecret = "X+lO9bFc4PfAth8jb22vxcsKaiDAFsrgE7PI5q6+txk=";
                string SDK_ComplianceCheckUrl = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance/invoices";


                ZatcaRequestApi zatcaRequestApi = new()
                {
                    uuid = model.ZatcaUUID,
                    invoiceHash = model.InvoiceHash,
                    invoice = model.Base64SignedInvoice
                };

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

                model.RequestType = apiResponse.RequestType;
                model.StatusCode = apiResponse.StatusCode;

                model.ApprovalStatus = string.IsNullOrEmpty(apiResponse.ClearanceStatus) ? apiResponse.ReportingStatus : apiResponse.ClearanceStatus;

                model.EnvironmentType = _certInfo.EnvironmentType;

                model.ServerResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                return View("Index", model);

            }
            catch (Exception ex)
            {
                model.ServerResult = ex.Message;
                return View("Index", model);
            }
        }

        [HttpPost("clearance")]
        public async Task<IActionResult> Clearance([FromForm] ApprovedInvoice model)
        {
            var _certInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(model.CertificateInfo) ?? throw new InvalidOperationException("CertificateInfo is not available. Make sure to call ProcessFormData first.");

            var testapi = await CheckMangerApiAsync(model.Referrer, _certInfo.ApiSecret);

            if (testapi)
            {
                try
                {
                    ZatcaRequestApi zatcaRequestApi = new()
                    {
                        uuid = model.ZatcaUUID,
                        invoiceHash = model.InvoiceHash,
                        invoice = model.Base64SignedInvoice
                    };

                    var jsonContent = JsonConvert.SerializeObject(zatcaRequestApi);

                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                    _httpClient.DefaultRequestHeaders.Add("Clearance-Status", "1");
                    _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_certInfo.PCSIDBinaryToken}:{_certInfo.PCSIDSecret}")));

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_certInfo.ClearanceUrl, content);
                    var resultContent = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                        apiResponse ??= new ServerResult();

                        if (apiResponse.ClearedInvoice != null)
                        {
                            var clearedInvoiceXml = Encoding.UTF8.GetString(Convert.FromBase64String(apiResponse.ClearedInvoice));

                            model.Base64SignedInvoice = apiResponse.ClearedInvoice;

                            XmlSerializer serializer = new(typeof(Invoice));
                            using StringReader reader = new(clearedInvoiceXml);
                            var clearedInvoice = (Invoice)serializer.Deserialize(reader);

                            var qrCodeNode = clearedInvoice?.AdditionalDocumentReference?
                                .FirstOrDefault(docRef => docRef.ID.Value == "QR")?.Attachment?.EmbeddedDocumentBinaryObject;

                            if (qrCodeNode != null)
                            {
                                model.Base64QrCode = qrCodeNode.Value;
                            }
                        }

                        apiResponse.RequestType = "Invoice Clearance";
                        apiResponse.RequestUri = _certInfo.ClearanceUrl;
                        apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

                        model.RequestType = apiResponse.RequestType;
                        model.StatusCode = apiResponse.StatusCode;
                        model.ApprovalStatus = apiResponse.ClearanceStatus;
                        model.EnvironmentType = _certInfo.EnvironmentType;

                        model.ServerResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest || (apiResponse.ClearanceStatus != null && apiResponse.ClearanceStatus.Contains("NOT")))
                        {
                            model.ApprovalStatus = "REJECTED";
                        }

                        model.Timestamp = DateTime.Now;

                        string approvalStatus = model.ApprovalStatus.Contains("NOT") ? "REJECTED" : model.ApprovalStatus;
                        model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.ApprovedInvoiceGuid, approvalStatus);
                        model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.ZatcaUUIDGuid, model.ZatcaUUID);

                        if (!approvalStatus.Equals("REJECTED"))
                        {
                            model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.QrCodeGuid, model.Base64QrCode);
                        }

                        bool updateBusinessData = await ZatcaReference.UpdateReferenceFolder(_certInfo.ApiEndpoint, _certInfo.ApiSecret, model.ICV.ToString(), model.InvoiceHash);

                        if (updateBusinessData)
                        {
                            string sanitizedReference = new(model.Reference.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
                            string fileName = $"{model.IssueDate}_{sanitizedReference}_{model.ZatcaUUID}.txt";

                            string fileContent = await ZatcaReference.UpdateInvoice(_certInfo, model);

                            TempData.Clear();
                            TempData["StringFileContent"] = fileContent;
                            TempData["StringFileName"] = fileName;
                        }
                    }
                    else
                    {
                        var apiBadResponse = JsonConvert.DeserializeObject<ApiBadResponse>(resultContent);
                        apiBadResponse.RequestUri = _certInfo.ClearanceUrl;
                        apiBadResponse.RequestType = "Invoice Clearance";
                        model.ServerResult = JsonConvert.SerializeObject(apiBadResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    }

                    return View("Index", model);
                }
                catch (Exception ex)
                {
                    model.ServerResult = ex.Message;
                    return View("Index", model);
                }
            }
            else
            {
                model.ServerResult = "Access Token in certificate not valid, create new Access Token and copy Secret to busines details and try to reporting/clearance again";
                return View("Index", model);
            }
        }

        [HttpPost("reporting")]
        public async Task<IActionResult> Reporting([FromForm] ApprovedInvoice model)
        {
            {
                var _certInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(model.CertificateInfo) ?? throw new InvalidOperationException("CertificateInfo is not available. Make sure to call ProcessFormData first.");

                var testapi = await CheckMangerApiAsync(model.Referrer, _certInfo.ApiSecret);

                if (testapi)
                {
                    try
                    {
                        ZatcaRequestApi zatcaRequestApi = new()
                        {
                            uuid = model.ZatcaUUID,
                            invoiceHash = model.InvoiceHash,
                            invoice = model.Base64SignedInvoice
                        };

                        var jsonContent = JsonConvert.SerializeObject(zatcaRequestApi);

                        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                        _httpClient.DefaultRequestHeaders.Add("Clearance-Status", "0");
                        _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_certInfo.PCSIDBinaryToken}:{_certInfo.PCSIDSecret}")));

                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        var response = await _httpClient.PostAsync(_certInfo.ReportingUrl, content);

                        var resultContent = await response.Content.ReadAsStringAsync();

                        if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                            apiResponse ??= new ServerResult(); ;

                            apiResponse.RequestUri = _certInfo.ReportingUrl;
                            apiResponse.RequestType = "Invoice Reporting";
                            apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

                            model.RequestType = apiResponse.RequestType;
                            model.StatusCode = apiResponse.StatusCode;

                            model.ApprovalStatus = apiResponse.ReportingStatus;

                            model.EnvironmentType = _certInfo.EnvironmentType;

                            model.ServerResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest || apiResponse.ReportingStatus.Contains("NOT"))
                            {
                                model.ApprovalStatus = "REJECTED";
                            }

                            model.Timestamp = DateTime.Now;

                            string approvalStatus = model.ApprovalStatus.Contains("NOT") ? "REJECTED" : model.ApprovalStatus;

                            model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.ApprovedInvoiceGuid, approvalStatus);
                            model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.ZatcaUUIDGuid, model.ZatcaUUID);

                            if (!approvalStatus.Equals("REJECTED"))
                            {
                                model.EditData = JsonParser.ModifyStringInEditData(model.EditData, model.ManagerUUID, ManagerCustomField.QrCodeGuid, model.Base64QrCode);
                            }

                            bool updateBusinessData = await ZatcaReference.UpdateReferenceFolder(_certInfo.ApiEndpoint, _certInfo.ApiSecret, model.ICV.ToString(), model.InvoiceHash);

                            //Console.WriteLine(updateBusinessData);

                            if (updateBusinessData)
                            {
                                string sanitizedReference = new(model.Reference.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
                                string fileName = $"{model.IssueDate}_{sanitizedReference}_{model.ZatcaUUID}.txt";

                                string fileContent = await ZatcaReference.UpdateInvoice(_certInfo, model);

                                TempData.Clear();
                                TempData["StringFileContent"] = fileContent;
                                TempData["StringFileName"] = fileName;
                            }

                        }
                        else
                        {
                            var apiBadResponse = JsonConvert.DeserializeObject<ApiBadResponse>(resultContent);
                            apiBadResponse.RequestUri = _certInfo.ReportingUrl;
                            apiBadResponse.RequestType = "Invoice Reporting";
                            model.ServerResult = JsonConvert.SerializeObject(apiBadResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        }

                        return View("Index", model);
                    }

                    catch (Exception ex)
                    {
                        model.ServerResult = ex.Message;
                        return View("Index", model);
                    }
                }
                else
                {
                    model.ServerResult = "Access Token in certificate not valid, create new Access Token and copy Secret to busines details and try to reporting again";
                    return View("Index", model);
                }
            }
        }

        private static async Task<bool> CheckMangerApiAsync(string Referrer, string apiSecret)
        {
            try
            {

                var apiUrl = UrlHelper.CheckApiUrl(Referrer);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-KEY", apiSecret);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}



