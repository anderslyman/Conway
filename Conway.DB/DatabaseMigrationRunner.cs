using FluentMigrator.Runner;
using FluentMigrator.Runner.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace Conway.Persistence
{
    public class DatabaseMigrationRunner
    {
        public static void MigrateDatabase(string defaultSchema, string connectionString, long? version = null)
        {
            // TODO: add code to inspect any executed sql for direct refences to the database by name
            try
            {
                var serviceProvider = CreateServices(defaultSchema, connectionString);

                using (var scope = serviceProvider.CreateScope())
                {
                    RunMigrations(scope.ServiceProvider, version);
                }
            }
            catch
            {
                // TODO: log an issue throw custom exception
                throw;
            }
        }

        public static void MigrateDatabaseDown(string defaultSchema, string connectionString, long version)
        {
            try
            {
                var serviceProvider = CreateServices(defaultSchema, connectionString);

                using (var scope = serviceProvider.CreateScope())
                {
                    RunMigrationsDown(scope.ServiceProvider, version);
                }
            }
            catch
            {
                // TODO: log an issue throw custom exception
                throw;
            }
        }

        public static void RollbackDatabase(string defaultSchema, string connectionString, int numberOfMigrationsToRollBack)
        {
            try
            {
                var serviceProvider = CreateServices(defaultSchema, connectionString);

                using (var scope = serviceProvider.CreateScope())
                {
                    Rollback(scope.ServiceProvider, numberOfMigrationsToRollBack);
                }
            }
            catch
            {
                // TODO: log an issue throw custom exception
                throw;
            }
        }

        private static IServiceProvider CreateServices(string defaultSchema, string connectionString)
        {
            var conventionSet = new DefaultConventionSet(defaultSchema, null);

            return new ServiceCollection()
              .AddSingleton<IConventionSet>(conventionSet)
              .AddFluentMigratorCore()
              .ConfigureRunner(rb => rb
                .AddSqlServer()
                .WithGlobalConnectionString(connectionString)
                .WithGlobalCommandTimeout(new TimeSpan(0, 5, 0)) // Default was 30 seconds
                .ScanIn(typeof(DatabaseMigrationRunner).Assembly).For.Migrations().For.EmbeddedResources())
              .AddLogging(lb => lb.AddFluentMigratorConsole())
              .BuildServiceProvider(false);
        }

        private static void RunMigrations(IServiceProvider serviceProvider, long? version)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            if (version.HasValue)
            {
                runner.MigrateUp(version.Value);
            }
            else
            {
                runner.MigrateUp();
            }
        }

        private static void RunMigrationsDown(IServiceProvider serviceProvider, long version)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateDown(version);
        }

        private static void Rollback(IServiceProvider serviceProvider, int numberOfMigrationsToRollBack)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            runner.Rollback(numberOfMigrationsToRollBack);
        }
    }
}
