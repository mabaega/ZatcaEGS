using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Zatca.eInvoice.Helpers
{

    public enum InvoiceType
    {
        TaxInvoice = 388,
        TaxInvoiceDebitNote = 383,
        TaxInvoiceCreditNote = 381,
        PrepaymentTaxInvoice = 386
    }

    //public enum InvoiceSubType
    //{
    //    Normal = 1,
    //    Simplified = 2
    //}

    public class InvoiceTypeCode
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlText]
        public string Value { get; set; }

        public InvoiceTypeCode(InvoiceType type, string subType)
        {
            string typeCode = ((int)type).ToString();
            Name = (subType.Trim() + "0000000")[..7];
            Value = typeCode;
        }

        public InvoiceTypeCode() { }
    }

    public enum BooleanEnum
    {
        [EnumMember(Value = "true")]
        True,
        [EnumMember(Value = "false")]
        False
    }

    public enum PartyIdentificationEnum
    {
        CRN, MOM, MLS, Number700, SAG, OTH
    }

    public enum EnvironmentType
    {
        NonProduction,
        Simulation,
        Production
    }

}

