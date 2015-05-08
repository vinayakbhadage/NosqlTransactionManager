using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosqlTransactionManager
{
    public class LockManager : ILockManager
    {
        private static HashSet<string> _transactionLocks = new HashSet<string>();
        private static StackExchangeRedisCacheClient _redisClient = null;
        public LockManager()
        {
            var serializer = new NewtonsoftSerializer();
            _redisClient = new StackExchangeRedisCacheClient(serializer);
        }

        public void AcquireLock(long id, string participantName)
        {
            if (_redisClient.Exists(GetLockKey(id, participantName)))
            {
                throw new EntityLockException("Write Lock is already acquired on entity");
            }
            else
            {
                _redisClient.Add(GetLockKey(id, participantName), Boolean.TrueString);
               // _transactionLocks.Add(GetLockKey(id, participantName));
            }
        }



        public void ReleaseLock(long id, string participantName)
        {
            if (!_redisClient.Remove(GetLockKey(id, participantName)))
            {
                throw new EntityLockException("Write Lock is not acquired on entity");
            }
        }

        private static string GetLockKey(long id, string participantName)
        {
            return participantName + ":" + id;
        }
    }
}
