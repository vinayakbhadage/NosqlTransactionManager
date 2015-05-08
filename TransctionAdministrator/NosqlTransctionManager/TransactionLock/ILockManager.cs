using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosqlTransactionManager
{
    public interface ILockManager
    {
        void AcquireLock(long id, string participantName);
        void ReleaseLock(long id, string participantName);
    }
}
