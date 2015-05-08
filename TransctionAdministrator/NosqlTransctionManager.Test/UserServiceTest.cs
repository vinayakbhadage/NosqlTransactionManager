using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using Microsoft.QualityTools.Testing.Fakes;
using NosqlTransactionManager.Fakes;
using System.Fakes;

namespace NosqlTransactionManager.Test
{
    [TestClass]
    public class UserServiceTest
    {

        [TestMethod]
        public void Create_User_Successfully()
        {
            var userService = new UserService();

            User user = new User();
            user.FirstName = "john";
            user.LastName = "doe";

            user = userService.Create(user);

            Thread.Sleep(5000);
            Assert.IsNotNull(user.Id);
        }

        [TestMethod]
        public void CreateUser_ElasticSearch_Is_Down_In_Phase_1_RollbackUserCreation()
        {

            var userService = new UserService();

            User user = new User();
            user.FirstName = "john";
            user.LastName = "doe";
            using (var context = ShimsContext.Create())
            {
                ShimDateTime.NowGet = () => new DateTime(2012, 12, 21);

                user = userService.Create(user);

                Thread.Sleep(5000);
                Assert.IsNull(user);
            }
        }


        [TestMethod]
        public void CreateUser_ElasticSearch_Is_Down_In_Phase_2_RollbackUserCreation()
        {

            var userService = new UserService();


            User user = new User();
            user.FirstName = "john";
            user.LastName = "doe";
            using (var context = ShimsContext.Create())
            {
                ShimVexiereConfiguration.GetParticipantName = () =>  "ES";
                user = userService.Create(user);

                Thread.Sleep(5000);
                Assert.IsNull(user);
            }

        }


        [TestMethod]
        public void UpdateUser_2PC_Successfully()
        {

            var userService = new UserService();
            User user = new User();
            user.FirstName = "john";
            user.LastName = "doe";

            user = userService.Create(user);

            user.LastName = "down";
            user = userService.Update(user);
            Thread.Sleep(5000);
            Assert.IsNotNull(user);
        }



        [TestMethod]
        public void UpdateUser_Phase_1_ElasticSearch_Is_Down_Rollback()
        {

            var userService = new UserService();
            User user = new User();
            user.FirstName = "john";
            user.LastName = "doe";

            user = userService.Create(user);

            using (var context = ShimsContext.Create())
            {
                ShimVexiereConfiguration.GetParticipantName = () => "ES";
                user = userService.Update(user);

                Thread.Sleep(5000);
                Assert.IsNull(user);
            }
        }


        [TestMethod]
        public void DeleteUser_2PC_Successfully()
        {

            var userService = new UserService();
            User user = new User();
            user.FirstName = "john";
            user.LastName = "doe";

            user = userService.Create(user);

            user.LastName = "down";
            user = userService.Delete(user);
            Thread.Sleep(5000);
            Assert.IsNotNull(user);
        }

        [TestMethod]
        public void DeleteUser_Phase_1_ElasticSearch_Down_Rollback_Is_Expected()
        {

            var userService = new UserService();
            User user = new User();
            user.FirstName = "john";
            user.LastName = "doe";

            user = userService.Create(user);

            using (var context = ShimsContext.Create())
            {
                ShimVexiereConfiguration.GetParticipantName = () => "ES";
                user = userService.Delete(user);

                Thread.Sleep(5000);
                Assert.IsNull(user);
            }
        }
    }
}
