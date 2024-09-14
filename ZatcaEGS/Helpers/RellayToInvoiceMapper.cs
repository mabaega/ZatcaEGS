using Microsoft.VisualBasic;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;
using ZatcaEGS.Models;
using static ZatcaEGS.Helpers.VATInfoHelper;

namespace ZatcaEGS.Helpers
{
    public class RelayToInvoiceMapper
    {
        private readonly RelayData _relayData;
        private readonly CertificateInfo _certInfo;

        private readonly ManagerInvoice _managerInvoice;
        private readonly string _invoiceCurrencyCode;
        private readonly string _taxCurrencyCode;

        public RelayToInvoiceMapper(RelayData relayData)
        {
            _relayData = relayData;

            _certInfo = ObjectCompressor.DeserializeFromBase64String<CertificateInfo>(relayData.CertInfoString);

            _managerInvoice = relayData.ManagerInvoice;
            _invoiceCurrencyCode = _managerInvoice.InvoiceParty.Currency?.Code ?? "SAR";
            _taxCurrencyCode = "SAR";
        }

        public Invoice GenerateInvoiceObject()
        {
            int invoiceType = 388;
            if (_relayData.Referrer.Contains("debit-note"))
            {
                invoiceType = 383;
            }
            else if (_relayData.Referrer.Contains("credit-note"))
            {
                invoiceType = 381;
            }

            string invoiceSubType = JsonParser.FindStringByGuid(_relayData.InvoiceJson, ManagerCustomField.InvoiceSubTypeGuid, "RefInvoice") ?? "0200000";

            string dateCreated = null;
            string timeCreated = null;

            if (!string.IsNullOrEmpty(_relayData.DateCreated) && _relayData.DateCreated.Contains(' '))
            {
                var dateTimeParts = _relayData.DateCreated.Split(' ');
                dateCreated = dateTimeParts[0];
                timeCreated = dateTimeParts[1];
            }

            Invoice invoice = new()
            {
                ProfileID = "reporting:1.0",
                ID = new ID(_managerInvoice.Reference),
                UUID = _relayData.ZatcaUUID,

                IssueDate = dateCreated ?? _managerInvoice.IssueDate.ToString("yyyy-MM-dd"),
                IssueTime = timeCreated ?? "00:00:00",

                InvoiceTypeCode = new InvoiceTypeCode((InvoiceType)invoiceType, invoiceSubType),
                DocumentCurrencyCode = _invoiceCurrencyCode,
                TaxCurrencyCode = _taxCurrencyCode
            };

            string InvoiceRef = _managerInvoice.RefInvoice?.Reference;
            if (InvoiceRef != null)
            {
                invoice.BillingReference = new BillingReference
                {
                    InvoiceDocumentReference = new InvoiceDocumentReference
                    {
                        ID = new ID(InvoiceRef)
                    }
                };
            }

            invoice.AdditionalDocumentReference = CreateAdditionalDocumentReferences().ToArray();

            invoice.AccountingSupplierParty = CreateAccountingSupplierParty();

            invoice.AccountingCustomerParty = CreateAccountingCustomerParty();

            invoice.Delivery = new Delivery();
            invoice.Delivery.ActualDeliveryDate = _managerInvoice.IssueDate.ToString("yyyy-MM-dd");

            if (DateAndTime.Year(_managerInvoice.DueDateDate) < 2024)
            {
                invoice.Delivery.LatestDeliveryDate = _managerInvoice.IssueDate.ToString("yyyy-MM-dd");
            }
            else
            {
                invoice.Delivery.LatestDeliveryDate = _managerInvoice.DueDateDate.ToString("yyyy-MM-dd");
            }

            invoice.PaymentMeans = CreatePaymentMeans();

            invoice.InvoiceLine = CreateInvoiceLines().ToArray();
            invoice.TaxTotal = CalculateTaxTotals().ToArray();
            invoice.LegalMonetaryTotal = CalculateLegalMonetaryTotal();

            return invoice;
        }

        private List<AdditionalDocumentReference> CreateAdditionalDocumentReferences()
        {
            List<AdditionalDocumentReference> references = new();

            AdditionalDocumentReference referenceICV = new()
            {
                ID = new ID("ICV"),
                UUID = (_relayData.LastICV + 1).ToString()
            };
            references.Add(referenceICV);

            AdditionalDocumentReference referencePIH = new()
            {
                ID = new ID("PIH"),
                Attachment = new Attachment
                {
                    EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObject(_relayData.LastPIH)
                }
            };
            references.Add(referencePIH);

            return references;
        }

        private AccountingSupplierParty CreateAccountingSupplierParty()
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
                        CompanyID = _certInfo.EnvironmentType == EnvironmentType.NonProduction ? "399999999900003" : _certInfo.CompanyID,
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

