using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using NosqlTransactionManager.ResourceManager;
using NosqlTransactionManager.TransactionLog;

namespace NosqlTransactionManager
{
    public class TransactionExecutor<T> : IEnlistmentNotification
    {

        private List<TransactionLog<T>> _transactionLogs;
        private IWriteResource<T> _resource = null;
        private long _transactionId;
        private ILockManager _lockManager;
        private List<TransactionLogException> _exceptionsList;
        private ITransactionLogManager _logManager;
        private IReadResource<T> _readResource;


        public TransactionExecutor(long transactionId, IWriteResource<T> resource, IReadResource<T> readResource, List<TransactionLogException> exceptionsList)
        {
            _logManager = new TransactionLogManager();
            _transactionId = transactionId;
            _resource = resource;
            _lockManager = new LockManager();
            _exceptionsList = exceptionsList;
            _transactionLogs = new List<TransactionLog<T>>();
            _readResource = readResource;

            if (Transaction.Current != null)
            {
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);

            }
        }

        #region supported commands

        internal void Create(T t)
        {
            FillTransactionLog(t, OperationType.Create);
        }

        internal void CreateMany(List<T> s)
        {
            if (s != null)
            {
                foreach (T t in s)
                {
                    FillTransactionLog(t, OperationType.Create);
                }
            }
        }

        internal void Update(T t)
        {
            FillTransactionLog(t, OperationType.Update);
        }

        internal void UpdateMany(List<T> s)
        {
            if (s != null)
            {
                foreach (T t in s)
                {
                    FillTransactionLog(t, OperationType.Update);
                }
            }
        }

        internal void Delete(T t)
        {
            FillTransactionLog(t, OperationType.Delete);
        }

        internal void DeleteMany(List<T> s)
        {
            if (s != null)
            {
                foreach (T t in s)
                {
                    FillTransactionLog(t, OperationType.Delete);
                }
            }
        }

        #endregion

