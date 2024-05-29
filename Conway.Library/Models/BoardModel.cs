using Conway.Library.Attributes;
using Conway.Library.Models.Base;
using Newtonsoft.Json;

namespace Conway.Library.Models
{
    public class BoardModel : StandardColumnsModel
    {
        [Slapper.AutoMapper.Id]
        [Column("BoardID")]
        public int Id { get; set; }

        [Column("StateJSON")]
        public string? StateJson { get; set; }

        [NotMapped]
        public bool[,]? State => JsonConvert.DeserializeObject<bool[,]>(StateJson ?? "[]");
    }
}
