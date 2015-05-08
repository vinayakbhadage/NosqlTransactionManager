using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosqlTransactionManager
{
    public class EntityLockException : Exception
    {
        private string p;

        public EntityLockException(string p)
        {
            // TODO: Complete member initialization
            this.p = p;
        }
    }
}
