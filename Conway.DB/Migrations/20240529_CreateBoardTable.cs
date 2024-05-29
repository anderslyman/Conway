using FluentMigrator;

namespace Conway.Persistence.Migrations
{
    [Migration(202405291130)]
    public class CreateBoardTable : Migration
    {
        public override void Up()
        {
            Create.Table("Board")
              .WithColumn("BoardID").AsInt64().PrimaryKey("PK_Board").Identity()
              .WithColumn("StateJSON").AsString(int.MaxValue).NotNullable()
              .WithColumn("LastUpdated").AsDateTimeOffset().NotNullable();

            /* NOTE: StateJSON is a JSON representation of the board state. A byte array or blob would 
             *   also work, but JSON allows a higher level of debuggability/visibility inside SSMS.
             */
        }

        public override void Down()
        {
            Delete.Table("Board");
        }
    }
}
