
namespace EGS.Helpers
{
    public class VATInfoHelper
    {
        public class VATInfo
        {
            public string CategoryID { get; set; }
            public string ExemptReasonCode { get; set; }
            public string ExemptReason { get; set; }
            public string ArabicTranslation { get; set; }

            public VATInfo(string categoryID, string exemptReasonCode, string exemptReason, string arabicTranslation)
            {
                CategoryID = categoryID;
                ExemptReasonCode = exemptReasonCode;
                ExemptReason = exemptReason;
                ArabicTranslation = arabicTranslation;
            }
        }

        private static readonly Dictionary<string, VATInfo> VATDictionary;

        static VATInfoHelper()
        {
            VATDictionary = new Dictionary<string, VATInfo>()
        {
            { "S", new VATInfo("S", null, null, null) },
            { "E | VATEX-SA-29", new VATInfo("E", "VATEX-SA-29", "Financial services mentioned in Article 29 of the VAT Regulations.", "الخدمات المالية") },
            { "E | VATEX-SA-29-7", new VATInfo("E", "VATEX-SA-29-7", "Life insurance services mentioned in Article 29 of the VAT Regulations.", "عقد تأمين على الحياة") },
            { "E | VATEX-SA-30", new VATInfo("E", "VATEX-SA-30", "Real estate transactions mentioned in Article 30 of the VAT Regulations.", "الضريبة التوريدات العقارية المعفاة من") },
            { "Z | VATEX-SA-32", new VATInfo("Z", "VATEX-SA-32", "Export of goods.", "صادرات السلع من المملكة") },
            { "Z | VATEX-SA-33", new VATInfo("Z", "VATEX-SA-33", "Export of services.", "صادرات الخدمات من المملكة") },
            { "Z | VATEX-SA-34-1", new VATInfo("Z", "VATEX-SA-34-1", "The international transport of Goods.", "النقل الدولي للسلع") },
            { "Z | VATEX-SA-34-2", new VATInfo("Z", "VATEX-SA-34-2", "International transport of passengers.", "النقل الدولي للركاب") },
            { "Z | VATEX-SA-34-3", new VATInfo("Z", "VATEX-SA-34-3", "Services directly connected and incidental to a Supply of international passenger transport.", "الخدمات المرتبطة مباشرة أو عرضيًا بتوريد النقل الدولي للركاب") },
            { "Z | VATEX-SA-34-4", new VATInfo("Z", "VATEX-SA-34-4", "Supply of a qualifying means of transport.", "توريد وسائل النقل المؤهلة") },
            { "Z | VATEX-SA-34-5", new VATInfo("Z", "VATEX-SA-34-5", "Any services relating to Goods or passenger transportation, as defined in article twenty five of these Regulations.", "الخدمات ذات الصلة بنقل السلع أو الركاب، وفقًا للتعريف الوارد بالمادة الخامسة والعشرين من الالئحة التنفيذية لنظام ضريبة القيامة المضافة") },
            { "Z | VATEX-SA-35", new VATInfo("Z", "VATEX-SA-35", "Medicines and medical equipment.", "الأدوية والمعدات الطبية") },
            { "Z | VATEX-SA-36", new VATInfo("Z", "VATEX-SA-36", "Qualifying metals.", "المعادن المؤهلة") },
            { "Z | VATEX-SA-EDU", new VATInfo("Z", "VATEX-SA-EDU", "Private education to citizen.", "الخدمات التعليمية الخاصة للمواطنين") },
            { "Z | VATEX-SA-HEA", new VATInfo("Z", "VATEX-SA-HEA", "Private healthcare to citizen.", "الخدمات الصحية الخاصة للمواطنين") },
            { "Z | VATEX-SA-MLTRY", new VATInfo("Z", "VATEX-SA-MLTRY", "Supply of qualified military goods", "توريد السلع العسكرية المؤهلة") },
            { "O | VATEX-SA-OOS", new VATInfo("O", "VATEX-SA-OOS", "The reason is a free text, has to be provided by the taxpayer on case to case basis.", "السبب يتم تزويده من قبل المكلف على أساس كل حالة على حدة") }
        };
        }

        public static VATInfo GetVATInfo(string key)
        {
            if (key == null) return new VATInfo(null, null, null, null);

            return VATDictionary.TryGetValue(key, out VATInfo value) ? value : new VATInfo(null, null, null, null);
        }

    }

}
