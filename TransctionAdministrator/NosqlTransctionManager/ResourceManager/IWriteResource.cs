using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosqlTransactionManager
{
    public interface IWriteResource<T>
    {
         string ParticipantName { get; }
         T Create(T t);       
         T Update(T t);         
         bool Delete(T t);
 
    }
}
