﻿using System.ComponentModel.DataAnnotations;
using Zatca.eInvoice.Helpers;
using ZatcaEGS.Helpers;

namespace ZatcaEGS.Models
{
    public class ApprovedInvoice
    {
        [Key]
        // We can not reuse Manager UUID when Invoice are Rejected by Zatca Server
        public string ZatcaUUID { get; set; }
        public string ManagerUUID { get; set; }
        public string InvoiceType { get; set; }
        public string InvoiceSubType { get; set; }
        public string Reference { get; set; }
        public string IssueDate { get; set; }
        public string PartyName { get; set; }
        public string CurrencyCode { get; set; } = "SAR";

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; } = 0;
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal TaxAmount { get; set; } = 0;
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; } = 0;

        public int ICV { get; set; }

        public string RequestType { get; set; }
        public string StatusCode { get; set; }

        public string ApprovalStatus { get; set; }

        //public string ClearanceStatus { get; set; }
        //public string ReportingStatus { get; set; }

        public string PIH { get; set; }
        public string InvoiceHash { get; set; }

        public string EditData { get; set; } //Manager Data
        public string Base64Invoice { get; set; } //Manager Data
        public string Referrer { get; set; } //Manager Data

        public string ServerResult { get; set; }

        public string Base64SignedInvoice { get; set; }
        public string Base64QrCode { get; set; }
        public string XmlFileName { get; set; }

        public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.NonProduction;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        private string _decodedQrCode;
        public string DecodedQrCode
        {
            get
            {
                _decodedQrCode = QrCodeDecoder.GetDecodedContentAsString(Base64QrCode);
                return _decodedQrCode;
            }
        }
    }
}
