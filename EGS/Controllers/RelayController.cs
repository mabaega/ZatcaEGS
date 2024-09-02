using Microsoft.AspNetCore.Mvc;
using EGS.Models;
using Zatca.eInvoice.Models;
using EGS.Helpers;
using System.Text;
using Zatca.eInvoice;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;
using System.Reflection;



namespace EGS.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class RelayController : Controller
    {
        private CertificateInfo GetCertificateInfoFromSession()
        {
            var certInfoJson = HttpContext.Session.GetString("CertInfo");
            return string.IsNullOrEmpty(certInfoJson) ? null : JsonConvert.DeserializeObject<CertificateInfo>(certInfoJson);
        }

        private void SetCertificateInfoInSession(CertificateInfo certInfo)
        {
            var certInfoJson = JsonConvert.SerializeObject(certInfo);
            HttpContext.Session.SetString("CertInfo", certInfoJson);
        }

        private readonly HttpClient _httpClient = new();

        [HttpPost("relay")]
        public IActionResult ProcessFormData([FromForm] Dictionary<string, string> formData)
        {
            try
            {
                var relayData = new RelayData
                {
                    Referrer = formData.GetValueOrDefault("Referrer"),
                    Key = formData.GetValueOrDefault("Key"),
                    Data = formData.GetValueOrDefault("Data"),
                    Callback = formData.GetValueOrDefault("Callback")
                };

                int _iCV = 1;
                string _PIH = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";

                var _certInfo = DataHelper.GetCerificateInfo(relayData.Data, ZatcaCustomField.CertificateInfoGuid);

                if (_certInfo == null)
                {
                    var errorModel = new ErrorViewModel
                    {
                        ErrorMessage = @"
                            Cerifikat tidak ditemukan.<br/>
                            Anda perlu melakukan Onbording Device untuk bisnis anda!",
                        ReferrerLink = relayData.Referrer,
                        ShowSetupLink = true
                    };

                    return View("Error", errorModel);
                }

                SetCertificateInfoInSession(_certInfo);

                string _managerData = relayData.Data;

                string IcvPih = DataHelper.FindStringValueByKey(JObject.Parse(relayData.Data), ZatcaCustomField.IcvPihGuid);

                if (!string.IsNullOrEmpty(IcvPih) && IcvPih.Contains('#'))
                {
                    string[] parts = IcvPih.Split('#');
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0], out int iCVValue))
                        {
                            _iCV = iCVValue + 1;
                            _PIH = parts[1];
                        }
                    }
                }


                ApprovedInvoice approvedInvoice = DataHelper.GetApprovedInvoice(relayData.Data, ZatcaCustomField.ApprovedInvoiceGuid);

                if (approvedInvoice != null && approvedInvoice.ManagerUUID == relayData.Key && (approvedInvoice.ApprovalStatus == "CLEARED" || approvedInvoice.ApprovalStatus == "REPORTED"))
                {
                    approvedInvoice.Referrer = relayData.Referrer;
                    approvedInvoice.CallBack = relayData.Callback;

                    approvedInvoice.EditData = DataHelper.ModifyStringInEditData(relayData.Data, relayData.Key, ZatcaCustomField.QrCodeGuid, approvedInvoice.Base64QrCode);
                    string ApprovedInfo = DataHelper.FindStringValueByKey(JObject.Parse(relayData.Data), ZatcaCustomField.ApprovedInvoiceGuid) ?? "";
                    approvedInvoice.EditData = DataHelper.ModifyStringInEditData(relayData.Data, relayData.Key, ZatcaCustomField.ApprovedInvoiceGuid, ApprovedInfo);


                    approvedInvoice.RequestType += " --- FROM INVOICE LOG ---";
                    ServerResult serverResult = JsonConvert.DeserializeObject<ServerResult>(approvedInvoice.ServerResult);
                    if (serverResult != null)
                    {
                        serverResult.RequestType += " --- FROM INVOICE LOG ---";
                    }
                    else
                    {
                        throw new InvalidOperationException("Gagal mendeserialisasi JSON ke objek ServerResult.");
                    }
                    approvedInvoice.ServerResult = JsonConvert.SerializeObject(serverResult, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    return View("Index", approvedInvoice);
                }
                else
                {
                    string zatcaUUID = relayData.Key;

                    if (approvedInvoice != null && approvedInvoice.ApprovalStatus.Contains("NOT"))
                    {
                        var sNumber = approvedInvoice.ZatcaUUID.Substring(relayData.Key.Length - 12, 12);

                        if (int.TryParse(sNumber, out int numericValue))
                        {
                            sNumber = (numericValue + 1).ToString("D12");
                        }
                        else
                        {
                            sNumber = "000000000001";
                        }

                        zatcaUUID = relayData.Key.Substring(0, relayData.Key.Length - 12) + sNumber;
                    }

                    var mapper = new RelayToInvoiceMapper(relayData, _certInfo, zatcaUUID, _iCV, _PIH);
                    Invoice invoice = mapper.GenerateInvoiceObject();

                    InvoiceGenerator ig = new(
                        invoice,
                        Encoding.UTF8.GetString(Convert.FromBase64String(_certInfo.PCSIDBinaryToken)),
                        _certInfo.EcSecp256k1Privkeypem
                    );

                    ig.GetSignedInvoiceXML(out string invoiceHash, out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out string requestApi);

                    ManagerInvoice managerInvoice = DataHelper.GetManagerInvoice(relayData.Data, relayData.Key);

                    var editData = DataHelper.ModifyStringInEditData(relayData.Data, relayData.Key, ZatcaCustomField.QrCodeGuid, base64QrCode);
                    var amount = invoice.LegalMonetaryTotal.TaxExclusiveAmount.NumericValue;
                    var totalAmount = invoice.LegalMonetaryTotal.TaxInclusiveAmount.NumericValue;
                    var taxAmount = totalAmount - amount;

                    approvedInvoice = new ApprovedInvoice
                    {
                        ZatcaUUID = zatcaUUID,
                        ManagerUUID = relayData.Key,
                        InvoiceType = invoice.InvoiceTypeCode?.Value,
                        InvoiceSubType = invoice.InvoiceTypeCode?.Name,
                        Reference = managerInvoice?.Reference,
                        IssueDate = managerInvoice?.IssueDate.ToString("yyyy-MM-dd"),
                        PartyName = managerInvoice?.InvoiceParty?.Name,
                        CurrencyCode = managerInvoice?.InvoiceParty?.Currency?.Code ?? "SAR",
                        Amount = amount,
                        TaxAmount = taxAmount,
                        TotalAmount = totalAmount,
                        Base64Invoice = Convert.ToBase64String(Encoding.UTF8.GetBytes(relayData.Data)),
                        Referrer = relayData.Referrer,
                        CallBack = relayData.Callback,
                        ICV = _iCV,
                        PIH = _PIH,
                        InvoiceHash = invoiceHash,
                        Base64SignedInvoice = base64SignedInvoice,
                        Base64QrCode = base64QrCode,
                        XmlFileName = XmlFileName,
                        Timestamp = DateTime.Now,
                        EditData = editData,
                        EnvironmentType = _certInfo.EnvironmentType,
                    };

                    return View("Index", approvedInvoice);
                }
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
                var _certInfo = GetCertificateInfoFromSession();

                if (_certInfo == null)
                {
                    throw new InvalidOperationException("CertificateInfo is not available. Make sure to call ProcessFormData first.");
                }

                ZatcaRequestApi zatcaRequestApi = new()
                {
                    Uuid = model.ZatcaUUID,
                    InvoiceHash = model.InvoiceHash,
                    Invoice = model.Base64SignedInvoice
                };

                var jsonContent = JsonConvert.SerializeObject(zatcaRequestApi);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
                _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_certInfo.CCSIDBinaryToken}:{_certInfo.CCSIDSecret}")));

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_certInfo.ComplianceCheckUrl, content);

                var resultContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);

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
            var testapi = await CheckMangerApiAsync(model.Referrer);
            if (testapi)
            {
                try
                {
                    var _certInfo = GetCertificateInfoFromSession();

                    if (_certInfo == null)
                    {
                        throw new InvalidOperationException("CertificateInfo is not available. Make sure to call ProcessFormData first.");
                    }

                    ZatcaRequestApi zatcaRequestApi = new()
                    {
                        Uuid = model.ZatcaUUID,
                        InvoiceHash = model.InvoiceHash,
                        Invoice = model.Base64SignedInvoice
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
                    var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                    if (apiResponse != null && apiResponse.ClearedInvoice != null)
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
                    apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

                    model.RequestType = apiResponse.RequestType;
                    model.StatusCode = apiResponse.StatusCode;

                    model.ApprovalStatus = apiResponse.ClearanceStatus;

                    model.EnvironmentType = _certInfo.EnvironmentType;

                    model.ServerResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    if (model.ApprovalStatus.Contains("CLEARED"))
                    {
                        model.Timestamp = DateTime.Now;
                        model.EditData = DataHelper.ModifyStringInEditData(model.EditData, model.ManagerUUID, ZatcaCustomField.QrCodeGuid, model.Base64QrCode);
                        model.EditData = DataHelper.ModifyStringInEditData(model.EditData, model.ManagerUUID, ZatcaCustomField.ApprovedInvoiceGuid, ObjectCompressor.SerializeToBase64String(model));
                        model.EditData = DataHelper.ModifyStringInEditData(model.EditData, model.ManagerUUID, ZatcaCustomField.CertificateInfoGuid, ObjectCompressor.SerializeToBase64String(_certInfo));
                        model.EditData = DataHelper.ModifyStringInEditData(model.EditData, model.ManagerUUID, ZatcaCustomField.IcvPihGuid, model.ICV + '#' + model.InvoiceHash);

                        using (HttpClient client = new HttpClient())
                        {
                            var postData = new FormUrlEncodedContent(new[]
                            {
                            new KeyValuePair<string, string>("Edit", model.EditData)
                        });

                            var clientResponse = await client.PostAsync(model.CallBack, postData);
                            // Handle the response if needed
                            if (!clientResponse.IsSuccessStatusCode)
                            {
                                model.UpdateInvoiceStatus = "Failed to send data back to the Manager.";
                                throw new Exception("Failed to send data back to the client.");
                            }
                            model.UpdateInvoiceStatus = "Updated!";
                        }
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
                var testapi = await CheckMangerApiAsync(model.Referrer);
                if (testapi)
                {


                    try
                    {
                        var _certInfo = GetCertificateInfoFromSession();

                        if (_certInfo == null)
                        {
                            throw new InvalidOperationException("CertificateInfo is not available. Make sure to call ProcessFormData first.");
                        }

                        ZatcaRequestApi zatcaRequestApi = new()
                        {
                            Uuid = model.ZatcaUUID,
                            InvoiceHash = model.InvoiceHash,
                            Invoice = model.Base64SignedInvoice
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
                        var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                        apiResponse.RequestType = "Invoice Reporting";
                        apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

                        model.RequestType = apiResponse.RequestType;
                        model.StatusCode = apiResponse.StatusCode;

                        model.ApprovalStatus = apiResponse.ReportingStatus;

                        model.EnvironmentType = _certInfo.EnvironmentType;

                        model.ServerResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        if (model.ApprovalStatus.Contains("REPORTED"))
                        {
                            model.Timestamp = DateTime.Now;
                            model.EditData = DataHelper.ModifyStringInEditData(model.EditData, model.ManagerUUID, ZatcaCustomField.QrCodeGuid, model.Base64QrCode);
                            model.EditData = DataHelper.ModifyStringInEditData(model.EditData, model.ManagerUUID, ZatcaCustomField.ApprovedInvoiceGuid, ObjectCompressor.SerializeToBase64String(model));
                            model.EditData = DataHelper.ModifyStringInEditData(model.EditData, model.ManagerUUID, ZatcaCustomField.CertificateInfoGuid, ObjectCompressor.SerializeToBase64String(_certInfo));
                            model.EditData = DataHelper.ModifyStringInEditData(model.EditData, model.ManagerUUID, ZatcaCustomField.IcvPihGuid, model.ICV + '#' + model.InvoiceHash);

                            using (HttpClient client = new HttpClient())
                            {
                                var postData = new FormUrlEncodedContent(new[]
                                {
                            new KeyValuePair<string, string>("Data", model.EditData)
                        });

                                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                                var clientResponse = await client.PostAsync(model.CallBack, postData);

                                if (!clientResponse.IsSuccessStatusCode)
                                {
                                    model.UpdateInvoiceStatus = "Failed to send data back to the Manager.";
                                    throw new Exception("Failed to send data back to the client.");
                                }
                                model.UpdateInvoiceStatus = "Updated!";
                            }
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

        [HttpPost("update-invoice")]
        public async Task<IActionResult> UpdateInvoice([FromForm] ApprovedInvoice model)
        {
            try
            {
                var _certInfo = GetCertificateInfoFromSession();

                if (_certInfo == null)
                {
                    throw new InvalidOperationException("CertificateInfo is not available. Make sure to call ProcessFormData first.");
                }

                var apiUrl = DataHelper.ConstructApiUrl(model.Referrer, model.ManagerUUID);

                string filecontent = ObjectCompressor.SerializeToBase64String(model);
                //Created file download

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-KEY", _certInfo.ApiSecret);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var inv = DataHelper.FindValueByKey(JObject.Parse(model.EditData), model.ManagerUUID).ToString();

                    var payload = inv;

                    var content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync(apiUrl, content);

                    ServerResult serverResult = new();

                    if (response.IsSuccessStatusCode)
                    {
                        serverResult.Message = $"Manager invoice successfully updated. \nPlease return to the manager to view the results.";
                    }
                    else
                    {
                        serverResult.Message = $"Failed to update manager invoice. \nPlease ensure that the Access Token recorded in the Gateway Settings is correct and still valid.\n\n" +
                                       $"If the issue persists, you may need to manually copy the following content into your manager invoice:\n\n{filecontent}";
                        byte[] fileBytes = Encoding.UTF8.GetBytes(filecontent);
                        string fileName = "UpdatedInvoice.txt";

                        // Kembalikan file ke pengguna
                        return File(fileBytes, "application/octet-stream", fileName);
                    }

                    model.ServerResult = JsonConvert.SerializeObject(serverResult, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                }

                return View("Index", model);
            }

            catch (Exception ex)
            {
                model.ServerResult = ex.Message;
                return View("Index", model);
            }
        }

        private async Task<bool> CheckMangerApiAsync(string Referrer)
        {
            try
            {
                var _certInfo = GetCertificateInfoFromSession();

                if (_certInfo == null)
                {
                    throw new InvalidOperationException("CertificateInfo is not available. Make sure to call ProcessFormData first.");
                }

                var apiUrl = DataHelper.CheckApiUrl(Referrer);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-KEY", _certInfo.ApiSecret);
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



