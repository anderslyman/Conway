using FluentMigrator;

namespace Conway.Persistence.Migrations
{
    [Migration(202405252200)]
    public class CreateLogSchema : Migration
    {
        public override void Up()
        {
            Create.Schema("logs");

            // General logging table
            // Default definition is from here: https://github.com/serilog-mssql/serilog-sinks-mssqlserver#table-definition
            Execute.Sql(@"
                CREATE TABLE [logs].[General] (
                   [Id] int IDENTITY(1,1) NOT NULL,
                   [Message] nvarchar(max) NULL,
                   [MessageTemplate] nvarchar(max) NULL,
                   [Level] nvarchar(128) NULL,
                   [TimeStamp] datetime NOT NULL,
                   [Exception] nvarchar(max) NULL,
                   [LogEvent] nvarchar(max) NULL

                   CONSTRAINT [PK_Logs_General] PRIMARY KEY CLUSTERED ([Id] ASC)
                );
            ");

            // Custom additions
            Alter.Table("General").InSchema("logs")
              .AddColumn("IPAddress").AsString(40).Nullable()
              .AddColumn("UserAgent").AsString(500).Nullable(); // TODO: Add username when auth is enabled

            // Query logging table
            // Default definition is from here: https://github.com/serilog-mssql/serilog-sinks-mssqlserver#table-definition
            Execute.Sql(@"
                CREATE TABLE [logs].[Query] (
                   [Id] int IDENTITY(1,1) NOT NULL,
                   [Message] nvarchar(max) NULL,
                   [Level] nvarchar(128) NULL,
                   [TimeStamp] datetime NOT NULL,
                   [Exception] nvarchar(max) NULL,
                   [LogEvent] nvarchar(max) NULL

                   CONSTRAINT [PK_Logs_Query] PRIMARY KEY CLUSTERED ([Id] ASC)
                );
            ");

            // Custom additions
            Alter.Table("Query").InSchema("logs")
              .AddColumn("IPAddress").AsString(40).Nullable()
              .AddColumn("UserAgent").AsString(500).Nullable(); // TODO: Add username when auth is enabled

            // Endpoint logging table
            // Default definition is from here: https://github.com/serilog-mssql/serilog-sinks-mssqlserver#table-definition
            Execute.Sql(@"
                CREATE TABLE [logs].[Endpoint] (
                   [Id] int IDENTITY(1,1) NOT NULL,
                   [Message] nvarchar(max) NULL,
                   [MessageTemplate] nvarchar(max) NULL,
                   [Level] nvarchar(128) NULL,
                   [TimeStamp] datetime NOT NULL,
                   [Exception] nvarchar(max) NULL,
                   [LogEvent] nvarchar(max) NULL

                   CONSTRAINT [PK_Logs_Endpoint] PRIMARY KEY CLUSTERED ([Id] ASC)
                );
            ");

            // Custom additions
            Alter.Table("Endpoint").InSchema("logs")
              .AddColumn("IPAddress").AsString(40).Nullable()
              .AddColumn("UserAgent").AsString(500).Nullable(); // TODO: Add username when auth is enabled;
        }

        public override void Down()
        {
            Delete.Table("Endpoint").InSchema("logs");
            Delete.Table("Query").InSchema("logs");
            Delete.Table("General").InSchema("logs");
            Delete.Schema("logs");
        }
    }
}
