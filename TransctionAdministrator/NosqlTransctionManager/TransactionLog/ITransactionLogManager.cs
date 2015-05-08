using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosqlTransactionManager.TransactionLog
{
    public interface ITransactionLogManager
    {
        void CreateTransactionLog<T>(TransactionLog<T> transactionLog);

        void UpdateTransactionLog<T>(TransactionLog<T> transactionLog);
    }
}
