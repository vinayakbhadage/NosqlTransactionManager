using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Jil;
using StackExchange.Redis.Extensions.MsgPack;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosqlTransactionManager.TransactionLog
{
    public class TransactionLogManager : ITransactionLogManager
    {
        private static StackExchangeRedisCacheClient _redisClient = null;

        public TransactionLogManager()
        {
            var serializer = new JilSerializer();
            _redisClient = new StackExchangeRedisCacheClient(serializer);
        }


        public void CreateTransactionLog<T>(TransactionLog<T> transactionLog)
        {
            try
            {
                _redisClient.Add(GetKey<T>(transactionLog), transactionLog);
            }
            catch (Exception ex)
            {
                throw new TransactionLogException(transactionLog.Id, transactionLog.TransactionId, ex);
            }

        }

        private static string GetKey<T>(TransactionLog<T> transactionLog)
        {
            return typeof(T).Name + ":" + transactionLog.TransactionId.ToString() + ":" + transactionLog.Id.ToString();
        }

        public void UpdateTransactionLog<T>(TransactionLog<T> transactionLog)
        {
            try
            {
                _redisClient.Replace(GetKey<T>(transactionLog), transactionLog);
            }
            catch (Exception ex)
            {
                throw new TransactionLogException(transactionLog.Id, transactionLog.TransactionId, ex);
            }

        }
    }
}
