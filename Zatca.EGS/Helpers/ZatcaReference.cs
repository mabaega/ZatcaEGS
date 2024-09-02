using System.Text;
using Zatca.EGS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace Zatca.EGS.Helpers
{
    public class ZatcaReference
    {
        public static async Task<(string ICV, string PIH)> GetReferenceFolder(string apiPath, string apiSecret)
        {
            string fullPath = $"{apiPath}/folder-form/{ManagerCustomField.FolderReferenceGuid}";
            
            //Console.WriteLine($"Request URL: {fullPath}");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("X-API-KEY", apiSecret);

                try
                {
                    HttpResponseMessage response = await client.GetAsync(fullPath);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.Content.Headers.ContentType?.MediaType == "application/json")
                    {
                        try
                        {
                            var jsonObject = JsonConvert.DeserializeObject<JObject>(responseBody);

                            // Verify response structure
                            var customFields2 = jsonObject?["CustomFields2"] as JObject;
                            var strings = customFields2?["Strings"] as JObject;

                            string icv = strings?[ManagerCustomField.LastIcvGuid]?.ToString();
                            string pih = strings?[ManagerCustomField.LastPihGuid]?.ToString();

                            return (icv, pih);
                        }
                        catch //(JsonException jsonEx)
                        {
                            //Console.WriteLine($"JSON Parsing error: {jsonEx.Message}");
                            return (null, null);
                        }
                    }
                    else
                    {
                        //Console.WriteLine("Response is not JSON.");
                        return (null, null);
                    }
                }
                catch //(HttpRequestException httpEx)
                {
                    //Console.WriteLine($"Request error: {httpEx.Message}");
                    return (null, null);
                }
            }
        }

        public static async Task<bool> UpdateReferenceFolder(string apiPath, string apiSecret, string icvValue, string pihValue)
        {
            string fullPath = $"{apiPath}/folder-form/{ManagerCustomField.FolderReferenceGuid}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("X-API-KEY", apiSecret);

                try
                {
                    var strings = new Dictionary<string, string>
                    {
                        { ManagerCustomField.LastIcvGuid, icvValue },
                        { ManagerCustomField.LastPihGuid, pihValue }
                    };

                    var payload = new
                    {
                        Description = "ZatcaReference",
                        CustomFields = new { },
                        CustomFields2 = new
                        {
                            Strings = strings,
                            Decimals = new { },
                            Dates = new { },
                            Booleans = new { },
                            StringArrays = new { }
                        },
                        Key = ManagerCustomField.FolderReferenceGuid
                    };

                    string jsonPayload = JsonConvert.SerializeObject(payload);

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PutAsync(fullPath, content);

                    response.EnsureSuccessStatusCode();

                    return response.IsSuccessStatusCode;
                }
                catch //(HttpRequestException e)
                {
                    //Console.WriteLine($"Request error: {e.Message}");
                    return false;
                }
            }
        }


        public static async Task<string> UpdateInvoice(CertificateInfo certInfo, ApprovedInvoice model)
        {
            try
            {

                var apiUrl = UrlHelper.ConstructInvoiceApiUrl(model.Referrer, model.ManagerUUID);
                string UpdateMessage = string.Empty;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("X-API-KEY", certInfo.ApiSecret);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var inv = (JsonParser.FindValueByKey(JObject.Parse(model.EditData), model.ManagerUUID)?.ToString()) ?? throw new InvalidOperationException("Unable to find the required value in EditData.");
                    var content = new StringContent(inv, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        UpdateMessage = "Manager invoice successfully updated.";
                    }
                    else
                    {
                        UpdateMessage = "Failed to update manager invoice. Please ensure that the Access Token is correct and still valid.";
                    }
                }

                string InvoiceInfo = GenerateInvoiceInfo(model);

                InvoiceInfo += $"Manager Update Status: \n{UpdateMessage}\n\n";

                return InvoiceInfo;
            }
            catch //(Exception ex)
            {
                //Console.WriteLine($"Request error: {ex.Message}");
                return null;
            }
        }


        private static string GenerateInvoiceInfo(ApprovedInvoice model)
        {
            string InvoiceInfo = $"ManagerUUID: \n{model.ManagerUUID}\n\n";
            InvoiceInfo += $"ApprovalStatus: \n{model.ApprovalStatus}\n\n";
            InvoiceInfo += $"EnvironmentType: \n{model.EnvironmentType}\n\n\n";
            InvoiceInfo += $"UpdatedRelayData: \n{model.EditData}\n\n\n";
            InvoiceInfo += $"ICV: \n{model.ICV}\n\n";
            InvoiceInfo += $"ZatcaUUID: \n{model.ZatcaUUID}\n\n";
            InvoiceInfo += $"InvoiceHash: \n{model.InvoiceHash}\n\n";
            InvoiceInfo += $"Base64SignedInvoice: \n{model.Base64SignedInvoice}\n\n\n";
            InvoiceInfo += $"Base64QrCode: \n{model.Base64QrCode}\n\n";
            var DecodedQrCode = model.DecodedQrCode;
            InvoiceInfo += $"Decoded QrCode: \n{DecodedQrCode}\n\n";
            var formattedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(model.ServerResult), Formatting.Indented);
            InvoiceInfo += $"ServerResult: \n{formattedJson}\n\n";
            InvoiceInfo += $"TimeStamp: \n{model.Timestamp}\n\n";
            return InvoiceInfo;
        }
    }
}
