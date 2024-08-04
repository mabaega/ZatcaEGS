using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("======================== CUSTOM FIELD & FOOTER GENERATOR ========================");
        Console.WriteLine();
        Console.WriteLine("  This application is used to automatically add fields to your Business Data.");
        Console.WriteLine("  Please ensure you have backed up your business database.");
        Console.WriteLine("---------------------------------------------------------------------------------");
        Console.WriteLine("  Generate a new Access Token for your business data.");
        Console.WriteLine("  Note the EndPoint and Secret obtained.");
        Console.WriteLine();
        Console.WriteLine("=================================================================================");

        string baseUrl = GetBaseUrlFromUserInput(); 
        string apiKey = GetApiKeyFromUserInput();

        string jsonFilePath = "CfAndFooterGenerator.jsondata.json";
        string jsonData;
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(jsonFilePath))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                jsonData = reader.ReadToEnd();
            }
        }

        try
        {
            JObject jsonObject = JObject.Parse(jsonData);
            JArray jsonDataArray = (JArray)jsonObject["jsondata"];

            foreach (JObject item in jsonDataArray)
            {
                string apiPath = item.Value<string>("apipath");

                if (item.ContainsKey("data"))
                {
                    JObject dataObject = (JObject)item["data"];
                    string jsonPayload = dataObject.ToString(Formatting.None);

                    // Kirim data ke API
                    await SendDataToApi(baseUrl, apiPath, jsonPayload, apiKey);
                }
                else if (item.ContainsKey("customFields"))
                {
                    JArray customFieldsArray = (JArray)item["customFields"];
                    foreach (JObject customField in customFieldsArray)
                    {
                        string jsonPayload = customField.ToString(Formatting.None);
                        // Kirim data ke API
                        await SendDataToApi(baseUrl, apiPath, jsonPayload, apiKey);
                    }
                }
            }

            Console.WriteLine("Data telah berhasil dikirim.");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.ReadLine();
        }
    }

    static async Task SendDataToApi(string baseUrl, string apiPath, string jsonPayload, string apiKey)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var fullPath = new Uri(baseUrl + apiPath);
                httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(fullPath, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Data berhasil dikirim untuk API path: {apiPath}");
                    Console.WriteLine(jsonPayload);
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"Gagal mengirim data untuk API path: {apiPath}. Status code: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string GetBaseUrlFromUserInput()
    {
        // Fungsi untuk mendapatkan base URL dari pengguna
        Console.Write("  Api2 EndPoint : ");
        return Console.ReadLine();
    }

    static string GetApiKeyFromUserInput()
    {
        // Fungsi untuk mendapatkan X-API-KEY dari pengguna
        Console.Write("  Api2 Secret : ");
        return Console.ReadLine();
    }
}
