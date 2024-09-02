using Newtonsoft.Json.Linq;

public class Program
{
    public static void Main()
    {
        string jsonData = @"{
            ""Referrer"": ""https://mybook.manager.io/sales-invoice-view?ogYOWmF0Y2EgZUludm9pY2WqBlwvc2FsZXMtaW52b2ljZXM_b2dZT1dtRjBZMkVnWlVsdWRtOXBZMlc0REFEQURBQzREUURvRFFEUUR3Q29FQUM0RUFESUVBRHdFQURBRVFESUVRQ1FFZ0R3LXdFQcIMEgl1Vbih2bM3SRG0uBNSYTBChcgMANAMCtgMAA"",
            ""Callback"": ""https://mybook.manager.io/callback?Cg5aYXRjYSBlSW52b2ljZQ"",
            ""Key"": ""a1b85575-b3d9-4937-b4b8-135261304285"",
            ""Data"": {
                ""BaseCurrency"": {
                    ""Code"": ""SAR"",
                    ""Name"": ""SAR"",
                    ""Symbol"": ""﷼""
                },
                ""BusinessDetails"": {
                    ""Name"": ""Maximum Speed Tech Supply LTD"",
                    ""Address"": ""الامير سلطان | Prince Sultan\nmربع | Al-Murabba\nالرياض | Riyadh\nBuilding : 2322  / Postal Code :23333"",
                    ""Country"": ""ar-SA"",
                    ""CustomFields"": {
                        ""d96d97e8-c857-42c6-8360-443c06a13de9"": ""399999999900003""
                    },
                    ""CustomFields2"": {}
                },
                ""SalesInvoice"": {
                    ""a1b85575-b3d9-4937-b4b8-135261304285"": {
                        ""IssueDate"": ""2024-08-11T00:00:00"",
                        ""DueDate"": 1,
                        ""DueDateDays"": 30,
                        ""DueDateDate"": ""2024-08-31T00:00:00"",
                        ""Reference"": ""19"",
                        ""Customer"": ""962bd1d4-3560-41a2-bd06-9701c46a2449"",
                        ""BillingAddress"": ""صلاح الدين | Salah Al-Din\nmروج | Al-Murooj\nالرياض | Riyadh\nBuildingNumber : 1111 / PostalZone : 12222"",
                        ""Lines"": [
                            {
                                ""Item"": ""7d96f7a3-ac59-41f3-ac62-c4799f0e2ac3"",
                                ""LineDescription"": ""Item with 15% Vat"",
                                ""CustomFields2"": {},
                                ""Qty"": 1.0,
                                ""SalesUnitPrice"": 2000.0,
                                ""TaxCode"": ""1731afd8-40df-484c-8335-81a1451ab8f8""
                            }
                        ],
                        ""HasLineNumber"": true,
                        ""HasLineDescription"": true,
                        ""ShowTaxAmountColumn"": true,
                        ""AlsoActsAsDeliveryNote"": true,
                        ""HasSalesInvoiceFooters"": true,
                        ""SalesInvoiceFooters"": [
                            ""e9a08b8d-0c2f-411b-a7f0-76dd4a6d6968""
                        ],
                        ""HasRelay"": true,
                        ""Relay"": ""https://relay.manager.io"",
                        ""CustomFields2"": {
                            ""Strings"": {
                                ""df844e4d-2ccc-4e46-9e3b-ac51a80868a2"": ""30 | Credit"",
                                ""e1050215-e02a-4de0-9d55-cf0dba27a0d6"": ""0200000"",
                                ""3c4c1fb3-3b0e-4f2e-9eeb-2d466c496e2f"": """"
                            }
                        }
                    }
                },
                ""Customer"": {
                    ""962bd1d4-3560-41a2-bd06-9701c46a2449"": {
                        ""Name"": ""شركة نماذج فاتورة المحدودة | Fatoora Samples LTD"",
                        ""BillingAddress"": ""صلاح الدين | Salah Al-Din\nmروج | Al-Murooj\nالرياض | Riyadh\nBuildingNumber : 1111 / PostalZone : 12222"",
                        ""DeliveryAddress"": ""صلاح الدين | Salah Al-Din\nmروج | Al-Murooj\nالرياض | Riyadh\nBuildingNumber : 1111 / PostalZone : 12222"",
                        ""Email"": ""customer01@customer.net"",
                        ""HasDefaultDueDateDays"": true,
                        ""DefaultDueDateDays"": 30,
                        ""CustomFields"": {
                            ""7de1f605-f0a8-4cae-80a4-664d6dbe70d1"": ""399999999800003""
                        },
                        ""CustomFields2"": {
                            ""Strings"": {
                                ""93f79973-5346-4c6b-b912-90ea9bbf69c2"": ""\""StreetName\"": \""صلاح الدين | Salah Al-DinX\""\n\""BuildingNumber\"": \""1111\""\n\""CitySubdivisionName\"": \""المروج | Al-Murooj\""\n\""CityName\"": \""الرياض | Riyadh\""\n\""PostalZone\"": \""12222\""\n\""CountryIdentificationCode\"": \""SA\""\n\""TaxSchemeCompanyID\"": \""399999999800003\""\n\""TaxSchemeID\"": \""VAT\""\n\""RegistrationName\"": \""شركة نماذج فاتورة المحدودة | Fatoora Samples LTD\""""
                            }
                        }
                    }
                },
                ""InventoryItem"": {
                    ""7d96f7a3-ac59-41f3-ac62-c4799f0e2ac3"": {
                        ""ItemCode"": """",
                        ""ItemName"": ""Item 15% Vat"",
                        ""UnitName"": ""PCE"",
                        ""HasDefaultLineDescription"": true,
                        ""DefaultLineDescription"": ""Item with 15% Vat"",
                        ""HasDefaultTaxCode"": true,
                        ""DefaultTaxCode"": ""1731afd8-40df-484c-8335-81a1451ab8f8"",
                        ""HideItemNameOnPrintedDocuments"": true,
                        ""CustomFields2"": {
                            ""Strings"": {
                                ""6862773d-f847-486e-824b-0b42aaf0cf17"": ""S"",
                                ""88797f0a-d3aa-4de6-8e0c-76ab1131b206"": """",
                                ""2ef3daa5-c4cf-4cdc-a19c-5a14ea8fcfb5"": """"
                            }
                        }
                    }
                },
                ""TaxCode"": {
                    ""1731afd8-40df-484c-8335-81a1451ab8f8"": {
                        ""Name"": ""VAT 15%"",
                        ""Label"": ""15%"",
                        ""ReportingCategory"": ""d4c5c416-894c-48d5-ab2c-0a566744c92d"",
                        ""TaxRate"": 2,
                        ""Type"": 1,
                        ""Rate"": 15.0,
                        ""Account"": ""de183911-fe83-4c61-98e0-823b929b8005"",
                        ""TaxAmountReportingCategory"": ""6521e584-ae2c-4656-ad16-14b139f903e8"",
                        ""Components"": [
                            {
                                ""Name"": ""ضريبة القيمة المضافة 15%"",
                                ""ComponentRate"": 15.0,
                                ""ComponentAccount"": ""de183911-fe83-4c61-98e0-823b929b8005"",
                                ""ComponentTaxAmountReportingCategory"": ""6521e584-ae2c-4656-ad16-14b139f903e8""
                            }
                        ],
                        ""CustomSalesInvoiceTitle"": true,
                        ""SalesInvoiceTitle"": ""فاتورة ضريبية - Tax Invoice"",
                        ""CustomCreditNoteTitle"": true,
                        ""CreditNoteTitle"": ""إشعار دائن ضريبي - Tax Credit Note"",
                        ""CustomFields"": {
                            ""988936d4-c5bb-41af-a5d8-c3c503b4a22d"": ""VAT 15%""
                        }
                    }
                },
                ""TextCustomField"": {
                    ""df844e4d-2ccc-4e46-9e3b-ac51a80868a2"": {
                        ""Name"": ""Payment Means"",
                        ""Position"": 2,
                        ""Placement"": [
                            ""ad12b60b-23bf-4421-94df-8be79cef533e"",
                            ""245e5943-0092-409d-96ae-e2ee10eac75b"",
                            ""274fc6d0-2eac-43d0-8286-79c856e644aa""
                        ],
                        ""Type"": 2,
                        ""Size"": 2,
                        ""Description"": """"
                    },
                    ""e1050215-e02a-4de0-9d55-cf0dba27a0d6"": {
                        ""Name"": ""Invoice Sub Type"",
                        ""Position"": 1,
                        ""Placement"": [
                            ""274fc6d0-2eac-43d0-8286-79c856e644aa"",
                            ""ad12b60b-23bf-4421-94df-8be79cef533e"",
                            ""245e5943-0092-409d-96ae-e2ee10eac75b""
                        ],
                        ""OptionsForDropdownList"": ""Normal (1)\nSimplified (2)""
                    },
                    ""3c4c1fb3-3b0e-4f2e-9eeb-2d466c496e2f"": {
                        ""Name"": ""QR Code"",
                        ""Position"": 3,
                        ""Placement"": [
                            ""ad12b60b-23bf-4421-94df-8be79cef533e"",
                            ""274fc6d0-2eac-43d0-8286-79c856e644aa"",
                            ""245e5943-0092-409d-96ae-e2ee10eac75b""
                        ],
                        ""Type"": 1,
                        ""Size"": 2,
                        ""Description"": """"
                    },
                    ""93f79973-5346-4c6b-b912-90ea9bbf69c2"": {
                        ""Name"": ""PartyTax Info"",
                        ""Placement"": [
                            ""ec37c11e-2b67-49c6-8a58-6eccb7dd75ee"",
                            ""6d2dc48d-2053-4e45-8330-285ebd431242""
                        ],
                        ""Type"": 1,
                        ""Size"": 1,
                        ""Description"": """"
                    },
                    ""6862773d-f847-486e-824b-0b42aaf0cf17"": {
                        ""Name"": ""Tax Category Info"",
                        ""Position"": 1,
                        ""Placement"": [
                            ""0dbdbf8a-d80c-48e6-b453-bb7862445b7c"",
                            ""7affe9ee-731f-4936-8acf-15cae7bcacee"",
                            ""efc4f2cc-acf0-4815-a9a8-13bae00c6167""
                        ],
                        ""Type"": 2,
                        ""Size"": 1
                    }
                }
            }
        }";

