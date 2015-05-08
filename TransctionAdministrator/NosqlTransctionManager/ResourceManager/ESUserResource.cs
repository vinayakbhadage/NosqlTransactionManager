using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosqlTransactionManager.ResourceManager;

namespace NosqlTransactionManager
{
    public class ESUserResource : IWriteResource<User>, IReadResource<User>
    {
        private ElasticClient _elasticClient;

        public ESUserResource()
        {
            var node = new Uri(VexiereConfiguration.ElasticSearchUri);

            var settings = new ConnectionSettings(
                node,
                defaultIndex: "twopc"
            );

            _elasticClient = new ElasticClient(settings);
        }

        public string ParticipantName
        {
            get { return "ES"; ; }
        }

        public User Create(User user)
        {
            ThrowException();
            var entityResponse = _elasticClient.Index<User>(user);

            return user;
        }

        public User Update(User user)
        {
            ThrowException();
            var entityResponse = _elasticClient.Index<User>(user);

            return user;
        }

        public bool Delete(User user)
        {
            ThrowException();
            var entityResponse = _elasticClient.Delete<User>(user.Id);
            return entityResponse.IsValid;
        }

        public User GetById(long id)
        {
            ThrowException();
            return _elasticClient.Get<User>(u => u.Id(id)).Source;
        }

        public long GetId(User t)
        {
            return t.Id;
        }

        public static void ThrowException() {
            if (DateTime.Now == new DateTime(2012, 12, 21))
                throw new Exception("Boom!");
        }
    }
}
