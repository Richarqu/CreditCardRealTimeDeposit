using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreditCardRealTimeDeposit
{
   public class InsertTransactionModel
    {
        public string UserId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string CardAccountNumber { get; set; }
        public string CardHolderEmbossingName { get; set; }
        public string Reference { get; set; }
        public string CardholderBillingCurrency { get; set; }
        public string CardholderBillingAmount { get; set; }
        public string ReversalIndicator { get; set; }
        public string SupplementaryCardNumber { get; set; }
        public string ReasonCode { get; set; }
        public string PaymentIndicator { get; set; }
        public string Filler { get; set; }
        public decimal BillingAmount { get; set; }
        public DateTime LogDate { get; set; }
        public int FailedStatus { get; set; }
        public string FileName { get; set; }
        public bool Acknowledge { get; set; }
    }
}
