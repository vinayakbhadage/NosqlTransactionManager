using Nest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace VexiereTranscationManager
{
    public class ElasticSearchResourceManager : IEnlistmentNotification
    {

        private List<TransactionLog> _transactionLogs;
        private string transactionId;
        private ElasticClient _elasticClient;

        public ElasticSearchResourceManager(string transactionId)
        {
            FailedOnCommit = false;
            this.transactionId = transactionId;

            var transaction = Transaction.Current;

            if (transaction != null)
            {
                transaction.EnlistVolatile(this, EnlistmentOptions.None);
            }

            _transactionLogs = new List<TransactionLog>();


            var node = new Uri(VexiereConfiguration.ElasticSearchUri);

            var settings = new ConnectionSettings(
                node,
                defaultIndex: "twopc"
            );

            _elasticClient = new ElasticClient(settings);

        }



        public void Commit(Enlistment enlistment)
        {
            Debug.WriteLine("iN ES Commit");
            try
            {

                if (FailedOnCommit)
                    throw new Exception("Make commit failed");

                foreach (var transactionLog in _transactionLogs)
                {
                    switch (transactionLog.OperationType)
                    {
                        case OperationType.Create:
                            CommitCreateTrabsactionLog(transactionLog);
                            break;
                        case OperationType.Update:
                            CommitUpdateOrDeleteTrabsactionLog(transactionLog);
                            break;
                        case OperationType.Delete:
                            CommitUpdateOrDeleteTrabsactionLog(transactionLog);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
               
            }
            enlistment.Done();
        }

        private void CommitUpdateOrDeleteTrabsactionLog(TransactionLog transactionLog)
        {
            transactionLog.State = TransactionLogState.Committed;
            var logResponse = _elasticClient.Index<TransactionLog>(transactionLog);

            ElasticSearchLockManager.ReleaseLock(transactionLog.Entity.Id);
        }

        private void CommitCreateTrabsactionLog(TransactionLog transactionLog)
        {
            transactionLog.State = TransactionLogState.Committed;
            var logResponse = _elasticClient.Index<TransactionLog>(transactionLog);
        }

        public void InDoubt(Enlistment enlistment)
        {
            throw new NotImplementedException();
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
          

            Debug.WriteLine("iN ES Prepare");
            try
            {
               
                foreach (var transactionLog in _transactionLogs)
                {
                    switch (transactionLog.OperationType)
                    {
                        case OperationType.Create:
                            PrepareCreateTransactionLog(transactionLog);
                            break;
                        case OperationType.Update:
                            PrepareUpdateTransactionLog(transactionLog);
                            break;
                        case OperationType.Delete:
                            PrepareDeleteTransactionLog(transactionLog);
                            break;
                    }
                }

                preparingEnlistment.Prepared();
            }
            catch (Exception ex)
            {
                preparingEnlistment.ForceRollback(ex);
            }
        }

        private void PrepareDeleteTransactionLog(TransactionLog transactionLog)
        {
            ElasticSearchLockManager.AcquireLock(transactionLog.Entity.Id);

            transactionLog.OldEntity = _elasticClient.Get<User>(u => u.Id(transactionLog.Entity.Id)).Source;

            transactionLog.State = TransactionLogState.ReadyToCommit;

            var logResponse = _elasticClient.Index<TransactionLog>(transactionLog);

            var entityResponse = _elasticClient.Delete<User>(transactionLog.Entity.Id);
        }

        private void PrepareUpdateTransactionLog(TransactionLog transactionLog)
        {
            ElasticSearchLockManager.AcquireLock(transactionLog.Entity.Id);

            transactionLog.OldEntity = _elasticClient.Get<User>(u => u.Id(transactionLog.Entity.Id)).Source;

            transactionLog.State = TransactionLogState.ReadyToCommit;

            var logResponse = _elasticClient.Index<TransactionLog>(transactionLog);

            var entityResponse = _elasticClient.Index<User>(transactionLog.Entity);
        }

        private void PrepareCreateTransactionLog(TransactionLog transactionLog)
        {
            transactionLog.State = TransactionLogState.ReadyToCommit;
            var logResponse = _elasticClient.Index<TransactionLog>(transactionLog);
            var entityResponse = _elasticClient.Index<User>(transactionLog.Entity);
        }

        public void Rollback(Enlistment enlistment)
        {
            Debug.WriteLine("iN ES Rollback");
            try
            {
                foreach (var transactionLog in _transactionLogs)
                {
                    switch (transactionLog.OperationType)
                    {
                        case OperationType.Create:
                            RollbackCreateTransactionLog(transactionLog);
                            break;
                        case OperationType.Update:
                            RollbackUpdateOrDeleteTransactionLog(transactionLog);
                            break;
                        case OperationType.Delete:
                            RollbackUpdateOrDeleteTransactionLog(transactionLog);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                // log the error message
            }
            enlistment.Done();
        }

        private void RollbackUpdateOrDeleteTransactionLog(TransactionLog transactionLog)
        {
            transactionLog.State = TransactionLogState.Rollback;
            var logResponse = _elasticClient.Index<TransactionLog>(transactionLog);
            var entityResponse = _elasticClient.Index<User>(transactionLog.OldEntity);

           ElasticSearchLockManager.ReleaseLock(transactionLog.Entity.Id);
        }

        private void RollbackCreateTransactionLog(TransactionLog transactionLog)
        {
            transactionLog.State = TransactionLogState.Rollback;
            var logResponse = _elasticClient.Index<TransactionLog>(transactionLog);
            var entityResponse = _elasticClient.Delete<User>(u => u.Id(transactionLog.Entity.Id));
        }

        internal void Create(User user)
        {
            FillTransactionLog(user, OperationType.Create);
        }

        internal void Update(User user)
        {
            FillTransactionLog(user, OperationType.Update);
        }
        
        internal void Delete(User user)
        {
            FillTransactionLog(user, OperationType.Delete);
        }

        private void FillTransactionLog(User user, OperationType operationType)
        {
            var transactionLog = new TransactionLog();
            transactionLog.Id = UniqueIdGenerator.GetNextId();
            transactionLog.TransactionId = transactionId;
            transactionLog.OperationType = operationType;
            transactionLog.Entity = user;
            transactionLog.State = TransactionLogState.Ready;
            var transaction = Transaction.Current;

            if (transaction != null)
            {
                transactionLog.CreationTime = transaction.TransactionInformation.CreationTime;
            }
            _transactionLogs.Add(transactionLog);
        }


        public static bool FailedOnCommit { get; set; }
    }
}
