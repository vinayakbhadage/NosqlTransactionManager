using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VexiereTranscationManager
{
  public  class ElasticSearchLockManager
    {

        private static HashSet<long> _transactionLocks = new HashSet<long>();

        internal static void AcquireLock(long id)
        {
            if (_transactionLocks.Contains(id))
            {
                throw new EntityLockException("Write Lock is already acquired on entity");
            }
            else
            {
                _transactionLocks.Add(id);
            }
        }

        internal static void ReleaseLock(long id)
        {
            if (!_transactionLocks.Remove(id))
            {
                throw new EntityLockException("Write Lock is not acquired on entity");
            }

        }
    }
}
