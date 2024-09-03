using System.Text;
using Zatca.EGS.Models;
using Zatca.eInvoice;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;

namespace Zatca.EGS.Helpers
{
    public class ComplianceTest
    {
        private readonly CertificateInfo _certInfo;
        private readonly string _CSIDBinaryToken;
        private readonly string _EcSecp256k1Privkeypem;

        public ComplianceTest(CertificateInfo certInfo, string CSIDBinaryToken, string EcSecp256k1Privkeypem)
        {
            _certInfo = certInfo ?? new CertificateInfo();
            _CSIDBinaryToken = CSIDBinaryToken;
            _EcSecp256k1Privkeypem = EcSecp256k1Privkeypem;
        }

        internal AccountingSupplierParty GetAccountingSupplierParty()
        {
            return new AccountingSupplierParty
            {
                Party = new Party
                {
                    PartyIdentification = new PartyIdentification
                    {
                        ID = new ID
                        {
                            SchemeID = _certInfo.IdentificationScheme,
                            Value = _certInfo.IdentificationID
                        }
                    },
                    PostalAddress = new PostalAddress
                    {
                        StreetName = _certInfo.StreetName,
                        BuildingNumber = _certInfo.BuildingNumber,
                        CitySubdivisionName = _certInfo.CitySubdivisionName,
                        CityName = _certInfo.CityName,
                        PostalZone = _certInfo.PostalZone,
                        Country = new Country
                        {
                            IdentificationCode = _certInfo.CountryIdentificationCode
                        }
                    },
                    PartyTaxScheme = new PartyTaxScheme
                    {
                        CompanyID = _certInfo.CompanyID,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID(_certInfo.TaxSchemeID)
                        }
                    },
                    PartyLegalEntity = new PartyLegalEntity
                    {
                        RegistrationName = _certInfo.RegistrationName
                    }
                }
            };
        }
        internal AccountingCustomerParty GetAccountingCustomerParty()
        {
            return new AccountingCustomerParty
            {
                Party = new Party
                {
                    PostalAddress = new PostalAddress
                    {
                        StreetName = "Salah Al-Din",
                        BuildingNumber = "1111",
                        CitySubdivisionName = "Al-Murooj",
                        CityName = "Riyadh",
                        PostalZone = "12222",
                        Country = new Country
                        {
                            IdentificationCode = "SA"
                        }
                    },
                    PartyTaxScheme = new PartyTaxScheme
                    {
                        CompanyID = "399999999800003",
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("VAT")
                        }
                    },
                    PartyLegalEntity = new PartyLegalEntity
                    {
                        RegistrationName = "Fatoora Samples LTD"
                    }
                }
            };
        }

        internal BillingReference GetBillingReference(InvoiceType invType, string billRefNum)
        {
            if (invType == InvoiceType.TaxInvoiceCreditNote || invType == InvoiceType.TaxInvoiceDebitNote)
            {
                return new BillingReference
                {
                    InvoiceDocumentReference = new InvoiceDocumentReference
                    {
                        ID = new ID(billRefNum),
                    }
                };
            }
            else
            {
                return null;
            }
        }

        public string GetRequestApi(string InvRefNumber, string billRefNum, InvoiceType invType, string InvSubtype, int ICV, string PIH)
        {
            try
            {
                var invoiceObject = new Invoice
                {
                    ProfileID = "reporting:1.0",

                    ID = new ID(InvRefNumber),

                    UUID = "8e6000cf-1a98-4174-b3e7-" + InvSubtype + ICV.ToString("00000"),

                    IssueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    IssueTime = DateTime.UtcNow.ToString("HH:mm:ss"),

                    InvoiceTypeCode = new InvoiceTypeCode(invType, InvSubtype),

                    DocumentCurrencyCode = "SAR",
                    TaxCurrencyCode = "SAR",

                    BillingReference = GetBillingReference(invType, billRefNum),

                    Note = new Note() { LanguageID = "ar", Value = "Debit Note" },

                    AdditionalDocumentReference =
                    [
                        new()
                        {
                            ID = new ID("ICV"),
                            UUID = ICV.ToString()
                        },
                        new()
                        {
                            ID = new ID("PIH"),
                            Attachment = new Attachment
                            {
                                EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObject
                                {
                                    Value = PIH,
                                    MimeCode = "text/plain"
                                }
                            }
                        }
                    ],

                    AccountingSupplierParty = GetAccountingSupplierParty(),

                    AccountingCustomerParty = GetAccountingCustomerParty(),

                    Delivery = new Delivery()
                    {
                        ActualDeliveryDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        LatestDeliveryDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    },

                    PaymentMeans = new PaymentMeans
                    {
                        PaymentMeansCode = "10",
                        InstructionNote = invType == InvoiceType.TaxInvoiceCreditNote || invType == InvoiceType.TaxInvoiceDebitNote ? "Instruction Note for Debit or Credit" : null
                    },


                    AllowanceCharge = new AllowanceCharge
                    {
                        ChargeIndicator = false,
                        AllowanceChargeReason = "discount",
                        Amount = new Amount("SAR", 0.00),
                        TaxCategory =
                        [
                            new() {
                                ID = new ID("UN/ECE 5305", "6", "S"),
                                Percent = 15,
                                TaxScheme = new TaxScheme
                                {
                                    ID = new ID("UN/ECE 5153", "6", "VAT")
                                }
                            },
                            new() {
                                ID = new ID("UN/ECE 5305", "6", "S"),
                                Percent = 15,
                                TaxScheme = new TaxScheme
                                {
                                    ID = new ID("UN/ECE 5153", "6", "VAT")
                                }
                            }
                        ]
                    },

                    TaxTotal =
                    [
                        new() {
                            TaxAmount = new Amount("SAR", 30.15),
                        },
                        new() {
                            TaxAmount = new Amount("SAR", 30.15),
                            TaxSubtotal =
                            [
                                new() {
                                    TaxableAmount = new Amount("SAR", 201.00),
                                    TaxAmount = new Amount("SAR", 30.15),
                                    TaxCategory = new TaxCategory
                                    {
                                        ID = new ID("UN/ECE 5305", "6", "S"),
                                        Percent = 15.00,
                                        TaxScheme = new TaxScheme
                                        {
                                            ID = new ID("UN/ECE 5153", "6", "VAT")
                                        }
                                    }
                                }
                            ]
                        }
                    ],

                    LegalMonetaryTotal = new LegalMonetaryTotal
                    {
                        LineExtensionAmount = new Amount("SAR", 201.00),
                        TaxExclusiveAmount = new Amount("SAR", 201.00),
                        TaxInclusiveAmount = new Amount("SAR", 231.15),
                        AllowanceTotalAmount = new Amount("SAR", 0.00),
                        PrepaidAmount = new Amount("SAR", 0.00),
                        PayableAmount = new Amount("SAR", 231.15),
                    },

                    InvoiceLine =
                    [
                        new() {
                            ID = new ID("1"),
                            InvoicedQuantity = new InvoicedQuantity("PCE", 33.000000),
                            LineExtensionAmount = new Amount("SAR", 99.00),
                            TaxTotal = new TaxTotal
                            {
                                TaxAmount = new Amount("SAR", 14.85),
                                RoundingAmount = new Amount("SAR", 113.85)
                            },

                            Item = new Item
                            {
                                Name = "كتاب",
                                ClassifiedTaxCategory = new ClassifiedTaxCategory
                                {
                                    ID = new ID("S"),
                                    Percent = 15.00,
                                    TaxScheme = new TaxScheme
                                    {
                                        ID = new ID("VAT")
                                    }
                                }
                            },

                            Price = new Price
                            {
                                PriceAmount = new Amount("SAR", 3.00),
                                AllowanceCharge = new AllowanceCharge
                                {
                                    ChargeIndicator = true,
                                    AllowanceChargeReason = "discount",
                                    Amount = new Amount("SAR", 0.00)
                                }
                            }
                        },

                        new() {
                            ID = new ID("2"),
                            InvoicedQuantity = new InvoicedQuantity("PCE", 3.000000),
                            LineExtensionAmount = new Amount("SAR", 102.00),
                            TaxTotal = new TaxTotal
                            {
                                TaxAmount = new Amount("SAR", 15.30),
                                RoundingAmount = new Amount("SAR", 117.30)
                            },
                            Item = new Item
                            {
                                Name = "قلم",
                                ClassifiedTaxCategory = new ClassifiedTaxCategory
                                {
                                    ID = new ID("S"),
                                    Percent = 15.00,
                                    TaxScheme = new TaxScheme
                                    {
                                        ID = new ID("VAT")
                                    }
                                }
                            },
                            Price = new Price
                            {
                                PriceAmount = new Amount("SAR", 34.00),
                                AllowanceCharge = new AllowanceCharge
                                {
                                    ChargeIndicator = true,
                                    AllowanceChargeReason = "discount",
                                    Amount = new Amount("SAR", 0.00)
                                }
                            }
                        }
                    ]
                };

                InvoiceGenerator ig = new(
                    invoiceObject,
                    Encoding.UTF8.GetString(Convert.FromBase64String(_CSIDBinaryToken)),
                    _EcSecp256k1Privkeypem
                );

                if (invoiceObject.InvoiceTypeCode.Name.StartsWith("01"))
                {
                    //Standard Invoice
                    ig.GetInvoiceXML(out string InvoiceHash, out string base64SignedInvoice, out string XmlFileName, out string requestApi);
                    return requestApi;
                }
                else
                {
                    //Simplified Invoice
                    ig.GetSignedInvoiceXML(out string InvoiceHash, out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out string requestApi);
                    return requestApi;
                }

            }
            catch
            {
                //Console.WriteLine($"Error creating RequestApi: {ex.Message}");
                throw;
            }
        }
    }
}