        private AccountingCustomerParty CreateAccountingCustomerParty()
        {
            PartyTaxInfo partyInfo = _relayData.PartyInfo;

            return new AccountingCustomerParty
            {
                Party = new Party
                {
                    PostalAddress = new PostalAddress
                    {
                        StreetName = partyInfo.StreetName,
                        BuildingNumber = partyInfo.BuildingNumber,
                        CitySubdivisionName = partyInfo.CitySubdivisionName,
                        CityName = partyInfo.CityName,
                        PostalZone = partyInfo.PostalZone,
                        Country = new Country
                        {
                            IdentificationCode = partyInfo.CountryIdentificationCode
                        }
                    },
                    PartyTaxScheme = new PartyTaxScheme
                    {
                        CompanyID = partyInfo.CompanyID,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID(partyInfo.TaxSchemeID)
                        }
                    },
                    PartyLegalEntity = new PartyLegalEntity
                    {
                        RegistrationName = partyInfo.RegistrationName
                    }
                }
            };
        }

        private PaymentMeans CreatePaymentMeans()
        {
            var paymentMeansCode = 30;
            string paymentMeans = JsonParser.FindStringByGuid(_relayData.InvoiceJson, ManagerCustomField.PaymentMeansCodeGuid, "RefInvoice");
            string instructionNote = JsonParser.FindStringByGuid(_relayData.InvoiceJson, ManagerCustomField.InstructionNoteGuid, "RefInvoice");

            if (paymentMeans != null)
            {
                var parts = paymentMeans.Split('|');
                if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out int paymentCode))
                {
                    paymentMeansCode = paymentCode;
                }
            }

            return new PaymentMeans()
            {
                PaymentMeansCode = paymentMeansCode.ToString(),
                InstructionNote = instructionNote,
            };
        }

        private List<InvoiceLine> CreateInvoiceLines()
        {
            List<Line> lines = _managerInvoice.Lines;

            bool amountsIncludeTax = _managerInvoice.AmountsIncludeTax;
            bool hasDiscount = _managerInvoice.Discount;

            List<InvoiceLine> invoiceLines = new();
            int i = 0;

            foreach (var line in lines)
            {
                double percent = (line.TaxCode?.Rate ?? 0) / 100;
                double invoicedQuantity = Math.Round((line.Qty != 0 ? line.Qty : 1), 4);
                double discount = line.DiscountAmount * (hasDiscount ? 1 : 0);
                double unitPrice = ((line.UnitPrice * invoicedQuantity) - discount) / invoicedQuantity;
                double priceAmount = Math.Round(amountsIncludeTax ? (unitPrice / (1 + percent)) : unitPrice, 4);
                double lineExtensionAmount = Math.Round(invoicedQuantity * priceAmount, 2);
                double taxAmount = Math.Round(lineExtensionAmount * percent, 2);

                InvoiceLine invoiceLine = new();

                if (line.Item != null)
                {
                    invoiceLine.ID = new ID((++i).ToString());
                    invoiceLine.InvoicedQuantity = new InvoicedQuantity(line.Item.UnitName ?? "", invoicedQuantity);
                    invoiceLine.Item = new Zatca.eInvoice.Models.Item
                    {
                        Name = line.Item.ItemName ?? line.Item.Name,
                    };
                    invoiceLine.LineExtensionAmount = new Amount(_invoiceCurrencyCode, lineExtensionAmount);
                    invoiceLine.Price = new Price
                    {
                        PriceAmount = new Amount(_invoiceCurrencyCode, priceAmount),
                    };

                    VATInfo vatInfo = new VATInfo("S", null, null, null);

                    double rate = line.TaxCode?.Rate ?? 0;
                    if (rate == 0)
                    {
                        string itemTaxCategoryID = line.Item.CustomFields2.Strings[ManagerCustomField.ItemTaxCategoryGuid];
                        vatInfo = GetVATInfo(itemTaxCategoryID);
                    }

                    invoiceLine.Item.ClassifiedTaxCategory = new ClassifiedTaxCategory
                    {
                        Percent = rate,
                        ID = new ID("UN/ECE 5305", "6", vatInfo.CategoryID),
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("UN/ECE 5153", "6", "VAT")
                        }
                    };

                    invoiceLine.TaxTotal = new TaxTotal
                    {
                        TaxAmount = new Amount(_invoiceCurrencyCode, taxAmount),
                        RoundingAmount = new Amount(_invoiceCurrencyCode, lineExtensionAmount + taxAmount)
                    };
                }
                else
                {
                    //Prepaid Amount
                    invoiceLine.ID = new ID((++i).ToString());
                    invoiceLine.Price = new Price
                    {
                        PriceAmount = new Amount(_invoiceCurrencyCode, priceAmount),
                    };
                }

                invoiceLines.Add(invoiceLine);
            }

            return invoiceLines;
        }

        private List<TaxTotal> CalculateTaxTotals()
        {
            List<Line> lines = _managerInvoice.Lines;
            bool amountsIncludeTax = _managerInvoice.AmountsIncludeTax;
            double exchangeRate = _managerInvoice.ExchangeRate == 0 ? 1 : _managerInvoice.ExchangeRate;

            List<TaxTotal> taxTotals = new();
            double totalTaxAmount = 0;
            List<TaxSubtotal> taxSubtotals = new();

            foreach (var line in lines)
            {
                if (line.TaxCode != null)
                {

                    double percent = (line.TaxCode?.Rate ?? 0) / 100;
                    double invoicedQuantity = Math.Round((line.Qty != 0 ? line.Qty : 1), 4);
                    double discount = line.DiscountAmount * (_managerInvoice.Discount ? 1 : 0);
                    double unitPrice = ((line.UnitPrice * invoicedQuantity) - discount) / invoicedQuantity;
                    double priceAmount = Math.Round(amountsIncludeTax ? (unitPrice / (1 + percent)) : unitPrice, 4);
                    double lineExtensionAmount = Math.Round(invoicedQuantity * priceAmount, 2);
                    double taxAmount = Math.Round(lineExtensionAmount * percent, 2);

                    totalTaxAmount += taxAmount;

                    VATInfo vatInfo = new VATInfo("S", null, null, null);

                    double rate = line.TaxCode?.Rate ?? 0;
                    if (rate == 0)
                    {
                        string itemTaxCategoryID = line.Item.CustomFields2.Strings[ManagerCustomField.ItemTaxCategoryGuid];
                        vatInfo = GetVATInfo(itemTaxCategoryID);
                    }

                    TaxSubtotal taxSubtotal = new()
                    {
                        TaxableAmount = new Amount(_invoiceCurrencyCode, lineExtensionAmount),
                        TaxAmount = new Amount(_invoiceCurrencyCode, taxAmount),
                        TaxCategory = new TaxCategory
                        {
                            Percent = rate,
                            ID = new ID("UN/ECE 5305", "6", vatInfo.CategoryID),
                            TaxExemptionReasonCode = rate == 0 ? vatInfo.ExemptReasonCode : null,
                            TaxExemptionReason = rate == 0 ? vatInfo.ExemptReason : null,
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("UN/ECE 5153", "6", "VAT")
                            }
                        }
                    };

                    taxSubtotals.Add(taxSubtotal);
                }
            }

            TaxTotal taxTotalWithoutSubtotals = new()
            {
                TaxAmount = new Amount(_taxCurrencyCode, totalTaxAmount * exchangeRate)
            };
            taxTotals.Add(taxTotalWithoutSubtotals);


            TaxTotal taxTotalWithSubtotals = new()
            {
                TaxAmount = new Amount(_invoiceCurrencyCode, totalTaxAmount),
                TaxSubtotal = taxSubtotals.ToArray()
            };

            taxTotals.Add(taxTotalWithSubtotals);


            return taxTotals;
        }

        private LegalMonetaryTotal CalculateLegalMonetaryTotal()
        {
            List<Line> lines = _managerInvoice.Lines;
            bool amountsIncludeTax = _managerInvoice.AmountsIncludeTax;

            double sumLineExtensionAmount = 0;
            double sumTaxExclusiveAmount = 0;
            double sumTaxInclusiveAmount = 0;
            double sumAllowanceTotalAmount = 0;

            foreach (var line in lines)
            {
                double percent = (line.TaxCode?.Rate ?? 0) / 100;
                double invoicedQuantity = Math.Round((line.Qty != 0 ? line.Qty : 1), 4);
                double discount = line.DiscountAmount * (_managerInvoice.Discount ? 1 : 0);
                double unitPrice = ((line.UnitPrice * invoicedQuantity) - discount) / invoicedQuantity;
                double priceAmount = Math.Round(amountsIncludeTax ? (unitPrice / (1 + percent)) : unitPrice, 4);
                double lineExtensionAmount = Math.Round(invoicedQuantity * priceAmount, 2);
                double taxAmount = Math.Round(lineExtensionAmount * percent, 2);

                sumLineExtensionAmount += lineExtensionAmount;
                sumTaxExclusiveAmount += lineExtensionAmount;
                sumTaxInclusiveAmount += lineExtensionAmount + taxAmount;
                //sumAllowanceTotalAmount += discount;
            }

            return new LegalMonetaryTotal
            {
                LineExtensionAmount = new Amount(_invoiceCurrencyCode, sumLineExtensionAmount),
                TaxExclusiveAmount = new Amount(_invoiceCurrencyCode, sumTaxExclusiveAmount),
                TaxInclusiveAmount = new Amount(_invoiceCurrencyCode, sumTaxInclusiveAmount),
                AllowanceTotalAmount = new Amount(_invoiceCurrencyCode, sumAllowanceTotalAmount),
                //ChargeTotalAmount = new Amount(_invoiceCurrencyCode, 0),
                PrepaidAmount = new Amount(_invoiceCurrencyCode, 0),
                //PayableRoundingAmount = new Amount(_invoiceCurrencyCode,0),
                PayableAmount = new Amount(_invoiceCurrencyCode, sumTaxInclusiveAmount)
            };
        }
    }
}