using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosqlTransactionManager
{
    public class TransactionLogException : Exception
    {
        public long TransactionLogId { get; set; }

        public long TransactionId { get; set; }
        
        public TransactionLogException(long transactionLogId, long transactionId, Exception e)
            : base(e.Message, e)
        {
            TransactionId = transactionId;
            TransactionLogId = transactionLogId;
        }

    }
}