        #region prepared phase 1

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            Debug.WriteLine("iN MONGO PREPARE");
            try
            {
                foreach (var transactionLog in _transactionLogs)
                {
                    switch (transactionLog.OperationType)
                    {
                        case OperationType.Create:
                            PreparedCreateTransactionLog(transactionLog);
                            break;
                        case OperationType.Update:
                            PreparedUpdateTransactionLog(transactionLog);
                            break;
                        case OperationType.Delete:
                            PreparedDeleteTransactionLog(transactionLog);
                            break;
                    }
                }

                preparingEnlistment.Prepared();
            }
            catch (Exception ex)
            {
                RollbackReadyToCommitTransactions();
                preparingEnlistment.ForceRollback(ex);
            }
        }

        private void RollbackReadyToCommitTransactions()
        {
            foreach (var transactionLog in _transactionLogs)
            {
                if (transactionLog.State == TransactionLogState.ReadyToCommit)
                {
                    try
                    {
                        switch (transactionLog.OperationType)
                        {
                            case OperationType.Create:
                                RollbackCreateTransactionLog(transactionLog);
                                break;
                            case OperationType.Update:
                                RollbackUpdateTransactionLog(transactionLog);
                                break;
                            case OperationType.Delete:
                                RollbackDeleteTransactionLog(transactionLog);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_exceptionsList != null)
                            _exceptionsList.Add(new TransactionLogException(transactionLog.Id, _transactionId, ex));
                        try
                        {
                            transactionLog.State = TransactionLogState.FailedToRollback;
                            _logManager.UpdateTransactionLog<T>(transactionLog);
                        }
                        catch (Exception e)
                        {
                            if (_exceptionsList != null)
                                _exceptionsList.Add(new TransactionLogException(transactionLog.Id, _transactionId, e));
                        }
                    }
                }
            }
        }

        private void PreparedDeleteTransactionLog(TransactionLog<T> transactionLog)
        {
            var beforeEntity = _readResource.GetById(transactionLog.LockEntityId);
            _lockManager.AcquireLock(transactionLog.LockEntityId, _resource.ParticipantName);
            _resource.Delete(transactionLog.AfterEntity);
            UpdateTransactionLog(transactionLog, beforeEntity);
        }

        private void PreparedCreateTransactionLog(TransactionLog<T> transactionLog)
        {
            _resource.Create(transactionLog.AfterEntity);

            transactionLog.State = TransactionLogState.ReadyToCommit;
            _logManager.UpdateTransactionLog<T>(transactionLog);
        }

        private void PreparedUpdateTransactionLog(TransactionLog<T> transactionLog)
        {

            var beforeEntity = _readResource.GetById(transactionLog.LockEntityId);
            _lockManager.AcquireLock(transactionLog.LockEntityId, _resource.ParticipantName);

            _resource.Update(transactionLog.AfterEntity);

            UpdateTransactionLog(transactionLog, beforeEntity);
        }

        #endregion

        #region Commit phase 2

        public void Commit(Enlistment enlistment)
        {

            Debug.WriteLine("iN MONGO Commit");

            foreach (var transactionLog in _transactionLogs)
            {
                try
                {

                    if (transactionLog.State == TransactionLogState.ReadyToCommit)
                    {
                        ThrowException(_resource.ParticipantName);

                        switch (transactionLog.OperationType)
                        {
                            case OperationType.Create:
                                CommitCreateTransactionLog(transactionLog);
                                break;
                            case OperationType.Update:
                                CommitUpdateOrDeleteTransactionLog(transactionLog);
                                break;
                            case OperationType.Delete:
                                CommitUpdateOrDeleteTransactionLog(transactionLog);
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (_exceptionsList != null)
                        _exceptionsList.Add(new TransactionLogException(transactionLog.Id, _transactionId, e));

                    try
                    {
                        transactionLog.State = TransactionLogState.FailedToCommit;
                        _logManager.UpdateTransactionLog<T>(transactionLog);
                    }
                    catch (Exception ex)
                    {
                        if (_exceptionsList != null)
                            _exceptionsList.Add(new TransactionLogException(transactionLog.Id, _transactionId, ex));
                    }
                }
            }


            enlistment.Done();
        }

        public static void ThrowException(string participantName)
        {
            if (participantName == VexiereConfiguration.GetParticipantName())
                throw new Exception("phase tow failed!");
        }

        private void CommitUpdateOrDeleteTransactionLog(TransactionLog<T> transactionLog)
        {
            _lockManager.ReleaseLock(transactionLog.LockEntityId, _resource.ParticipantName);

            transactionLog.State = TransactionLogState.Committed;
            transactionLog.LockStatus = LockStatus.Release;
            _logManager.UpdateTransactionLog<T>(transactionLog);
        }

        private void CommitCreateTransactionLog(TransactionLog<T> transactionLog)
        {
            transactionLog.State = TransactionLogState.Committed;
            _logManager.UpdateTransactionLog<T>(transactionLog);
        }

        #endregion

        public void InDoubt(Enlistment enlistment)
        {

            enlistment.Done();
        }

        #region Rollback phase 2

        public void Rollback(Enlistment enlistment)
        {
            Debug.WriteLine("iN MONGO Rollback");

            RollbackReadyToCommitTransactions();

            enlistment.Done();
        }

        private void RollbackDeleteTransactionLog(TransactionLog<T> transactionLog)
        {
            _resource.Create(transactionLog.BeforeEntity);

            _lockManager.ReleaseLock(transactionLog.LockEntityId, _resource.ParticipantName);

            transactionLog.State = TransactionLogState.Rollback;
            transactionLog.LockStatus = LockStatus.Release;
            _logManager.UpdateTransactionLog<T>(transactionLog);
        }

        private void RollbackUpdateTransactionLog(TransactionLog<T> transactionLog)
        {
            _resource.Update(transactionLog.BeforeEntity);

            _lockManager.ReleaseLock(transactionLog.LockEntityId, _resource.ParticipantName);

            transactionLog.State = TransactionLogState.Rollback;
            transactionLog.LockStatus = LockStatus.Release;
            _logManager.UpdateTransactionLog<T>(transactionLog);

        }

        private void RollbackCreateTransactionLog(TransactionLog<T> transactionLog)
        {
            _resource.Delete(transactionLog.AfterEntity);

            transactionLog.State = TransactionLogState.Rollback;
            _logManager.UpdateTransactionLog<T>(transactionLog);
        }

        #endregion

        #region transaction log and lock

        private void FillTransactionLog(T t, OperationType operationType)
        {
            var transactionLog = new TransactionLog<T>();
            transactionLog.Id = UniqueIdGenerator.GetNextId();
            transactionLog.TransactionId = _transactionId;
            transactionLog.OperationType = operationType;

            transactionLog.State = TransactionLogState.Ready;
            transactionLog.Participant = _resource.ParticipantName;
            transactionLog.CreationTime = DateTime.UtcNow;

            transactionLog.BeforeEntity = default(T);
            transactionLog.AfterEntity = t;

            transactionLog.LockEntityId = _readResource.GetId(t);
            transactionLog.LockStatus = LockStatus.Release;
            transactionLog.ExpiryTime = transactionLog.CreationTime.Add(TransactionManager.DefaultTimeout);

            _logManager.CreateTransactionLog<T>(transactionLog);

            _transactionLogs.Add(transactionLog);
        }



        private void UpdateTransactionLog(TransactionLog<T> transactionLog, T beforeEntity)
        {
            transactionLog.BeforeEntity = beforeEntity;
            transactionLog.LockStatus = LockStatus.Acquired;
            transactionLog.State = TransactionLogState.ReadyToCommit;
            _logManager.UpdateTransactionLog<T>(transactionLog);
        }

        #endregion

    }
}
