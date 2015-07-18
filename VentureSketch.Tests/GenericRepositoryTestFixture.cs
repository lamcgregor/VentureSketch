using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using VentureSketch.Data;
using VentureSketch.Models;

namespace VentureSketch.Tests
{
    [TestFixture]
    public class GenericRepositoryTestFixture
    {
        private string _connectionString = ConfigurationManager.ConnectionStrings["VentureSketchConnectionString"].ConnectionString;
        private IRepository<Qualification> _qualificationRepository;
        private IRepository<User> _userRepository;
        private IRepository<ActivityType> _activityTypeRepository;

        [SetUp]
        public void Setup()
        {
            _qualificationRepository = new GenericRepository<Qualification>(_connectionString);
            _userRepository = new GenericRepository<User>(_connectionString);
            _activityTypeRepository = new GenericRepository<ActivityType>(_connectionString);
        }

        [TearDown]
        public void TearDown()
        {
            _qualificationRepository.Dispose();
            _userRepository.Dispose();
        }
        
        [Test]
        public void ListTest()
        {
            List<Qualification> allQualifications = _qualificationRepository.List().ToList();

            Assert.IsNotNull(allQualifications);
            Assert.Greater(allQualifications.Count, 0);

            //ensure all activity types are eager-loaded
            foreach (Qualification qualification in allQualifications)
            {
                Assert.IsNotNull(qualification.ActivityType);
            }

            Qualification qualificationToTest = allQualifications[0];
            
            Assert.AreEqual(qualificationToTest.ActivityTypeId, 3);
            Assert.AreEqual(qualificationToTest.Id, 1);
            Assert.AreEqual(qualificationToTest.Name, "ISA/SGB Level 1 Coach");
            Assert.IsNull(qualificationToTest.Notes);
            Assert.AreEqual(qualificationToTest.Rank, 2);

            qualificationToTest = allQualifications[3];

            Assert.AreEqual(qualificationToTest.ActivityTypeId, 3);
            Assert.AreEqual(qualificationToTest.Id, 4);
            Assert.AreEqual(qualificationToTest.Name, "BSA Level 2 Coach");
            Assert.AreEqual(qualificationToTest.Notes, "defunct");
            Assert.AreEqual(qualificationToTest.Rank, 3);

        }

        [Test]
        public void FindTest()
        {
            Qualification qualification = _qualificationRepository.Find(10);
            Assert.IsNotNull(qualification.ActivityType);
            Assert.AreEqual(qualification.ActivityType.Id, 8);
            Assert.AreEqual(qualification.ActivityType.Name, "Kite Sports");
            Assert.AreEqual(qualification.ActivityTypeId, 8);
            Assert.AreEqual(qualification.Id, 10);
            Assert.AreEqual(qualification.Name, "BKSA Powerkite");
            Assert.IsNull(qualification.Notes);
            Assert.AreEqual(qualification.Rank, 1);

            qualification = _qualificationRepository.Find(int.MaxValue);
            Assert.IsNull(qualification);

            User user = _userRepository.Find(3);

            Assert.IsNotNull(user);
            Assert.AreEqual(user.Email, "besimatalay@hotmail.com");
            Assert.AreEqual(user.Id, 3);
            Assert.AreEqual(user.Name, "Besim Atalay");
            Assert.AreEqual(user.PasswordHash, "12345");
            Assert.IsNotNull(user.UserQualifications);
            Assert.AreEqual(user.UserQualifications.Count, 2);
            
            Assert.AreEqual(user.UserQualifications[0].ActivityTypeId, 3);
            Assert.AreEqual(user.UserQualifications[0].Id, 4);
            Assert.AreEqual(user.UserQualifications[0].Name, "BSA Level 2 Coach");
            Assert.AreEqual(user.UserQualifications[0].Notes, "defunct");
            Assert.AreEqual(user.UserQualifications[0].Rank, 3); Assert.AreEqual(user.UserQualifications[0].ActivityTypeId, 3);
            
            Assert.AreEqual(user.UserQualifications[1].ActivityTypeId, 3); 
            Assert.AreEqual(user.UserQualifications[1].Id, 6);
            Assert.AreEqual(user.UserQualifications[1].Name, "BSA Level 4 Coach");
            Assert.AreEqual(user.UserQualifications[1].Notes, "defunct");
            Assert.AreEqual(user.UserQualifications[1].Rank, 1);

            user = _userRepository.Find(int.MaxValue);
            Assert.IsNull(user);
        }

