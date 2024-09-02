using Microsoft.AspNetCore.Mvc;
using ZatcaEGS.Models;
using Zatca.eInvoice.Models;
using ZatcaEGS.Helpers;
using System.Text;
using Zatca.eInvoice;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Xml.Serialization;

namespace ZatcaEGS.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class RelayController : Controller 
    {
        private readonly AppDbContext _dbContext;
        private readonly DeviceSetup _deviceSetup;
        private readonly HttpClient _httpClient = new ();
        public RelayController(AppDbContext dbContext) 
        {
            _dbContext = dbContext;
            _deviceSetup = _dbContext.DeviceSetups.OrderBy(x => x.RowId).FirstOrDefault() ?? new DeviceSetup();
        }

        [HttpPost("processform")]
        public IActionResult ProcessFormData([FromForm] Dictionary<string, string> formData)
        {
            try
            {

                var relayData = new RelayData
                {
                    Referrer = formData.GetValueOrDefault("Referrer"),
                    Key = formData.GetValueOrDefault("Key"),
                    View = formData.GetValueOrDefault("View"),
                    Edit = formData.GetValueOrDefault("Edit"),
                    HiddenFields = formData
                                 .Where(x => x.Key != "Referrer" && x.Key != "Key" && x.Key != "View" && x.Key != "Edit")
                                 .ToDictionary(x => x.Key, x => x.Value)
                };


                if (!InvoiceHelper.ValidateCompanyID(relayData.HiddenFields, _deviceSetup.BusinessDatabaseGuid(), _deviceSetup.CompanyID, out string validationMessage))
                {
                    var errorModel = new ErrorViewModel
                    {
                        ErrorMessage = @"
                            The data received does not meet the required specifications.<br/>
                            Please review the Device Settings in the ZatcaEGS application to ensure it matches your business data setup.",
                        ReferrerLink = relayData.Referrer
                    };
                    return View("Error", errorModel);
                }

                ApprovedInvoice approvedInvoice = _dbContext.ApprovedInvoices
                                .FirstOrDefault(invoice => invoice.ManagerUUID == relayData.Key &&
                                (invoice.ApprovalStatus == "CLEARED" || invoice.ApprovalStatus == "REPORTED"));


                if (approvedInvoice != null)
                {
                    approvedInvoice.RequestType += " --- FROM INVOICE LOG ---";
                    approvedInvoice.Referrer = relayData.Referrer;
                    return View("Index", approvedInvoice);
                }
                else
                {
                    (int ICV, string PIH) = InvoiceHelper.GetLastICVandPIH(_dbContext);
                    
                    string zatcaUUID = InvoiceHelper.GetZatcaUUID(_dbContext, relayData.Key);

                    Invoice invoice = RelayToInvoiceMapper.GenerateInvoiceObject(relayData, _deviceSetup, zatcaUUID, ICV, PIH);

                    InvoiceGenerator ig = new(
                        invoice,
                        Encoding.UTF8.GetString(Convert.FromBase64String(_deviceSetup.PCSIDBinaryToken)),
                        _deviceSetup.EcSecp256k1Privkeypem
                    );

                    ig.GetSignedInvoiceXML(out string invoiceHash, out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out string requestApi);

                    var managerInvoicejson = InvoiceHelper.GetManagerInvoiceJson(relayData);
                    ManagerInvoice managerInvoice = JsonConvert.DeserializeObject<ManagerInvoice>(managerInvoicejson);

                    var editData = InvoiceHelper.ModifyQrInEditData(relayData.Edit, _deviceSetup.QrCodeGuid, base64QrCode);

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
                        Base64Invoice = Convert.ToBase64String(Encoding.UTF8.GetBytes(managerInvoicejson)),
                        Referrer = relayData.Referrer,
                        ICV = ICV,
                        PIH = PIH,
                        InvoiceHash = invoiceHash,
                        Base64SignedInvoice = base64SignedInvoice,
                        Base64QrCode = base64QrCode,
                        XmlFileName = XmlFileName,
                        Timestamp = DateTime.Now,
                        EditData = editData,
                        EnvironmentType = _deviceSetup.EnvironmentType
                    };

                    return View("Index", approvedInvoice);
                }
            }
            catch
            {
                var errorModel = new ErrorViewModel
                {
                    ErrorMessage = @"Please review the Device Settings in the ZatcaEGS application to ensure it matches your business data setup.",
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
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_deviceSetup.CCSIDBinaryToken}:{_deviceSetup.CCSIDSecret}")));

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_deviceSetup.ComplianceCheckUrl, content);

                var resultContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                apiResponse.RequestType = "Invoice Compliant Check";
                apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

                model.RequestType = apiResponse.RequestType;
                model.StatusCode = apiResponse.StatusCode;

                model.ApprovalStatus = string.IsNullOrEmpty(apiResponse.ClearanceStatus) ? apiResponse.ReportingStatus : apiResponse.ClearanceStatus;

                model.EnvironmentType = _deviceSetup.EnvironmentType;

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
            try
            {
                ApprovedInvoice approvedInvoice = _dbContext.ApprovedInvoices
                                              .FirstOrDefault(invoice => invoice.ZatcaUUID == model.ZatcaUUID);


                if (approvedInvoice != null)
                {
                    var serverResult = JsonConvert.DeserializeObject<ServerResult>(approvedInvoice.ServerResult);
                    serverResult.RequestType += " --- FROM INVOICE LOG ---";
                    approvedInvoice.ServerResult = JsonConvert.SerializeObject(serverResult);

                    return View("Index", approvedInvoice);
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
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_deviceSetup.PCSIDBinaryToken}:{_deviceSetup.PCSIDSecret}")));

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_deviceSetup.ClearanceUrl, content);

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
                        model.EditData = InvoiceHelper.ModifyQrInEditData(model.EditData, _deviceSetup.QrCodeGuid, qrCodeNode.Value);
                    }
                }

                apiResponse.RequestType = "Invoice Clearance";
                apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

                model.RequestType = apiResponse.RequestType;
                model.StatusCode = apiResponse.StatusCode;

                model.ApprovalStatus = apiResponse.ClearanceStatus;

                model.EnvironmentType = _deviceSetup.EnvironmentType;

                model.ServerResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                //Also Save Rejected Invoice, Need New ICV and PIH to resend Corrected Documents
                if (model.ApprovalStatus.Contains("CLEARED"))
                {
                    model.Timestamp = DateTime.Now;

                    _dbContext.ApprovedInvoices.Add(model);
                    await _dbContext.SaveChangesAsync();
                }

                return View("Index", model);
            }

            catch (Exception ex)
            {
                model.ServerResult = ex.Message;
                return View("Index", model);
            }
        }

        [HttpPost("reporting")]
        public async Task<IActionResult> Reporting([FromForm] ApprovedInvoice model)
        {
            {
                try
                {
                    
                    ApprovedInvoice approvedInvoice = _dbContext.ApprovedInvoices
                               .FirstOrDefault(invoice => invoice.ZatcaUUID == model.ZatcaUUID);


                    if (approvedInvoice != null)
                    {
                        var serverResult = JsonConvert.DeserializeObject<ServerResult>(approvedInvoice.ServerResult);
                        serverResult.RequestType += " --- FROM INVOICE LOG ---";
                        approvedInvoice.ServerResult = JsonConvert.SerializeObject(serverResult);

                        return View("Index", approvedInvoice);
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
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_deviceSetup.PCSIDBinaryToken}:{_deviceSetup.PCSIDSecret}")));

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_deviceSetup.ReportingUrl, content);

                    var resultContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ServerResult>(resultContent);

                    apiResponse.RequestType = "Invoice Reporting";
                    apiResponse.StatusCode = $"{(int)response.StatusCode}-{response.StatusCode}";

                    model.RequestType = apiResponse.RequestType;
                    model.StatusCode = apiResponse.StatusCode;

                    model.ApprovalStatus = apiResponse.ReportingStatus;

                    model.EnvironmentType = _deviceSetup.EnvironmentType;
                    model.ServerResult = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    //Also Save Rejected Invoice, Need New ICV and PIH to resend Corrected Documents
                    if (model.ApprovalStatus.Contains("REPORTED"))
                    {
                        model.Timestamp = DateTime.Now;

                        _dbContext.ApprovedInvoices.Add(model);
                        await _dbContext.SaveChangesAsync();
                    }
                    return View("Index", model);
                }

                catch (Exception ex)
                {
                    model.ServerResult = ex.Message;
                    return View("Index", model);
                }
            }
        }

        //Update Manager Invoice
        [HttpPost("update-invoice")]
        public async Task<IActionResult> UpdateInvoice([FromForm] ApprovedInvoice model)
        {
            try
            {
                var apiUrl = InvoiceHelper.ConstructApiUrl(model.Referrer, model.ManagerUUID);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-KEY", _deviceSetup.AccessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var payload = model.EditData;
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync(apiUrl, content);
                    
                    ServerResult serverResult = new();

                    if (response.IsSuccessStatusCode)
                    {
                        serverResult.Message = "Manager invoice successfully updated. Please return to the manager to view the results.";
                    }
                    else
                    {
                        serverResult.Message = "Failed to update manager invoice. Please ensure that the Access Token recorded in the Gateway Settings is correct and still valid.";
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

    }
}



