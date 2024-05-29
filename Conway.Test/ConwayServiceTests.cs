using Conway.Library;
using Conway.Library.Services;
using Moq;

namespace Conway.Test
{
    [TestClass]
    public class ConwayServiceTests
    {
        private readonly IConwayService _conwayService;
        
        public ConwayServiceTests()
        {
            var databaseServiceMock = new Mock<IDatabaseService>();
            databaseServiceMock.Setup(d => d.GetFirstOrDefaultAsync<int>(It.IsAny<string>(), It.IsAny<object>())).Returns(Task.FromResult(1));
            var conwayGameMock = new Mock<IConwayGame>();

            _conwayService = new ConwayService(databaseServiceMock.Object, conwayGameMock.Object);
        }

        [TestMethod]
        public async Task CreateBoard_ReturnsId()
        {
            var id = await _conwayService.CreateBoard("[[0,0],[0,0]]");

            Assert.AreEqual(1, id);
        }

        // TODO: test other methods
    }
}