        [Test]
        public void AddOrUpdateTest()
        {
            IEnumerable<Qualification> allQualifications = _qualificationRepository.List();
            int qualificationCount = allQualifications.Count();

            Qualification qualification = new Qualification() { ActivityTypeId = 6, Name = "Test Qualification", Notes = "This is a test Qualification", Rank = 2 };
            _qualificationRepository.AddOrUpdate(qualification);

            Assert.Greater(qualification.Id, 0);
            Assert.IsNotNull(qualification.ActivityType);
            Assert.AreEqual(qualification.ActivityType.Id, 6);
            Assert.AreEqual(qualification.ActivityType.Name, "Sailing");
            Assert.AreEqual(qualification.Name, "Test Qualification");
            Assert.AreEqual(qualification.Notes, "This is a test Qualification");
            Assert.AreEqual(qualification.Rank, 2);

            int qualificationId = qualification.Id;
            qualification = _qualificationRepository.Find(qualificationId);
            Assert.IsNotNull(qualification.ActivityType);
            Assert.AreEqual(qualification.ActivityType.Id, 6);
            Assert.AreEqual(qualification.ActivityType.Name, "Sailing");
            Assert.AreEqual(qualification.Name, "Test Qualification");
            Assert.AreEqual(qualification.Notes, "This is a test Qualification");
            Assert.AreEqual(qualification.Rank, 2);

            allQualifications = _qualificationRepository.List();
            Assert.AreEqual(allQualifications.Count(), qualificationCount + 1);
            
            _qualificationRepository.Delete(qualification);

            allQualifications = _qualificationRepository.List();
            Assert.AreEqual(allQualifications.Count(), qualificationCount);

            User user = new User() { Email = "testuser@test.com", Name = "Test User", PasswordHash = "12345", UserTypeId = 4 };
            
            user.UserQualifications = new List<Qualification>();
            user.UserQualifications.Add(allQualifications.ElementAt<Qualification>(0));
            user.UserQualifications.Add(new Qualification() { ActivityTypeId = 3, Name = "Test Qualification" });

            //Add
            _userRepository.AddOrUpdate(user);
            Assert.Greater(user.Id, 0);
            Assert.IsNotNull(user.UserQualifications);
            Assert.Greater(user.UserQualifications.Count, 0);

            //Update
            user.Name = "Test User Changed";
            _userRepository.AddOrUpdate(user);

            user = _userRepository.Find(user.Id);
            Assert.AreEqual(user.Name, "Test User Changed");
        }

        [Test]
        public void DeleteTest()
        {
            List<ActivityType> allActivityTypes = _activityTypeRepository.List().ToList();
            int activityTypeCount = allActivityTypes.Count;

            ActivityType activityType = new ActivityType() { Name = "Test Activity" };
            
            _activityTypeRepository.AddOrUpdate(activityType);
            
            Assert.Greater(activityType.Id, 0);
            int activityTypeId = activityType.Id;

            activityType = _activityTypeRepository.Find(activityTypeId);
            
            Assert.IsNotNull(activityType);
            Assert.AreEqual(activityType.Name, "Test Activity");

            allActivityTypes = _activityTypeRepository.List().ToList();
            Assert.AreEqual(allActivityTypes.Count, activityTypeCount + 1);

            //Delete
            _activityTypeRepository.Delete(activityType);
            activityType = _activityTypeRepository.Find(activityTypeId);
            Assert.IsNull(activityType);

            allActivityTypes = _activityTypeRepository.List().ToList();
            Assert.AreEqual(allActivityTypes.Count, activityTypeCount);
        }

    }
}
