
namespace ZatcaEGS.Models
{
    public class RelayData
    {
        public string Referrer { get; set; }
        public string Key { get; set; }
        public string View { get; set; }
        public string Edit { get; set; }
        public Dictionary<string, string> HiddenFields { get; set; }
    }

    public class Currency
    {
        public string Code { get; set; } = "SAR";
        public string Name { get; set; }
        public string Symbol { get; set; }
    }

    public class CustomFields2
    {
        public Dictionary<string, string> Strings { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, decimal> Decimals { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, DateTime?> Dates { get; set; } = new Dictionary<string, DateTime?>();
        public Dictionary<string, bool> Booleans { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, List<string>> StringArrays { get; set; } = new Dictionary<string, List<string>>();
    }
    public class InvoiceParty
    {
        public string Name { get; set; }
        public Currency Currency { get; set; }
        public CustomFields2 CustomFields2 { get; set; }
    }

    public class LineItem
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UnitName { get; set; }
        public bool HasDefaultLineDescription { get; set; } = false;
        public string DefaultLineDescription { get; set; }
        public CustomFields2 CustomFields2 { get; set; }
    }

    public class TaxCode
    {
        public string Name { get; set; }
        public int TaxRate { get; set; }
        public double Rate { get; set; } = 0;
    }

    public class Line
    {
        public LineItem Item { get; set; }
        public string LineDescription { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }
        public CustomFields2 CustomFields2 { get; set; }
        public double Qty { get; set; } = 0;
        public double UnitPrice { get; set; } = 0;
        public double DiscountAmount { get; set; } = 0;
        public TaxCode TaxCode { get; set; }
    }

    public class RefInvoice
    {
        public string Reference { get; set; }
    }

    public class ManagerInvoice
    {
        public DateTime IssueDate { get; set; }
        public DateTime DueDateDate { get; set; }

        public string Reference { get; set; }
        public RefInvoice RefInvoice { get; set; }
        public InvoiceParty InvoiceParty { get; set; }
        public string BillingAddress { get; set; }
        public double ExchangeRate { get; set; } = 0;
        public string Description { get; set; }
        public List<Line> Lines { get; set; }
        public bool HasLineNumber { get; set; } = false;
        public bool HasLineDescription { get; set; } = false;
        public bool Discount { get; set; } = false;
        public bool AmountsIncludeTax { get; set; } = false;
        public CustomFields2 CustomFields2 { get; set; }
    }
}