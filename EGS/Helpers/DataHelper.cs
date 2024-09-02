using EGS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EGS.Helpers
{
    public class DataHelper
    {
        public static ManagerInvoice GetManagerInvoice(string data, string key)
        {
            JObject parsedData = JObject.Parse(data);
            JObject mergedObject = GenerateMergedObject(parsedData, parsedData);
            JToken result = FindValueByKey(mergedObject, key);

            string resultString = result?.ToString();

            if (!string.IsNullOrEmpty(resultString))
            {
                resultString = resultString.Replace("Customer", "InvoiceParty")
                                             .Replace("Supplier", "InvoiceParty")
                                             .Replace("SalesInvoice", "RefInvoice")
                                             .Replace("PurchaseInvoice", "RefInvoice")
                                             .Replace("SalesUnitPrice", "UnitPrice")
                                             .Replace("PurchaseUnitPrice", "UnitPrice");

                return JsonConvert.DeserializeObject<ManagerInvoice>(resultString);
            }
            else
            {
                return null;
            }
        }

        public static CertificateInfo GetCerificateInfo(string data, string key)
        {
            JObject parsedData = JObject.Parse(data);
            JToken result = FindValueByKey(parsedData, key);
            string resultString = result?.ToString();

            if (!string.IsNullOrEmpty(resultString))
            {
                return ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(resultString);
            }
            else
            {
                return null;
            }
        }

        public static ApprovedInvoice GetApprovedInvoice(string data, string key)
        {
            JObject parsedData = JObject.Parse(data);
            JToken result = FindValueByKey(parsedData, key);
            string resultString = result?.ToString();

            if (!string.IsNullOrEmpty(resultString))
            {
                var approved = ObjectCompressor.DeserializeFromBase64String<ApprovedInvoice>(resultString);
                return approved;
            }
            else
            {
                return null;
            }
        }
        private static JObject GenerateMergedObject(JObject data, JObject root)
        {
            if (data == null) return new JObject();

            JObject result = new JObject();
            foreach (var prop in data.Properties())
            {
                result[prop.Name] = ProcessValue(prop.Value, root);
            }

            return result;
        }

        private static JToken ProcessValue(JToken value, JObject root)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    return GenerateMergedObject(value as JObject, root);
                case JTokenType.Array:
                    return ProcessArray(value as JArray, root);
                case JTokenType.String when IsGuid(value.ToString()):
                    return FindGuidReplacement(root, value.ToString()) ?? value;
                default:
                    return value;
            }
        }

        private static JArray ProcessArray(JArray array, JObject root)
        {
            JArray newArray = new JArray();
            foreach (var item in array)
            {
                newArray.Add(ProcessValue(item, root));
            }

            return newArray;
        }

        private static bool IsGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        private static JToken FindGuidReplacement(JObject root, string guid)
        {
            foreach (var prop in root.Properties())
            {
                if (prop.Name == guid)
                    return prop.Value;

                if (prop.Value is JObject childObject)
                {
                    var result = FindGuidReplacement(childObject, guid);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        public static JToken FindValueByKey(JToken token, string key)
        {
            if (token == null) return null;

            if (token.Type == JTokenType.Object)
            {
                var obj = token as JObject;
                if (obj.TryGetValue(key, out var value))
                    return value;

                foreach (var property in obj.Properties())
                {
                    var result = FindValueByKey(property.Value, key);
                    if (result != null)
                        return result;
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                {
                    var result = FindValueByKey(item, key);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        public static string FindStringValueByKey(JToken token, string key)
        {
            if (token == null) return null;

            if (token.Type == JTokenType.Object)
            {
                var obj = token as JObject;
                if (obj.TryGetValue(key, out var value))
                    return value.ToString();

                foreach (var property in obj.Properties())
                {
                    var result = FindValueByKey(property.Value, key);
                    if (result != null)
                        return result.ToString();
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                {
                    var result = FindValueByKey(item, key);
                    if (result != null)
                        return result.ToString();
                }
            }

            return null;
        }

        //public static string ReplaceStringValueByKey(string json, string key, JToken newValue)
        //{
        //    if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
        //        return json;

        //    JToken token;

        //    try
        //    {
        //        token = JToken.Parse(json);
        //    }
        //    catch (JsonReaderException)
        //    {
        //        return json;
        //    }

        //    var updatedToken = ReplaceValueByKey(token, key, newValue);
        //    return updatedToken;
        //}
        private static string ReplaceValueByKey(JToken token, string key, JToken newValue)
        {
            if (token == null || string.IsNullOrEmpty(key))
                return null;

            if (token.Type == JTokenType.Object)
            {
                var obj = token as JObject;
                if (obj.TryGetValue(key, out var existingValue))
                    obj[key] = newValue;

                foreach (var property in obj.Properties())
                    ReplaceValueByKey(property.Value, key, newValue);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                    ReplaceValueByKey(item, key, newValue);
            }

            return token.ToString();
        }

        public static string ModifyStringInEditData(string editData, string key, string fieldGuid, string newValue)
        {
            JObject jsonObject = JObject.Parse(editData);
            var targetSection = FindSectionContainingKey(jsonObject, key);

            if (targetSection == null)
            {
                throw new InvalidOperationException("The section with the specified key was not found.");
            }

            var specificSection = targetSection[key] as JObject;
            if (specificSection == null)
            {
                throw new InvalidOperationException($"The key '{key}' is missing or is not an object.");
            }

            var customFields2 = specificSection["CustomFields2"] as JObject;
            if (customFields2?["Strings"]?[fieldGuid] != null)
            {
                customFields2["Strings"][fieldGuid] = newValue;
            }
            else
            {
                if (customFields2 == null)
                {
                    customFields2 = new JObject();
                    specificSection["CustomFields2"] = customFields2;
                }

                var strings = customFields2["Strings"] as JObject;
                if (strings == null)
                {
                    strings = new JObject();
                    customFields2["Strings"] = strings;
                }

                strings[fieldGuid] = newValue;
            }

            return jsonObject.ToString(Formatting.Indented);
        }

        private static JObject FindSectionContainingKey(JObject jsonObject, string key)
        {
            var queue = new Queue<JObject>();
            queue.Enqueue(jsonObject);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Property(key) != null)
                {
                    return current;
                }
                foreach (var property in current.Properties())
                {
                    if (property.Value.Type == JTokenType.Object)
                    {
                        queue.Enqueue(property.Value as JObject);
                    }
                }
            }
            return null;
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
        public static string CheckApiUrl(string referrer)
        {
            var uri = new Uri(referrer);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";

            if (uri.Port != 80 && uri.Port != 443)
            {
                baseUrl += $":{uri.Port}";
            }

            return $"{baseUrl}api2/access-tokens?fields=Name";
        }

    }
}
