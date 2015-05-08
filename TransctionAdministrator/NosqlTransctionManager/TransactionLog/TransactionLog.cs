using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NosqlTransactionManager
{
    public enum TransactionLogState { Begin, Ready, ReadyToCommit, Committed, Rollback, Completed, Aborted, FailedToRollback, FailedToCommit, InDoubt };

    public enum OperationType { Create, Update, Delete, None, CreateMany };

    public enum LockStatus { Release, Acquired };

    public class TransactionLog<T>
    {
        public long Id { get; set; }

        public long TransactionId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public OperationType OperationType { get; set; }

        public string Participant { get; set; }

        public T AfterEntity { get; set; }

        public T BeforeEntity { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TransactionLogState State { get; set; }

        public long LockEntityId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public LockStatus LockStatus { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime ExpiryTime { get; set; }

        public TransactionLog()
        {
        }

        //internal void CreateTransactionLog()
        //{
        //    try
        //    {
        //        var mongoClient = new MongoClient();
        //        var mongoDataBase = mongoClient.GetDatabase("2PCDB");
        //        var transactionCollection = mongoDataBase.GetCollection<TransactionLog<T>>("TransactionLog");
        //        transactionCollection.InsertOneAsync(this);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new TransactionLogException(Id, TransactionId, ex);
        //    }
        //}

        //internal void UpdateTransactionLog()
        //{
        //    try
        //    {
        //        var mongoClient = new MongoClient();
        //        var mongoDataBase = mongoClient.GetDatabase("2PCDB");
        //        var transactionCollection = mongoDataBase.GetCollection<TransactionLog<T>>("TransactionLog");
        //        transactionCollection.ReplaceOneAsync<TransactionLog<T>>(x => x.Id == this.Id, this);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new TransactionLogException(Id, TransactionId, ex);
        //    }
        //}
    }
}
