namespace ZatcaEGS.Helpers
{
    public static class UrlHelper
    {
        public static string ConstructInvoiceApiUrl(string referrer, string invoiceUUID)
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

        public static string GetRelayUrl(string referrer)
        {
            var uri = new Uri(referrer);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";

            if (uri.Port != 80 && uri.Port != 443)
            {
                baseUrl += $":{uri.Port}";
            }

            return $"{baseUrl}/relay";
        }
    }
}
