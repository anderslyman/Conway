using AutoMapper;
using Conway.Library.Dtos.Outgoing;
using Conway.Library.Models;
using Conway.Library.Services;
using Microsoft.AspNetCore.Mvc;

namespace Conway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConwayController : ControllerBase
    {
        private readonly IConwayService _conwayService;
        private readonly IMapper _mapper;

        public ConwayController(IConwayService conwayService, IMapper mapper)
        {
            _conwayService = conwayService;
            _mapper = mapper;
        }

        /// <summary>
        /// Creates a new board with the given state.
        /// </summary>
        /// <param name="dto" example="{ &quot;stateJson&quot;: &quot;[[0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,0,0,0,1,0,0,0,0,0,1],[0,0,0,1,0,0,0,1,0,0,0,0,0,0,0,1,0,0,0,0,0,1,1,0,0],[0,1,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,1,0,0,1,0,0,0,0],[0,0,0,0,1,0,1,0,0,0,0,1,0,1,0,0,1,0,0,0,0,1,0,0,0],[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,1],[0,0,0,0,0,1,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0],[0,0,0,1,0,1,0,1,0,0,0,0,0,1,0,1,1,0,0,0,0,0,0,1,0],[0,0,1,1,0,1,0,0,0,0,0,1,0,1,0,0,0,0,1,0,0,1,0,0,0],[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,1,0],[0,0,0,1,1,0,1,1,0,0,0,0,0,0,1,1,0,0,0,0,0,0,1,1,0],[0,0,1,0,0,0,1,0,0,0,1,1,1,0,0,0,0,0,1,0,1,0,1,0,0],[1,0,1,0,0,0,0,0,0,1,0,0,0,1,0,1,0,1,0,0,0,1,0,0,0],[0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,0],[0,1,0,0,0,1,1,1,0,1,0,1,0,1,1,0,1,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1,0,0,1,1,0,0],[0,0,0,0,0,0,0,1,0,0,0,1,0,0,0,0,0,0,0,0,1,0,0,0,0],[0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,1,1,0,0,0,0],[0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0,1,1,0,0],[1,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,0,0,0],[1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0],[1,0,0,1,0,0,0,0,0,1,0,0,0,0,1,1,1,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,1,0,0,0,0],[0,0,0,0,0,1,0,0,0,0,0,0,0,1,0,0,0,1,0,0,0,0,0,0,0],[0,1,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,0,0],[0,0,0,0,0,0,1,1,0,0,0,1,1,0,1,0,1,0,0,0,0,0,0,0,0]]&quot; }"></param>
        /// <returns></returns>
        [HttpPost("Create")]
        public async Task<int> CreateBoard([FromBody] CreateBoardDto dto)
        {
            return await _conwayService.CreateBoard(dto.StateJson);
        }

        /// <summary>
        /// Retrieves the specified board.
        /// </summary>
        /// <param name="boardId" example="1">The board Id.</param>
        /// <returns></returns>
        [HttpGet("Get")]
        public async Task<BoardDto> GetBoard(int boardId)
        {
            /* Separation of models and dtos allows us to pick and choose which properties to expose to the client.
             * e.g. The models should match what's in the database, while the dtos should match what the client expects.
             * Automapper is a helpful library for that, cuts out a lot of boilerplate code.
             */
            return _mapper.Map<BoardModel, BoardDto>(await _conwayService.GetBoard(boardId));
        }

        /// <summary>
        /// Retrieves the next board state.
        /// </summary>
        /// <param name="boardId" example="1">The board Id.</param>
        /// <returns></returns>
        [HttpGet("GetNextState")]
        [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)] // Cache for 24 hours
        public async Task<BoardStateAfterIterationsDto> GetNextBoardState(int boardId)
        {
            return await _conwayService.GetNextBoardState(boardId);
        }

        /// <summary>
        /// Retrieves the board state after a specified number of iterations.
        /// </summary>
        /// <param name="boardId" example="1">The board Id.</param>
        /// <param name="iterations" example="10"></param>
        /// <returns></returns>
        [HttpGet("GetStateAfterIterations")]
        [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)] // Cache for 24 hours
        public async Task<BoardStateAfterIterationsDto> GetBoardStateAfterIterations(int boardId, int iterations)
        {
            return await _conwayService.GetBoardStateAfterIterations(boardId, iterations);
        }

        /// <summary>
        /// Attempts to find the final state of the board after a specified number of iterations.
        /// </summary>
        /// <param name="boardId" example="1">The board Id.</param>
        /// <param name="maxIterations" example="900">The maximum number of iterations to process when trying to find the end state.</param>
        /// <returns></returns>
        /// <response code="200">Returns the end state</response>
        /// <response code="500">If the final state was not reached after <c>maxIterations</c></response>
        [HttpGet("GetFinalState")]
        [ResponseCache(Duration = 60 * 60 * 24, Location = ResponseCacheLocation.Any)] // Cache for 24 hours
        public async Task<BoardStateAfterIterationsDto> GetFinalBoardState(int boardId, int maxIterations)
        {
            return await _conwayService.GetFinalBoardState(boardId, maxIterations);
        }
    }
}
