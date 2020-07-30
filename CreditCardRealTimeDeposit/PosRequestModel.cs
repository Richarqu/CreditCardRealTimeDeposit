using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreditCardRealTimeDeposit
{
    public class PosRequestModel
    {
        public string PAN { get; set; }
        public string CardExpiry { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string LogonNextPassword { get; set; }
        public string NextChallenge { get; set; }
        public string AmountTwo { get; set; }
        public bool IsDebit { get; set; }
    }
}
