using System;
namespace NosqlTransactionManager.ResourceManager
{
   public interface IReadResource<T>
    {
        T GetById(long id);
        long GetId(T t);
    }
}