        JObject parsedData = JObject.Parse(jsonData);

        // Get the Key value
        string key = parsedData["Key"]?.ToString();

        // Generate the merged object
        JObject result = GenerateMergedObject(parsedData["Data"] as JObject, parsedData["Data"] as JObject);

        // Find and print the result
        JToken xresult = FindValueByKey(result, key);
        Console.WriteLine(xresult?.ToString() ?? $"Key '{key}' not found in the JSON structure.");

        Console.ReadLine();
    }

    public static JObject GenerateMergedObject(JObject data, JObject root)
    {
        if (data == null) return new JObject();

        JObject result = new JObject();
        foreach (var prop in data.Properties())
        {
            result[prop.Name] = ProcessValue(prop.Value, root);
        }

        return result;
    }

    private static JToken ProcessValue(JToken value, JObject root)
    {
        switch (value.Type)
        {
            case JTokenType.Object:
                return GenerateMergedObject(value as JObject, root);
            case JTokenType.Array:
                return ProcessArray(value as JArray, root);
            case JTokenType.String when IsGuid(value.ToString()):
                return FindGuidReplacement(root, value.ToString()) ?? value;
            default:
                return value;
        }
    }

    private static JArray ProcessArray(JArray array, JObject root)
    {
        JArray newArray = new JArray();
        foreach (var item in array)
        {
            newArray.Add(ProcessValue(item, root));
        }

        return newArray;
    }

    private static bool IsGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }

    private static JToken FindGuidReplacement(JObject root, string guid)
    {
        foreach (var prop in root.Properties())
        {
            if (prop.Name == guid)
                return prop.Value;

            if (prop.Value is JObject childObject)
            {
                var result = FindGuidReplacement(childObject, guid);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    public static JToken FindValueByKey(JToken token, string key)
    {
        if (token == null) return null;

        if (token.Type == JTokenType.Object)
        {
            var obj = token as JObject;
            if (obj.TryGetValue(key, out var value))
                return value;

            foreach (var property in obj.Properties())
            {
                var result = FindValueByKey(property.Value, key);
                if (result != null)
                    return result;
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                var result = FindValueByKey(item, key);
                if (result != null)
                    return result;
            }
        }

        return null;
    }
}