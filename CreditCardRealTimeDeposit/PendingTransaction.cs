using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreditCardRealTimeDeposit
{
    public class PendingTransaction
    {
        public string accNum { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
        public Guid transId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransRef { get; set; }
    }
}
