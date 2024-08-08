using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ZatcaEGS.Models;

namespace ZatcaEGS.Helpers
{
    public class InvoiceHelper
    {
        public static string GetManagerInvoiceJson(RelayData relayData)
        {
            var edit = JsonConvert.DeserializeObject<JObject>(relayData.Edit);
            ReplaceGuidsWithHiddenFieldValues(edit, relayData.HiddenFields);
            var Ret = JsonConvert.SerializeObject(edit, Formatting.Indented);
            Ret = Ret.Replace("Customer", "InvoiceParty")
                     .Replace("Supplier", "InvoiceParty")
                     .Replace("SalesInvoice", "RefInvoice")
                     .Replace("PurchaseInvoice", "RefInvoice")
                     .Replace("SalesUnitPrice", "UnitPrice")
                     .Replace("PurchaseUnitPrice", "UnitPrice");

            return Ret;
        }

        public static void ReplaceGuidsWithHiddenFieldValues(JObject edit, System.Collections.Generic.Dictionary<string, string> hiddenFields)
        {
            var propertiesToReplace = hiddenFields.Keys.ToList();
            foreach (var property in edit.Descendants().OfType<JProperty>())
            {
                if (propertiesToReplace.Contains(property.Value.ToString()))
                {
                    var guidValue = property.Value.ToString();
                    if (hiddenFields.TryGetValue(guidValue, out string value))
                    {
                        property.Value = JsonConvert.DeserializeObject<JObject>(value);
                    }
                }
            }
        }

        public static void UpdateOrAddValueInCustomFields2Strings(JObject jObject, string fieldGuid, string newValue)
        {
           
            var strings = (JObject)jObject["CustomFields2"]["Strings"];

            if (strings.ContainsKey(fieldGuid))
            {
                strings[fieldGuid] = newValue;
            }
            else
            {
                strings.Add(fieldGuid, newValue);
            }
        }
        public static (int ICV, string PIH) GetLastICVandPIH(AppDbContext _dbContext)
        {
            var lastInvoice = _dbContext.ApprovedInvoices
                                               .OrderByDescending(invoice => invoice.Timestamp)
                                               .FirstOrDefault();

            if (lastInvoice == null)
            {
                return (1, "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==");
            }

            return (lastInvoice.ICV + 1, lastInvoice.PIH);
        }

        public static string GetZatcaUUID(AppDbContext _dbContext, string managerUUID)
        {
            int invoiceCount = _dbContext.ApprovedInvoices.Where(invoice => invoice.ManagerUUID == managerUUID).Count();

            if (invoiceCount == 0)
            {
                return managerUUID;
            }
            else 
            {
                return managerUUID.Substring(0, managerUUID.Length - 12) + invoiceCount.ToString("000000000000");
            }
        }

        public static string ModifyQrInEditData(string editData, string qrFieldGuid, string newValue)
        {
            JObject jsonObject = JObject.Parse(editData);
            if (jsonObject["CustomFields2"]["Strings"][qrFieldGuid] != null)
            {
                jsonObject["CustomFields2"]["Strings"][qrFieldGuid] = newValue;
            }
            else
            {
                jsonObject["CustomFields2"]["Strings"][qrFieldGuid] = newValue;
            }
            string updatedJsonString = jsonObject.ToString(Formatting.Indented);

            return updatedJsonString;
        }

        public static string ConstructApiUrl(string referrer, string invoiceUUID)
        {
            var uri = new Uri(referrer);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";

            if (uri.Port != 80 && uri.Port != 443) 
            {
                baseUrl += $":{uri.Port}";
            }

            if (referrer.Contains("purchase-invoice-view"))
            {
                return $"{baseUrl}/api2/purchase-invoice-form/{invoiceUUID}";
            }
            else if (referrer.Contains("sales-invoice-view"))
            {
                return $"{baseUrl}/api2/sales-invoice-form/{invoiceUUID}";
            }
            else if (referrer.Contains("debit-note-view"))
            {
                return $"{baseUrl}/api2/debit-note-form/{invoiceUUID}";
            }
            else if (referrer.Contains("credit-note-view"))
            {
                return $"{baseUrl}/api2/credit-note-form/{invoiceUUID}";
            }

            throw new ArgumentException("Invalid referrer URL");
        }


        public static bool ValidateCompanyID(
                            Dictionary<string, string> hiddenFields,
                            string businessDatabaseGuidKey,
                            string companyID,
                            out string message)
        {
            message = null;

            if (!hiddenFields.ContainsKey(businessDatabaseGuidKey))
            {
                message = "Missing key '_gatewaysetting.BusinessDatabaseGuid' in HiddenFields.";
                return false;
            }

            string jsonString = hiddenFields.GetValueOrDefault(businessDatabaseGuidKey);
            if (jsonString == null)
            {
                message = "BusinessDatabaseGuid key is missing in HiddenFields.";
                return false;
            }

            try
            {
                JObject jsonObject = JObject.Parse(jsonString);
                string customFieldValue = jsonObject["CustomFields"]?["d96d97e8-c857-42c6-8360-443c06a13de9"]?.ToString();

                if (string.IsNullOrEmpty(customFieldValue))
                {
                    message = "Expected custom field not found in Business Detail JSON.";
                    return false;
                }

                if (!customFieldValue.Equals(companyID, StringComparison.OrdinalIgnoreCase))
                {
                    message = "The data received does not match the Company ID.";
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                message = "Error parsing BusinessDatabaseGuid JSON.";
                return false;
            }
        }

    }

    
}


