using Conway.Library.Dtos.Base;

namespace Conway.Library.Dtos.Outgoing
{
    public class BoardDto : StandardColumnsDto
    {
        public int Id { get; set; }
        public bool[,] State { get; set; }
    }
}
