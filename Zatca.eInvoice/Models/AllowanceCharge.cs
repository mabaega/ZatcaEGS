using System.Xml;
using System.Xml.Serialization;

namespace Zatca.eInvoice.Models
{
    public class AllowanceCharge
    {
        [XmlElement(ElementName = "ChargeIndicator", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
        public Boolean ChargeIndicator { get; set; }

        [XmlElement(ElementName = "AllowanceChargeReasonCode", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
        public string AllowanceChargeReasonCode { get; set; }

        [XmlElement(ElementName = "AllowanceChargeReason", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
        public string AllowanceChargeReason { get; set; }

        [XmlElement(ElementName = "Amount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
        public Amount Amount { get; set; }

        [XmlElement(ElementName = "TaxCategory", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
        public TaxCategory[] TaxCategory { get; set; }
    }
}
