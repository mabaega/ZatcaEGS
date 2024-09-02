using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;
using ZatcaEGS.Models;
using static ZatcaEGS.Helpers.VATInfoHelper;

namespace ZatcaEGS.Helpers
{
    public class RelayToInvoiceMapper
    {
        public static Invoice GenerateInvoiceObject(RelayData relayData, DeviceSetup deviceSetup, string zatcaUUID, Int32 iCv, string pIh)
        {
            var managerInvoicejson = InvoiceHelper.GetManagerInvoiceJson(relayData);
            ManagerInvoice managerInvoice = JsonConvert.DeserializeObject<ManagerInvoice>(managerInvoicejson);

            string invoiceCurrencyCode = managerInvoice.InvoiceParty.Currency?.Code ?? "SAR";
            string TaxCurrencyCode = "SAR";

            int invoiceType = 388;
            if (relayData.Referrer.Contains("debit-note"))
            {
                invoiceType = 383;
            }
            else if (relayData.Referrer.Contains("credit-note"))
            {
                invoiceType = 381;
            }

            string invoiceSubType = managerInvoice.CustomFields2.Strings[deviceSetup.InvoiceSubTypeGuid] ?? "0200000";

            Invoice invoice = new()
            {
                ProfileID = "reporting:1.0",
                ID = new ID(managerInvoice.Reference),
                UUID = zatcaUUID, //relayData.Key, Handle Rejected Document
                IssueDate = managerInvoice.IssueDate.ToString("yyyy-MM-dd"),
                IssueTime = "00:00:00",
                InvoiceTypeCode = new InvoiceTypeCode((InvoiceType)invoiceType, invoiceSubType),
                //Note = new Note(managerInvoice.Description + ""),
                DocumentCurrencyCode = invoiceCurrencyCode,
                TaxCurrencyCode = TaxCurrencyCode
            };

            string InvoiceRef = managerInvoice.RefInvoice?.Reference;
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

            invoice.AdditionalDocumentReference = CreateAdditionalDocumentReferences(iCv, pIh).ToArray();

            invoice.AccountingSupplierParty = CreateAccountingSupplierParty(deviceSetup);

            string partyTaxInfoJson = managerInvoice.InvoiceParty.CustomFields2.Strings[deviceSetup.PartyTaxInfoGuid];
            invoice.AccountingCustomerParty = CreateAccountingCustomerParty(partyTaxInfoJson);

            invoice.Delivery = new Delivery();
            invoice.Delivery.ActualDeliveryDate = managerInvoice.IssueDate.ToString("yyyy-MM-dd");

            if (DateAndTime.Year(managerInvoice.DueDateDate) < 2024)
            {
                invoice.Delivery.LatestDeliveryDate = managerInvoice.IssueDate.ToString("yyyy-MM-dd");
            }
            else
            {
                invoice.Delivery.LatestDeliveryDate = managerInvoice.DueDateDate.ToString("yyyy-MM-dd");
            };


            invoice.PaymentMeans = CreatePaymentMeans(managerInvoice, deviceSetup);

            //AllowanceCharge on Document Level
            //invoice.AllowanceCharge = CreateAllowanceCharge(mi, invoiceCurrencyCode, deviceSetup);
            invoice.InvoiceLine = CreateInvoiceLines(managerInvoice, invoiceCurrencyCode, deviceSetup).ToArray();
            invoice.TaxTotal = CalculateTaxTotals(managerInvoice, invoiceCurrencyCode, TaxCurrencyCode, deviceSetup).ToArray();
            invoice.LegalMonetaryTotal = CalculateLegalMonetaryTotal(managerInvoice, invoiceCurrencyCode);

            return invoice;
        }

        private static List<AdditionalDocumentReference> CreateAdditionalDocumentReferences(Int32 iCv, string pIh)
        {
            List<AdditionalDocumentReference> references = new();

            AdditionalDocumentReference referenceICV = new()
            {
                ID = new ID("ICV"),
                UUID = iCv.ToString()
            };
            references.Add(referenceICV);

            AdditionalDocumentReference referencePIH = new()
            {
                ID = new ID("PIH"),
                Attachment = new Attachment
                {
                    EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObject(pIh)
                }
            };
            references.Add(referencePIH);

            return references;
        }

