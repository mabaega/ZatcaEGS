using Newtonsoft.Json;

namespace ZatcaEGS.Models
{
    //public class ZatcaRequestApi
    //{
    //    [JsonProperty("uuid")]
    //    public string uuid { get; set; }

    //    [JsonProperty("invoiceHash")]
    //    public string invoiceHash { get; set; }

    //    [JsonProperty("invoice")]
    //    public string invoice { get; set; }
    //}

    public class ServerResult
    {

        [JsonProperty("requestUri")]
        public string RequestUri { get; set; }

        [JsonProperty("requestType")]
        public string RequestType { get; set; }

        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("clearanceStatus")]
        public string ClearanceStatus { get; set; }

        [JsonProperty("reportingStatus")]
        public string ReportingStatus { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("uuid")]
        public string UUID { get; set; }

        [JsonProperty("reasonPhrase")]
        public string ReasonPhrase { get; set; }

        [JsonProperty("isSuccessStatusCode")]
        public string IsSuccessStatusCode { get; set; }

        [JsonProperty("validationResults")]
        public ValidationResult ValidationResults { get; set; }

        [JsonProperty("errorMessages")]
        public List<DetailInfo> ErrorMessages { get; set; }

        [JsonProperty("errors")]
        public List<DetailInfo> Errors { get; set; }

        [JsonProperty("warningMessages")]
        public List<DetailInfo> WarningMessages { get; set; }

        [JsonProperty("warnings")]
        public List<DetailInfo> Warnings { get; set; }

        [JsonProperty("clearedInvoice")]
        public string ClearedInvoice { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("invoiceHash")]
        public string InvoiceHash { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("qrBuyertStatus")]
        public string QrBuyerStatus { get; set; }

        [JsonProperty("qrSellertStatus")]
        public string QrSellerStatus { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
    }

    public class DetailInfo
    {

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

    }

    public class ValidationResult
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("infoMessages")]
        public List<DetailInfo> InfoMessages { get; set; }

        [JsonProperty("warningMessages")]
        public List<DetailInfo> WarningMessages { get; set; }

        [JsonProperty("errorMessages")]
        public List<DetailInfo> ErrorMessages { get; set; }

    }

    public class ApiBadResponse
    {
        // Properti umum untuk semua jenis respons
        public string RequestUri { get; set; }
        public string RequestType { get; set; }
        public string HttpCode { get; set; }
        public string HttpMessage { get; set; }

        // Properti khusus untuk setiap jenis status kode
        public string MoreInformation { get; set; } // 401, 404, dll.
        public string Location { get; set; } // 303
        public string MaxAllowedSize { get; set; } // 413
        public string RetryAfter { get; set; } // 429, 503
        public string Details { get; set; } // 500, 504
        public List<FieldError> Errors { get; set; } // 400 atau kesalahan validasi lainnya
    }

    // Kelas tambahan jika ada daftar kesalahan (contoh: 400 Bad Request)
    public class FieldError
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }

}