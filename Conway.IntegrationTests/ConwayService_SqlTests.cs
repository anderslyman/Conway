using Conway.Library;
using Conway.Library.Services;

namespace Conway.IntegrationTest.Database
{
    [TestClass]
    public class ConwayService_SqlTests
    {
        private readonly ConwayService _conwayService;

        public ConwayService_SqlTests()
        {
            /* TODO: Get the normal connection string and replace the database name with a test database.
             *   Wrap tests in a transaction that is rolled back at the end to keep the db clean.
             * It's important to use a real db so that the SQL in the tests are compiled and exercised.
             */
            var testConnectionString = "TODO"; 
            var databaseService = new DatabaseService(testConnectionString);
            var conwayGame = new ConwayGame();
            _conwayService = new ConwayService(databaseService, conwayGame);
        }

        [TestMethod]
        public async Task GetBoard()
        {
            var board = _conwayService.GetBoard(1);

            Assert.AreEqual(1, board.Id);
        }

        // TODO: test other methods
    }
}