        private static AccountingSupplierParty CreateAccountingSupplierParty(DeviceSetup deviceSetup)
        {
            return new AccountingSupplierParty
            {
                Party = new Party
                {
                    PartyIdentification = new PartyIdentification
                    {
                        ID = new ID
                        {
                            SchemeID = deviceSetup.IdentificationScheme,
                            Value = deviceSetup.IdentificationID
                        }
                    },
                    PostalAddress = new PostalAddress
                    {
                        StreetName = deviceSetup.StreetName,
                        BuildingNumber = deviceSetup.BuildingNumber,
                        CitySubdivisionName = deviceSetup.CitySubdivisionName,
                        CityName = deviceSetup.CityName,
                        PostalZone = deviceSetup.PostalZone,
                        Country = new Country
                        {
                            IdentificationCode = deviceSetup.CountryIdentificationCode
                        }
                    },
                    PartyTaxScheme = new PartyTaxScheme
                    {
                        CompanyID = deviceSetup.EnvironmentType == EnvironmentType.NonProduction ? "399999999900003" : deviceSetup.CompanyID,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID(deviceSetup.TaxSchemeID)
                        }
                    },
                    PartyLegalEntity = new PartyLegalEntity
                    {
                        RegistrationName = deviceSetup.RegistrationName
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
        private static PaymentMeans CreatePaymentMeans(ManagerInvoice managerInvoice, DeviceSetup deviceSetup)
        {
            var paymentMeansCode = 30;
            string paymentMeans = null;
            string instructionNote = null;

            if (managerInvoice.CustomFields2?.Strings != null)
            {
                managerInvoice.CustomFields2.Strings.TryGetValue(deviceSetup.PaymentMeansCodeGuid, out paymentMeans);
                managerInvoice.CustomFields2.Strings.TryGetValue(deviceSetup.InstructionNoteGuid, out instructionNote);
            }

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

        private static AllowanceCharge CreateAllowanceCharge(ManagerInvoice managerInvoice, string currencyCode, DeviceSetup deviceSetup)
        {
            List<Line> lines = managerInvoice.Lines;
            bool hasDiscount = managerInvoice.Discount;

            double totalDiscount = 0;

            if (hasDiscount)
            {
                List<TaxCategory> taxCategories = new();

                foreach (var line in lines)
                {
                    totalDiscount += line.DiscountAmount;

                    string itemTaxCategoryID = line.Item.CustomFields2.Strings[deviceSetup.ItemTaxCategoryGuid];
                    VATInfo vatInfo = GetVATInfo(itemTaxCategoryID);

                    if (line.TaxCode != null)
                    {
                        double rate = line.TaxCode?.Rate ?? 0;

                        TaxCategory taxCategory = new()
                        {
                            ID = new ID("UN/ECE 5305", "6", vatInfo.CategoryID),
                            TaxExemptionReasonCode = rate == 0 ? vatInfo.ExemptReasonCode : null,
                            TaxExemptionReason = rate == 0 ? vatInfo.ExemptReason : null,
                            Percent = rate,
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("UN/ECE 5153", "6", "VAT")
                            }
                        };
                        taxCategories.Add(taxCategory);
                    }
                    else
                    {
                        TaxCategory taxCategory = new()
                        {
                            ID = new ID("UN/ECE 5305", "6", itemTaxCategoryID),
                            Percent = 0,
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("UN/ECE 5153", "6", "VAT")
                            }
                        };
                        taxCategories.Add(taxCategory);
                    }
                }

                AllowanceCharge allowanceCharge = new()
                {
                    ChargeIndicator = false,
                    AllowanceChargeReason = "discount",
                    Amount = new Amount(currencyCode, totalDiscount)
                };

                allowanceCharge.TaxCategory = taxCategories.ToArray();

                return allowanceCharge;

            }

            return null;
        }


        private static List<InvoiceLine> CreateInvoiceLines(ManagerInvoice managerInvoice, string currencyCode, DeviceSetup deviceSetup)
        {
            List<Line> lines = managerInvoice.Lines;

            bool amountsIncludeTax = managerInvoice.AmountsIncludeTax;
            bool hasDiscount = managerInvoice.Discount;

            List<InvoiceLine> invoiceLines = new();
            int i = 0;

            foreach (var line in lines)
            {
                double percent = (line.TaxCode?.Rate ?? 0) / 100;
                double invoicedQuantity = Math.Round((line.Qty != 0 ? line.Qty : 1), 4);
                double discount = line.DiscountAmount * (managerInvoice.Discount ? 1 : 0);
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
                    invoiceLine.LineExtensionAmount = new Amount(currencyCode, lineExtensionAmount);
                    invoiceLine.Price = new Price
                    {
                        PriceAmount = new Amount(currencyCode, priceAmount),
                        //AllowanceCharge = hasDiscount ? new AllowanceCharge
                        //{
                        //    ChargeIndicator = false, // Set to false for a discount
                        //    AllowanceChargeReasonCode = null,
                        //    AllowanceChargeReason = "discount",
                        //    Amount = new Amount(currencyCode, discount)
                        //} : null
                    };

                    string itemTaxCategoryID = line.Item.CustomFields2.Strings[deviceSetup.ItemTaxCategoryGuid];
                    VATInfo vatInfo = GetVATInfo(itemTaxCategoryID);

                    double rate = line.TaxCode?.Rate ?? 0;

                    invoiceLine.Item.ClassifiedTaxCategory = new ClassifiedTaxCategory
                    {
                        Percent = rate,
                        ID = new ID("UN/ECE 5305", "6", itemTaxCategoryID),
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("UN/ECE 5153", "6", "VAT")
                        }
                    };

                    invoiceLine.TaxTotal = new TaxTotal
                    {
                        TaxAmount = new Amount(currencyCode, taxAmount),
                        RoundingAmount = new Amount(currencyCode, lineExtensionAmount + taxAmount)
                    };
                }
                else
                {
                    //Prepaid Amount
                    invoiceLine.ID = new ID((++i).ToString());
                    invoiceLine.Price = new Price
                    {
                        PriceAmount = new Amount(currencyCode, priceAmount),
                    };
                }

                invoiceLines.Add(invoiceLine);
            }

            return invoiceLines;
        }


