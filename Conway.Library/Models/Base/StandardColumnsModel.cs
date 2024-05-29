using Conway.Library.Attributes;

namespace Conway.Library.Models.Base
{
    public class StandardColumnsModel
    {
        // TODO: add OwnerId, CreatedById, and LastUpdatedById once auth is implemented

        [Column("LastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
