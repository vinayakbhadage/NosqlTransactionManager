using IdGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosqlTransactionManager
{
    public class UniqueIdGenerator
    {

        private static IdGenerator _generator;


        public static long GetNextId()
        {
            long id = 0;
            IdGenerator generator = GetIdGenerator();
            return generator.CreateId();
        }

        private static IdGenerator GetIdGenerator()
        {

            if (_generator == null)
            {
                // Let's say we take april 1st 2015 as our epoch
                var epoch = new DateTime(2015, 4, 1, 0, 0, 0, DateTimeKind.Utc);
                // Create a mask configuration of 45 bits for timestamp, 2 for generator-id 
                // and 16 for sequence
                var mc = new MaskConfig(45, 2, 16);
                // Create an IdGenerator with it's generator-id set to 0, our custom epoch 
                // and mask configuration
                _generator = new IdGenerator(0, epoch, mc);
            }

            return _generator;
        }

    }
}