        private static List<TaxTotal> CalculateTaxTotals(ManagerInvoice managerInvoice, string currencyCode, string taxCurrencyCode, DeviceSetup deviceSetup)
        {
            List<Line> lines = managerInvoice.Lines;
            bool amountsIncludeTax = managerInvoice.AmountsIncludeTax;
            double exchangeRate = managerInvoice.ExchangeRate == 0 ? 1 : managerInvoice.ExchangeRate;


            List<TaxTotal> taxTotals = new();
            double totalTaxAmount = 0;
            List<TaxSubtotal> taxSubtotals = new();

            foreach (var line in lines)
            {
                if (line.TaxCode != null)
                {
                    string itemTaxCategoryID = line.Item.CustomFields2.Strings[deviceSetup.ItemTaxCategoryGuid];
                    VATInfo vatInfo = GetVATInfo(itemTaxCategoryID);

                    double percent = (line.TaxCode?.Rate ?? 0) / 100;
                    double invoicedQuantity = Math.Round((line.Qty != 0 ? line.Qty : 1), 4);
                    double discount = line.DiscountAmount * (managerInvoice.Discount ? 1 : 0);
                    double unitPrice = ((line.UnitPrice * invoicedQuantity) - discount) / invoicedQuantity;
                    double priceAmount = Math.Round(amountsIncludeTax ? (unitPrice / (1 + percent)) : unitPrice, 4);
                    double lineExtensionAmount = Math.Round(invoicedQuantity * priceAmount, 2);
                    double taxAmount = Math.Round(lineExtensionAmount * percent, 2);

                    totalTaxAmount += taxAmount;

                    double rate = line.TaxCode?.Rate ?? 0;

                    TaxSubtotal taxSubtotal = new()
                    {
                        TaxableAmount = new Amount(currencyCode, lineExtensionAmount),
                        TaxAmount = new Amount(currencyCode, taxAmount),
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

            TaxTotal taxTotalWithSubtotals = new()
            {
                TaxAmount = new Amount(currencyCode, totalTaxAmount),
                TaxSubtotal = taxSubtotals.ToArray()
            };
            taxTotals.Add(taxTotalWithSubtotals);

            TaxTotal taxTotalWithoutSubtotals = new()
            {
                TaxAmount = new Amount(taxCurrencyCode, totalTaxAmount * exchangeRate)
            };
            taxTotals.Add(taxTotalWithoutSubtotals);

            return taxTotals;
        }

        private static LegalMonetaryTotal CalculateLegalMonetaryTotal(ManagerInvoice managerInvoice, string currencyCode)
        {
            List<Line> lines = managerInvoice.Lines;
            bool amountsIncludeTax = managerInvoice.AmountsIncludeTax;

            double sumLineExtensionAmount = 0;
            double sumTaxExclusiveAmount = 0;
            double sumTaxInclusiveAmount = 0;
            double sumAllowanceTotalAmount = 0;

            foreach (var line in lines)
            {
                double percent = (line.TaxCode?.Rate ?? 0) / 100;
                double invoicedQuantity = Math.Round((line.Qty != 0 ? line.Qty : 1), 4);
                double discount = line.DiscountAmount * (managerInvoice.Discount ? 1 : 0);
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
                LineExtensionAmount = new Amount(currencyCode, sumLineExtensionAmount),
                TaxExclusiveAmount = new Amount(currencyCode, sumTaxExclusiveAmount),
                TaxInclusiveAmount = new Amount(currencyCode, sumTaxInclusiveAmount),
                AllowanceTotalAmount = new Amount(currencyCode, sumAllowanceTotalAmount),
                PrepaidAmount = new Amount(currencyCode, 0),
                PayableAmount = new Amount(currencyCode, sumTaxInclusiveAmount)
            };
        }

    }
}

