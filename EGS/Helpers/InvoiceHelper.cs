using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace EGS.Helpers
{
    public class InvoiceHelper
    {
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


