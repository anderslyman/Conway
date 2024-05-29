using Conway.Library;

namespace Conway.Test
{
    [TestClass]
    public class ConwayGameTests
    {
        private readonly IConwayGame _conwayGame;

        public ConwayGameTests()
        {
            _conwayGame = new ConwayGame();
        }

        [TestMethod]
        public void MoveNext_UpdatesState()
        {
            var initialState = ConwayGame.GenerateCells(10, 10, true, 5);
            _conwayGame.Set(initialState);
            _conwayGame.MoveNext();
            var newState = _conwayGame.GetState();

            Assert.IsFalse(BoardStatesAreEqual(initialState, newState));
        }

        private bool BoardStatesAreEqual(bool[,] stateA, bool[,] stateB)
        {
            var widthA = stateA.GetLength(0);
            var heightA = stateA.GetLength(1);
            var widthB = stateB.GetLength(0);
            var heightB = stateB.GetLength(1);

            if (widthA != widthB || heightA != heightB)
            {
                return false;
            }

            for (int i = 0; i < widthA; i++)
            {
                for (int j = 0; j < heightA; j++)
                {
                    if (stateA[i, j] != stateB[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // TODO: test other methods
    }
}