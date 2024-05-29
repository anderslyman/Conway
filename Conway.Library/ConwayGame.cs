namespace Conway.Library
{
    public interface IConwayGame
    {
        int GetHashCode();
        (bool, int) MoveNext();
        void Set(bool[,] data);
        bool[,] GetState();
    }

    // Borrowing from my past self. I wrote a conway game years ago that runs in LINQPad (included in the solution root).
    // I just updated it to detect end state and cycles.
    public class ConwayGame : IConwayGame
    {
        // Use more memory than a BitArray, but has faster access (which is desirable here)
        public bool[,] Cells;
        private bool[,] _newCells;
        private static Random _rnd;

        static ConwayGame()
        {
            _rnd = new Random();
        }

        public override int GetHashCode()
        {
            /* Thanks to the prime, the distribution of hashcodes should be fairly well distributed
             *   but the possibility of a collision remains (of course).
             * If this prime is good enough for R#, it's good enough for me.
             */
            var prime = 397;
            var length = Cells.GetLength(0);
            var height = Cells.GetLength(1);

            unchecked // Allows arithmetic overflow
            {
                int result = 0;

                for (int x = 0; x < length; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (Cells[x, y])
                        {
                            result = result * prime ^ x * length + y;
                        }
                    }
                }

                return result;
            }
        }

        public void Set(bool[,] data)
        {
            Cells = data;
            _newCells = new bool[Cells.GetLength(0), Cells.GetLength(1)];
        }

        public bool[,] GetState()
        {
            return Cells;
        }

        // Randomly generate game board
        public static bool[,] GenerateCells(int x, int y, bool useRandom = true, int modulus = 25)
        {
            var rows = new bool[x, y];

            if (useRandom)
            {
                for (int h = 0; h < x; h++)
                {
                    for (int i = 0; i < y; i++)
                    {
                        rows[h, i] = _rnd.Next(100) % modulus == 0;
                    }
                }
            }

            return rows;
        }

        public (bool, int) MoveNext()
        {
            var length = Cells.GetLength(0);
            var height = Cells.GetLength(1);
            var actions = new Action[length];
            var anyChanged = false;
            var prime = 397;
            var hashcode = 0;

            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var count = Adjacents(x, y).Count(a => a);
                    var alive = Cells[x, y];

                    /* From Wikipedia: https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life
                     * 1. Any live cell with fewer than two live neighbors dies, as if by underpopulation.
                     * 2. Any live cell with two or three live neighbors lives on to the next generation.
                     * 3. Any live cell with more than three live neighbors dies, as if by overpopulation.
                     * 4. Any dead cell with exactly three live neighbors becomes a live cell, as if by reproduction.
                     */

                    // Since the default state is false on the new board, we only need to check for rules 2 and 4
                    if (count == 3 && !alive || alive && (count == 2 || count == 3))
                    {
                        /* Have to set the state on a new game board, then copy it over.
                         * If the state is updated incrementally, each update affects the 
                         *   next and Conway's GOL rules don't apply as they should.
                         */
                        _newCells[x, y] = true;
                        anyChanged = true;
                        hashcode = hashcode * prime ^ x * length + y;
                    }
                }
            }

            Set(_newCells);

            return (anyChanged, hashcode);
        }

        private IEnumerable<bool> Adjacents(int x, int y)
        {
            var xLength = Cells.GetLength(0) - 1;
            var yLength = Cells.GetLength(1) - 1;

            if (x > 0) yield return Cells[x - 1, y];
            else yield return false;

            if (x < xLength) yield return Cells[x + 1, y];
            else yield return false;

            if (y > 0) yield return Cells[x, y - 1];
            else yield return false;

            if (y < yLength) yield return Cells[x, y + 1];
            else yield return false;

            if (x > 0 && y > 0) yield return Cells[x - 1, y - 1];
            else yield return false;

            if (x < xLength && y < yLength) yield return Cells[x + 1, y + 1];
            else yield return false;

            if (x > 0 && y < yLength) yield return Cells[x - 1, y + 1];
            else yield return false;

            if (x < xLength && y > 0) yield return Cells[x + 1, y - 1];
            else yield return false;
        }
    }
}
