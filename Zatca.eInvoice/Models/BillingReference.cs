using System.Xml.Serialization;

namespace Zatca.eInvoice.Models
{
    public class BillingReference
    {
        [XmlElement(ElementName = "InvoiceDocumentReference", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
        public InvoiceDocumentReference InvoiceDocumentReference { get; set; }

    }

    public class InvoiceDocumentReference
    {
        [XmlElement(ElementName = "ID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
        public ID ID { get; set; }
    }
}