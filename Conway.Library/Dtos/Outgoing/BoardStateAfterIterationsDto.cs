namespace Conway.Library.Dtos.Outgoing
{
    public class BoardStateAfterIterationsDto
    {
        public int Id { get; set; }
        public bool[,] State { get; set; }
        public bool IsEndState { get; set; }
        public bool IsLooping { get; set; }
        public int CycleStart { get; set; }
        public int CycleLength { get; set; }
        public int Iterations { get; set; }
    }
}
