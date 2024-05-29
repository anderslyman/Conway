using Conway.Library.Dtos.Outgoing;
using Conway.Library.Exceptions;
using Conway.Library.Models;
using Newtonsoft.Json;

namespace Conway.Library.Services
{

    public interface IConwayService
    {
        Task<int> CreateBoard(string stateJson);
        Task<BoardModel> GetBoard(int boardId);
        Task<BoardStateAfterIterationsDto> GetNextBoardState(int boardId);
        Task<BoardStateAfterIterationsDto> GetBoardStateAfterIterations(int boardId, int iterations);
        Task<BoardStateAfterIterationsDto> GetFinalBoardState(int boardId, int maxIterations);
    }

    public class ConwayService : IConwayService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IConwayGame _conwayGame;

        public ConwayService(IDatabaseService databaseService, IConwayGame conwayGame)
        {
            _databaseService = databaseService;
            _conwayGame = conwayGame;
        }

        public async Task<BoardStateAfterIterationsDto> GetBoardStateAfterIterations(int boardId, int iterations)
        {
            return await GetBoardState(boardId, iterations);
        }

        public async Task<BoardStateAfterIterationsDto> GetFinalBoardState(int boardId, int maxIterations)
        {
            var result = await GetBoardState(boardId, maxIterations);

            if (!result.IsEndState && !result.IsLooping)
            {
                throw new EndStateNotReached($"End state not reached after {maxIterations} iterations.");
            }

            return result;
        }

        public async Task<BoardStateAfterIterationsDto> GetNextBoardState(int boardId)
        {
            return await GetBoardState(boardId, 1);
        }

        public async Task<int> CreateBoard(string stateJson)
        {
            const int maxBoardDimension = 100;
            bool[,]? boardState;

            try
            {
                boardState = JsonConvert.DeserializeObject<bool[,]>(stateJson);
            }
            catch
            {
                throw new ArgumentOutOfRangeException($"Please provide a state like the following: [[0,1],[0,1]]");
            }

            if (boardState == null)
            {
                throw new ArgumentNullException($"Please provide a state like the following: [[0,1],[0,1]]");
            }
            else if (boardState.GetLength(0) > maxBoardDimension || boardState.GetLength(1) > maxBoardDimension)
            {
                throw new ArgumentOutOfRangeException($"The board state cannot exceed 100 in either dimension.");
            }
            else if (boardState.GetLength(0) == 0 || boardState.GetLength(1) == 0)
            {
                throw new ArgumentOutOfRangeException($"The board state is empty.");
            }

            var id = await _databaseService.GetFirstOrDefaultAsync<int>($@"
                INSERT INTO [Board] (StateJSON,LastUpdated)
                VALUES (@StateJSON,GETUTCDATE());

                SELECT SCOPE_IDENTITY();
            ", new
            {
                StateJSON = stateJson
            });

            return id;
        }

        public async Task<BoardModel> GetBoard(int boardId)
        {
            return await _databaseService.GetFirstOrDefaultAsync<BoardModel>($@"
                SELECT {_databaseService.GetSelectStatementFromModel<BoardModel>("b")}
                FROM [Board] b WHERE b.BoardID = @BoardId",
                new { BoardId = boardId });
        }

        private async Task<BoardStateAfterIterationsDto> GetBoardState(int boardId, int iterations)
        {
            var board = await GetBoard(boardId);
            _conwayGame.Set(board.State);

            var result = new BoardStateAfterIterationsDto();
            var didBoardChange = true;
            var hashcode = _conwayGame.GetHashCode();
            var hashcodes = new List<int>();
            var hashcodesSet = new HashSet<int> { hashcode };

            for (int i = 0; i < iterations; i++)
            {
                (didBoardChange, hashcode) = _conwayGame.MoveNext();
                result.Iterations = i;

                if (!didBoardChange)
                {
                    result.IsEndState = true;
                    break;
                }

                if (hashcodesSet.Contains(hashcode))
                {
                    result.IsLooping = true;
                    result.CycleStart = hashcodes.IndexOf(hashcode);
                    result.CycleLength = hashcodes.Count - result.CycleStart;

                    break;
                }

                hashcodesSet.Add(hashcode);
                hashcodes.Add(hashcode);
            }

            result.State = _conwayGame.GetState();

            return result;
        }
    }
}
