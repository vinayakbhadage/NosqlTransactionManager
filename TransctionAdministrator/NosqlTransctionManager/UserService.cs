using IdGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using NosqlTransactionManager.TransactionLog;

namespace NosqlTransactionManager
{
    public class UserService
    {

        public User Create(User user)
        {

            var transactionId = UniqueIdGenerator.GetNextId();
            var exceptionsList = new List<TransactionLogException>();
            try
            {


                MongoUserResource resource = new MongoUserResource();
                ESUserResource esResource = new ESUserResource();

                user.Id = UniqueIdGenerator.GetNextId();

                using (TransactionScope scope = new TransactionScope())
                {

                    TransactionExecutor<User> mongoTRM = new TransactionExecutor<User>(transactionId, resource, resource, exceptionsList);

                    mongoTRM.Create(user);

                    TransactionExecutor<User> esTRM = new TransactionExecutor<User>(transactionId, esResource, resource, exceptionsList);
                    esTRM.Create(user);

                    scope.Complete();
                }


            }
            catch (TransactionAbortedException e)
            {
                user = null;
                Debug.WriteLine(e.Message);

            }
            catch (TransactionLogException ex)
            {
                user = null;
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (exceptionsList.Count > 0)
                {
                    // log all exceptions and notify to recovery manager
                    user = null;
                    SaveTransation(transactionId, TransactionLogState.InDoubt);
                }
            }

            return user;
        }

        private static void SaveTransation(long transactionId, TransactionLogState transactionLogState)
        {
            var log = new TransactionLog<bool>();
            log.TransactionId = transactionId;
            log.Id = UniqueIdGenerator.GetNextId();
            log.State = transactionLogState;
            log.OperationType = OperationType.None;
            log.CreationTime = DateTime.UtcNow;
            log.Participant = "TransactionCoordinator";
            log.ExpiryTime = log.CreationTime.Add(TransactionManager.DefaultTimeout);
            ITransactionLogManager logManager = new TransactionLogManager();
            logManager.CreateTransactionLog<bool>(log);
        }

        public User Update(User user)
        {
            var transactionId = UniqueIdGenerator.GetNextId();
            var exceptionsList = new List<TransactionLogException>();
            try
            {


                MongoUserResource resource = new MongoUserResource();
                ESUserResource esResource = new ESUserResource();



                using (TransactionScope scope = new TransactionScope())
                {

                    TransactionExecutor<User> mongoTRM = new TransactionExecutor<User>(transactionId, resource, resource, exceptionsList);

                    mongoTRM.Update(user);

                    TransactionExecutor<User> esTRM = new TransactionExecutor<User>(transactionId, esResource, resource, exceptionsList);
                    esTRM.Update(user);

                    scope.Complete();
                }


            }
            catch (TransactionAbortedException e)
            {
                user = null;
                Debug.WriteLine(e.Message);

            }
            finally
            {
                if (exceptionsList.Count > 0)
                {
                    // log all exceptions and notify to recovery manager
                    user = null;
                    SaveTransation(transactionId, TransactionLogState.InDoubt);
                }
            }


            return user;
        }

        public User Delete(User user)
        {
            var transactionId = UniqueIdGenerator.GetNextId();
            var exceptionsList = new List<TransactionLogException>();

            try
            {
                MongoUserResource resource = new MongoUserResource();
                ESUserResource esResource = new ESUserResource();

                using (TransactionScope scope = new TransactionScope())
                {
                    TransactionExecutor<User> mongoTRM = new TransactionExecutor<User>(transactionId, resource, resource, exceptionsList);
                    mongoTRM.Delete(user);

                    TransactionExecutor<User> esTRM = new TransactionExecutor<User>(transactionId, esResource, resource, exceptionsList);
                    esTRM.Delete(user);

                    scope.Complete();
                }
            }
            catch (TransactionAbortedException e)
            {
                user = null;
                Debug.WriteLine(e.Message);
            }
            finally
            {
                if (exceptionsList.Count > 0)
                {
                    // log all exceptions and notify to recovery manager
                    user = null;
                    SaveTransation(transactionId, TransactionLogState.InDoubt);
                }
            }

            return user;
        }
    }
}
