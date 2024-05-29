namespace Conway.Library.Dtos.Base
{
    public class StandardColumnsDto
    {
        // TODO: add OwnerId, CreatedById, and LastUpdatedById once auth is implemented
        public DateTimeOffset LastUpdated { get; set; }
    }
}
