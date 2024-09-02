using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;
using EGS.Models;
using static EGS.Helpers.VATInfoHelper;
using Newtonsoft.Json.Linq;

namespace EGS.Helpers
{
    public class RelayToInvoiceMapper
    {
        private readonly RelayData _relayData;
        private readonly CertificateInfo _certInfo;
        private readonly string _zatcaUUID;
        private readonly int _ICV;
        private readonly string _PIH;
        private readonly ManagerInvoice _managerInvoice;
        private readonly string _invoiceCurrencyCode;
        private readonly string _taxCurrencyCode;

        public RelayToInvoiceMapper(RelayData relayData, CertificateInfo certInfo, string zatcaUUID, int ICV, string PIH)
        {
            _relayData = relayData;
            _certInfo = certInfo;
            _zatcaUUID = zatcaUUID;
            _ICV = ICV;
            _PIH = PIH;

            _managerInvoice = DataHelper.GetManagerInvoice(_relayData.Data, _relayData.Key);
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

            string invoiceSubType = DataHelper.FindStringValueByKey(JObject.Parse(_relayData.Data), ZatcaCustomField.InvoiceSubTypeGuid) ?? "0200000";

            Invoice invoice = new()
            {
                ProfileID = "reporting:1.0",
                ID = new ID(_managerInvoice.Reference),
                UUID = _zatcaUUID,
                IssueDate = _managerInvoice.IssueDate.ToString("yyyy-MM-dd"),
                IssueTime = "00:00:00",
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

            string partyTaxInfoJson = DataHelper.FindStringValueByKey(JObject.Parse(_relayData.Data), ZatcaCustomField.PartyTaxInfoGuid);
            invoice.AccountingCustomerParty = CreateAccountingCustomerParty(partyTaxInfoJson);

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
                UUID = (_ICV).ToString()
            };
            references.Add(referenceICV);

            AdditionalDocumentReference referencePIH = new()
            {
                ID = new ID("PIH"),
                Attachment = new Attachment
                {
                    EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObject(_PIH)
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

        private static AccountingCustomerParty CreateAccountingCustomerParty(string partyTaxInfoJson)
        {
            PartyTaxInfo partyInfo = PartyTaxInfoParser.ParsePartyInfo(partyTaxInfoJson);

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
            string paymentMeans = DataHelper.FindStringValueByKey(JObject.Parse(_relayData.Data), ZatcaCustomField.PaymentMeansCodeGuid);
            string instructionNote = DataHelper.FindStringValueByKey(JObject.Parse(_relayData.Data), ZatcaCustomField.InstructionNoteGuid);

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
                double discount = line.DiscountAmount * (_managerInvoice.Discount ? 1 : 0);
                double unitPrice = ((line.UnitPrice * invoicedQuantity) - discount) / invoicedQuantity;
                double priceAmount = Math.Round(amountsIncludeTax ? (unitPrice / (1 + percent)) : unitPrice, 4);
                double lineExtensionAmount = Math.Round(invoicedQuantity * priceAmount, 2);
                double taxAmount = Math.Round(lineExtensionAmount * percent, 2);

                InvoiceLine invoiceLine = new();

                if (line.Item != null)
                {
                    invoiceLine.ID = new ID((++i).ToString());
                    invoiceLine.InvoicedQuantity = new InvoicedQuantity(line.Item.UnitName, invoicedQuantity);
                    invoiceLine.Item = new Zatca.eInvoice.Models.Item
                    {
                        Name = line.Item.ItemName
                    };
                    invoiceLine.LineExtensionAmount = new Amount(_invoiceCurrencyCode, lineExtensionAmount);
                    invoiceLine.Price = new Price
                    {
                        PriceAmount = new Amount(_invoiceCurrencyCode, priceAmount),
                    };

                    VATInfo vatInfo = new VATInfo("S",null, null, null);

                    double rate = line.TaxCode?.Rate ?? 0;
                    if (rate == 0) 
                    {
                        string itemTaxCategoryID = DataHelper.FindStringValueByKey(JObject.Parse(_relayData.Data), ZatcaCustomField.ItemTaxCategoryGuid);
                        vatInfo = GetVATInfo(itemTaxCategoryID);
                    }

                    invoiceLine.Item.ClassifiedTaxCategory = new ClassifiedTaxCategory
                    {
                        Percent = rate,
                        ID = new ID(vatInfo.CategoryID),
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("VAT")
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
                        string itemTaxCategoryID = DataHelper.FindStringValueByKey(JObject.Parse(_relayData.Data), ZatcaCustomField.ItemTaxCategoryGuid);
                        vatInfo = GetVATInfo(itemTaxCategoryID);
                    }

                    TaxSubtotal taxSubtotal = new()
                    {
                        TaxableAmount = new Amount(_invoiceCurrencyCode, lineExtensionAmount),
                        TaxAmount = new Amount(_invoiceCurrencyCode, taxAmount),
                        TaxCategory = new TaxCategory
                        {
                            Percent = rate,
                            ID = new ID(vatInfo.CategoryID),
                            TaxExemptionReasonCode = rate == 0 ? vatInfo.ExemptReasonCode : null,
                            TaxExemptionReason = rate == 0 ? vatInfo.ExemptReason : null,
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("VAT")
                            }
                        }
                    };

                    taxSubtotals.Add(taxSubtotal);
                }
            }

            TaxTotal taxTotalWithSubtotals = new()
            {
                TaxAmount = new Amount(_invoiceCurrencyCode, totalTaxAmount),
                TaxSubtotal = taxSubtotals.ToArray()
            };

            taxTotals.Add(taxTotalWithSubtotals);

            TaxTotal taxTotalWithoutSubtotals = new()
            {
                TaxAmount = new Amount(_taxCurrencyCode, totalTaxAmount * exchangeRate)
            };
            taxTotals.Add(taxTotalWithoutSubtotals);

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
                PrepaidAmount = new Amount(_invoiceCurrencyCode, 0),
                PayableAmount = new Amount(_invoiceCurrencyCode, sumTaxInclusiveAmount)
            };
        }
    }
}