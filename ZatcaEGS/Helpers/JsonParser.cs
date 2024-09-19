using Newtonsoft.Json.Linq;

namespace ZatcaEGS.Helpers
{
    public class JsonParser
    {
        public JsonParser() { }

        public static (string BusinessDetails, Dictionary<string, string> DynamicParts) ParseJson(string jsonString)
        {
            JObject jsonObject = JObject.Parse(jsonString);

            // Parse BaseCurrency
            // string baseCurrency = jsonObject["BaseCurrency"]?.ToString() ?? "{}";

            // Parse BusinessDetails
            string businessDetails = jsonObject["BusinessDetails"]?.ToString() ?? "{}";

            // Parse other dynamic parts
            var dynamicParts = new Dictionary<string, string>();

            foreach (var property in jsonObject.Properties())
            {
                if (property.Name != "BaseCurrency" && property.Name != "BusinessDetails")
                {
                    ParseDynamicPart(property.Value, dynamicParts);
                }
            }

            return (businessDetails, dynamicParts);
        }

        private static void ParseDynamicPart(JToken token, Dictionary<string, string> result)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (var property in token.Children<JProperty>())
                {
                    if (Guid.TryParse(property.Name, out _))
                    {
                        // Store the JSON string associated with the GUID directly
                        result[property.Name] = property.Value.ToString();
                    }
                }
            }
        }



        public static string ReplaceGuidValuesInJson(string jsonString, Dictionary<string, string> dynamicParts)
        {
            JObject jsonObject = JObject.Parse(jsonString);

            ReplaceGuidValuesRecursive(jsonObject, dynamicParts);

            return jsonObject.ToString();
        }

        private static void ReplaceGuidValuesRecursive(JToken token, Dictionary<string, string> dynamicParts)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (var property in token.Children<JProperty>())
                {
                    if (property.Value.Type == JTokenType.String && Guid.TryParse(property.Value.ToString(), out Guid guid))
                    {
                        if (dynamicParts.TryGetValue(guid.ToString(), out string replacement))
                        {
                            property.Value = JToken.Parse(replacement);
                        }
                    }
                    else
                    {
                        ReplaceGuidValuesRecursive(property.Value, dynamicParts);
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                {
                    ReplaceGuidValuesRecursive(item, dynamicParts);
                }
            }
        }


        public static string UpdateJsonGuidValue(string jsonString, string key)
        {
            JObject jsonObject = JObject.Parse(jsonString);
            UpdateJsonValueRecursive(jsonObject, key);
            return jsonObject.ToString();
        }

        private static void UpdateJsonValueRecursive(JObject jsonObject, string key)
        {
            if (jsonObject != null && jsonObject.Type == JTokenType.Object)
            {
                foreach (JProperty property in jsonObject.Properties())
                {
                    if (property.Value.Type == JTokenType.Object)
                    {
                        UpdateJsonValueRecursive((JObject)property.Value, key);
                    }
                    else if (property.Value.Type == JTokenType.String)
                    {
                        if (property.Name == key)
                        {
                            if (Guid.TryParse(property.Value.ToString(), out Guid guid))
                            {
                                property.Value = JValue.CreateString("#" + property.Value.ToString() + "#");
                            }
                        }
                    }
                }
            }
        }


        public static string FindStringByGuid(string jsonString, string key, string skipPropertyName = null)
        {
            JObject jsonObject = JObject.Parse(jsonString);
            JToken result = FindStringByGuidRecursive(jsonObject, key, skipPropertyName);
            return result?.ToString().Trim(); // Return the result as a string, or null if not found.
        }

        private static JToken FindStringByGuidRecursive(JObject jsonObject, string key, string skipPropertyName)
        {
            if (jsonObject == null || jsonObject.Type != JTokenType.Object)
            {
                return null;
            }

            foreach (JProperty property in jsonObject.Properties())
            {
                // Skip the property if it matches the skipPropertyName and its value is an object or array
                if (property.Name == skipPropertyName &&
                    (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array))
                {
                    continue;
                }

                if (property.Name == key)
                {
                    return property.Value; // Return the first found value.
                }
                else if (property.Value.Type == JTokenType.Object)
                {
                    JToken found = FindStringByGuidRecursive((JObject)property.Value, key, skipPropertyName);
                    if (found != null)
                    {
                        return found;
                    }
                }
                else if (property.Value.Type == JTokenType.Array)
                {
                    foreach (JToken item in property.Value)
                    {
                        if (item.Type == JTokenType.Object)
                        {
                            JToken found = FindStringByGuidRecursive((JObject)item, key, skipPropertyName);
                            if (found != null)
                            {
                                return found;
                            }
                        }
                    }
                }
            }

            return null; // Return null if the key is not found.
        }


        public static string FindValueByKey(JToken token, string key)
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


        // Modify Data from Relay
        public static string ModifyStringInEditData(string editData, string key, string fieldGuid, string newValue)
        {
            JObject jsonObject = JObject.Parse(editData);
            JObject targetSection;

            // Determine the target section based on the key
            if (string.IsNullOrEmpty(key))
            {
                targetSection = jsonObject; // If key is null or empty, work directly with the root object
            }
            else
            {
                targetSection = FindSectionContainingKey(jsonObject, key);
            }

            if (targetSection == null)
            {
                throw new InvalidOperationException("The section with the specified key was not found.");
            }

            // If the key is not null or empty, verify that it's a JObject
            JObject specificSection;

            if (!string.IsNullOrEmpty(key))
            {
                if (targetSection[key] is not JObject section)
                {
                    throw new InvalidOperationException($"The key '{key}' is missing or is not an object.");
                }
                specificSection = section;
            }
            else
            {
                // If key is null or empty, use the targetSection directly
                specificSection = targetSection;
            }

            // Ensure CustomFields2 and Strings are properly initialized
            var customFields2 = specificSection["CustomFields2"] as JObject;
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

            // Set or update the field value
            strings[fieldGuid] = newValue;

            return jsonObject.ToString();
        }

        private static JObject FindSectionContainingKey(JObject jsonObject, string key)
        {
            var queue = new Queue<JObject>();
            queue.Enqueue(jsonObject);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (string.IsNullOrEmpty(key) || current.Property(key) != null)
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


        public static string RemoveJsonField(string jsonString, string fieldToRemove)
        {
            // Parse the JSON string into a JObject
            var jObject = JObject.Parse(jsonString);

            // Recursively remove the field
            RemoveField(jObject, fieldToRemove);

            // Convert the modified JObject back to a JSON string
            return jObject.ToString();
        }

        private static void RemoveField(JObject jObject, string fieldToRemove)
        {
            // Check if the JObject contains the field to remove
            if (jObject.ContainsKey(fieldToRemove))
            {
                jObject.Remove(fieldToRemove);
            }

            // Recursively check all nested objects
            foreach (var property in jObject.Properties().ToList())
            {
                if (property.Value.Type == JTokenType.Object)
                {
                    RemoveField((JObject)property.Value, fieldToRemove);
                }
                else if (property.Value.Type == JTokenType.Array)
                {
                    foreach (var item in property.Value.Children<JObject>())
                    {
                        RemoveField(item, fieldToRemove);
                    }
                }
            }
        }
    }
}
