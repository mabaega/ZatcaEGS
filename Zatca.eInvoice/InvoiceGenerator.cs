using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;

namespace Zatca.eInvoice
{
    public class InvoiceGenerator
    {
        public Invoice InvoiceObject { get; set; }
        public string X509CertificateContent { get; set; }
        public string EcSecp256k1Privkeypem { get; set; }

        public InvoiceGenerator(Invoice invoiceObject, string x509CertificateContent, string ecSecp256k1Privkeypem)
        {
            this.InvoiceObject = invoiceObject;
            this.X509CertificateContent = x509CertificateContent;
            this.EcSecp256k1Privkeypem = ecSecp256k1Privkeypem;
        }
        public string GetCleanInvoiceXML(bool applayXsl = true)
        {
            try
            {
                XmlSerializerNamespaces namespaces = new();
                namespaces.Add("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
                namespaces.Add("sig", "urn:oasis:names:specification:ubl:schema:xsd:CommonSignatureComponents-2");
                namespaces.Add("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
                namespaces.Add("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
                namespaces.Add("sbc", "urn:oasis:names:specification:ubl:schema:xsd:SignatureBasicComponents-2");
                namespaces.Add("sac", "urn:oasis:names:specification:ubl:schema:xsd:SignatureAggregateComponents-2");
                namespaces.Add("ds", "http://www.w3.org/2000/09/xmldsig#");
                namespaces.Add("xades", "http://uri.etsi.org/01903/v1.3.2#");

                var invoiceData = InvoiceObject.ObjectToXml(namespaces);

                invoiceData = invoiceData.MoveLastAttributeToFirst();
                if (applayXsl) { invoiceData = invoiceData.ApplyXSLT(SharedUtilities.ReadResource("ZatcaDataInvoice.xsl"), true); }
                return invoiceData.ToFormattedXml();
            }
            catch
            {
                return null;
            }
        }
        public void GetSignedInvoiceXML(out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out string requestApi)
        {
            try
            {
                byte[] certificateBytes = Encoding.UTF8.GetBytes(X509CertificateContent);
                X509Certificate2 parsedCertificate = new(certificateBytes);

                string SignatureTimestamp = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss");
                string PublicKeyHashing = Convert.ToBase64String(Encoding.UTF8.GetBytes(SharedUtilities.HashSha256AsString(X509CertificateContent)));
                string IssuerName = parsedCertificate.IssuerName.Name;
                string SerialNumber = SharedUtilities.GetSerialNumberForCertificateObject(parsedCertificate);
                string SignedPropertiesHash = SharedUtilities.GetSignedPropertiesHash(SignatureTimestamp, PublicKeyHashing, IssuerName, SerialNumber);

                string CleanInvoice = GetCleanInvoiceXML(true);

                string InvoiceHash = SharedUtilities.GetBase64InvoiceHash(CleanInvoice);

                string SignatureValue = SharedUtilities.GetDigitalSignature(InvoiceHash, EcSecp256k1Privkeypem);

                SignedUBL signedUBL = new(InvoiceHash,
                    SignedPropertiesHash,
                    SignatureValue,
                    X509CertificateContent,
                    SignatureTimestamp,
                    PublicKeyHashing,
                    IssuerName,
                    SerialNumber);

                base64QrCode = QrCodeGenerator.GenerateQRCode(InvoiceObject, signedUBL);


                string stringXMLQrCode = SharedUtilities.ReadResource("ZatcaDataQr.xml").Replace("TLV_QRCODE_STRING", base64QrCode);
                string stringXMLSignature = SharedUtilities.ReadResource("ZatcaDataSignature.xml");
                string stringUBLExtension = signedUBL.ToString();

                int profileIDIndex = CleanInvoice.IndexOf("<cbc:ProfileID>");
                CleanInvoice = CleanInvoice.Insert(profileIDIndex - 6, stringUBLExtension);

                int AccountingSupplierPartyIndex = CleanInvoice.IndexOf("<cac:AccountingSupplierParty>");

                CleanInvoice = CleanInvoice.Insert(AccountingSupplierPartyIndex - 6, stringXMLQrCode);

                AccountingSupplierPartyIndex = CleanInvoice.IndexOf("<cac:AccountingSupplierParty>");

                CleanInvoice = CleanInvoice.Insert(AccountingSupplierPartyIndex - 6, stringXMLSignature);

                byte[] bytes = Encoding.UTF8.GetBytes(CleanInvoice);
                base64SignedInvoice = Convert.ToBase64String(bytes);

                requestApi = "{" +
                    "\"invoiceHash\": \"" + InvoiceHash + "\"," +
                    "\"uuid\": \"" + InvoiceObject.UUID + "\"," +
                    "\"invoice\": \"" + base64SignedInvoice + "\"" +
                    "}";

                string SellerIdentification = InvoiceObject.AccountingSupplierParty.Party.PartyTaxScheme.CompanyID.ToString();
                string IssueDate = InvoiceObject.IssueDate.Replace("-", "");
                string IssueTime = InvoiceObject.IssueTime.Replace(":", "");
                string InvoiceNumber = Regex.Replace(InvoiceObject.ID.Value.ToString(), @"[^a-zA-Z0-9]", "-");
                
                XmlFileName = $"{SellerIdentification}_{IssueDate}{IssueTime}_{InvoiceNumber}.xml";

            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Error parsing certificate: {ex.Message}");
                throw;
            }
        }

    }

}
