using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosqlTransactionManager.ResourceManager;

namespace NosqlTransactionManager
{
    public class MongoUserResource : IWriteResource<User>, IReadResource<User>
    {
        private MongoClient _mongoClient = null;
        private IMongoDatabase _mongoDataBase = null;

        private static string DBName = "2PCDB";


        public MongoUserResource()
        {
            _mongoClient = new MongoClient();
            _mongoDataBase = _mongoClient.GetDatabase(DBName);
        }

        public User Create(User user)
        {
            var collection = _mongoDataBase.GetCollection<User>("user");
            collection.InsertOneAsync(user);
            return user;
        }

        public User Update(User user)
        {
            var collection = _mongoDataBase.GetCollection<User>("user");
            collection.ReplaceOneAsync<User>(x => x.Id == user.Id, user);
            return user;
        }

        public bool Delete(User user)
        {
            var collection = _mongoDataBase.GetCollection<User>("user");
            return collection.DeleteOneAsync<User>(x => x.Id == user.Id).Result.DeletedCount > 0;
        }

        public User GetById(long id)
        {
            var collection = _mongoDataBase.GetCollection<User>("user");
            return collection.Find<User>(u => u.Id == id).SingleOrDefaultAsync().Result;
        }


        public long GetId(User user)
        {
            return user.Id;
        }

        public string ParticipantName
        {
            get
            {
                return "MongoDB";
            }          
        }
    }
}
