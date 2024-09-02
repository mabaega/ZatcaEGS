using System.Text;
using System.Xml.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;

namespace Zatca.EGS.Helpers
{
    public static class DocumentFormatter
    {
        public static string Beautify(this XDocument doc)
        {
            using MemoryStream memoryStream = new();
            using (XmlWriter writer = XmlWriter.Create(memoryStream, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            }))
            {
                doc.Save(writer);
            }

            memoryStream.Position = 0;
            using StreamReader reader = new(memoryStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public static string SerializeObject<T>(T value)
        {
            StringBuilder sb = new StringBuilder(256);
            StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);

            var jsonSerializer = JsonSerializer.CreateDefault();
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;
                jsonWriter.IndentChar = ' ';
                jsonWriter.Indentation = 4;

                jsonSerializer.Serialize(jsonWriter, value, typeof(T));
            }

            return sw.ToString();
        }
        public static string ExcludeClearanceInvoice(string jsonString)
        {
            var jsonObject = JObject.Parse(jsonString);
            jsonObject.Property("clearedInvoice")?.Remove();
            return jsonObject.ToString();
        }
    }

}
