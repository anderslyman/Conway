using Anotar.Serilog;
using Conway.Library;
using Conway.Library.Configuration;
using Conway.Library.Services;
using Conway.Library.Settings;
using Conway.Persistence;
using Destructurama;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.ObjectModel;
using System.Data;
using System.Net;
using System.Reflection;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Having these available in a global object is handy for middleware and other things that may not have access to DI.
        GlobalHosting.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        GlobalHosting.AppSettings = builder.Configuration.GetSection("GlobalAppSettings").Get<StaticAppSettings>();

        builder.Host.UseSerilog((context, configuration) =>
        {
            var isEndpointLogging = Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware");
            var isDatabaseLogging = Matching.FromSource("Conway.Library.Services.DatabaseService");
            var settings = context.Configuration.GetSection("GlobalAppSettings").Get<StaticAppSettings>();
            var enabledLoggers = settings.Logging;

            configuration
                .Destructure.JsonNetTypes()
                .ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                // Regardless of log level specified in the config, logging level should always be verbose for these areas
                .MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Verbose)
                .MinimumLevel.Override("Conway.Library.Services.DatabaseService", LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .WriteTo.Conditional( // Console
                  ev => enabledLoggers["Console"] && !isDatabaseLogging(ev),
                  wt => wt.Console()
                )
                .WriteTo.Conditional( // Text file - check for generated logs in the Conway.Api project root
                  ev => enabledLoggers["File"] && !isDatabaseLogging(ev),
                  wt => wt.File(path: "log.txt", rollingInterval: RollingInterval.Day)
                )
                .WriteTo.Conditional( // logs.General db table
                  ev => enabledLoggers["General"] && !isEndpointLogging(ev) && !isDatabaseLogging(ev),
                  wt => wt.MSSqlServer(
                    connectionString: GlobalHosting.ConnectionString,
                    sinkOptions: new MSSqlServerSinkOptions { AutoCreateSqlTable = false, SchemaName = "logs", TableName = "General" },
                    columnOptions: GetColumnOptions(true)
                ))

                /* If audit logging is needed, that could be configured below. 
                 * 
                 * Right now we write logs in batches, which result in better backend performance but writes aren't guaranteed. 
                 *   (in reality, it'd be rare that a write fails)
                 * 
                 *  Serilog offers audit logs, which would ensure all audit logging is logged immediately, at the expense of hitting the db far more often.
                 *  Recommended for production environments that care about auditing.
                 */

                .WriteTo.Conditional( // logs.Endpoint db table
                  ev => enabledLoggers["Endpoint"] && isEndpointLogging(ev),
                  wt => wt.MSSqlServer(
                    connectionString: GlobalHosting.ConnectionString,
                    sinkOptions: new MSSqlServerSinkOptions { AutoCreateSqlTable = false, SchemaName = "logs", TableName = "Endpoint" },
                    columnOptions: GetColumnOptions(true)
                ))
                .WriteTo.Conditional( // logs.Query db table
                  ev => enabledLoggers["Query"] && isDatabaseLogging(ev),
                  wt => wt.MSSqlServer(
                    connectionString: GlobalHosting.ConnectionString,
                    sinkOptions: new MSSqlServerSinkOptions { AutoCreateSqlTable = false, SchemaName = "logs", TableName = "Query" },
                    columnOptions: GetColumnOptions(true, co => co.Store.Remove(StandardColumn.MessageTemplate))
                ));
        });

        /* Separation of models and dtos allows us to pick and choose which properties to expose to the client.
         * e.g. The models should match what's in the database, while the dtos should match what the client expects.
         * Automapper is a helpful library for that, cuts out a lot of boilerplate code.
         */
        builder.Services.AddSingleton(AutomapperSetup.SetupMappings().CreateMapper());

        /* TODO: write a dispatch proxy that can measure performance when requested.
         *  This is handy in production scenarios where you want to measure the performance of all the underlying service calls.
         *  Sometimes replicating a performance issue is really difficult in a developer env, so appending an "Audit: true" header
         *    to a call is a handy way to generate a performance report.
         *  e.g. this could change to AddScopedWithTelemetry or AddScopedWithAudit or something
         */

        /* Entity framework is a great option. I find I prefer a bit more control over the SQL queries.
         * Using Dapper, the associated librar Slapper.Automapper, and some helper functions takes care of
         *   a lot of the downsides with maintaining hand-written SQL.
         * We can't prevent SQL Injection. However, a custom extension for Visual Studio could inspect the code 
         *   and perhaps detect any interpolation (which compiles out) in strings determined to contain 
         *   SQL - Microsoft's script dom package is great for parsing SQL into an AST.
         * Additionally, we can easily test the SQL queries in a test environment - we can check that they compile,
         *   and we can exercise them in some simple ways to ensure they return/manipulate data correctly.
         */
        builder.Services.AddScoped<IDatabaseService, DatabaseService>(sp => new DatabaseService(GlobalHosting.ConnectionString)); // TODO: inject user when auth is implemented
        builder.Services.AddScoped<IConwayGame, ConwayGame>();
        builder.Services.AddScoped<IConwayService, ConwayService>();

        builder.Services.AddControllers().AddNewtonsoftJson(o =>
        {
            o.SerializerSettings.Converters.Add(new BoolToBitJsonConverter());
        });
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        });
        builder.Services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

        var app = builder.Build();

        GlobalHosting.HostingEnvironment = app.Environment;

        LogAppSettings(app);
        RunMigrations();

        // TODO: configure CORS once auth is in place (an important part of general app security)

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseExceptionHandler(
              options =>
              {
                  options.Run(
                      async context =>
                      {
                          // Let's not show stack traces in production
                          context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                          context.Response.ContentType = "text/html";
                          var ex = context.Features.Get<IExceptionHandlerFeature>();
                          if (ex != null)
                          {
                              var err = $"An error occurred. Check the logs.";

                              await context.Response.WriteAsync(err).ConfigureAwait(false);
                          }
                      });
              }
            );
        }

        // Not required yet
        //app.UseAuthorization();

        // Logging enrichers
        app.Use(async (ctx, next) => await next(Enrich(ctx)));
        app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = EnrichHttpRequest);

        app.MapControllers();

        app.Run();
    }

    public static void RunMigrations()
    {
        /* A dacpac is another good option, but I often find I prefer a bit more control over 
         *   the migration process. Dacpacs make a lot of assumptions about how to migrate a database,
         *   and often get it right, but when they don't, it can be a real pain to debug and fix.
         *  
         * This gives us a bit more control over the process, and allows us to run migrations in code.
         */
        try
        {
            DatabaseMigrationRunner.MigrateDatabase("dbo", GlobalHosting.ConnectionString);
        }
        catch (Exception e)
        {
            LogTo.Error(e, "Migrations failed to run on app startup.");
            throw;
        }
    }

    public static HttpContext Enrich(HttpContext httpContext)
    {
        try
        {
            var userAgent = httpContext?.Request.Headers["User-Agent"];

            if (!string.IsNullOrEmpty(userAgent))
            {
                LogContext.PushProperty("UserAgent", Truncate(userAgent, 500));
            }

            var ip = (httpContext?.Connection?.RemoteIpAddress ?? new IPAddress(0)).ToString();

            if (!string.IsNullOrEmpty(ip))
            {
                LogContext.PushProperty("IPAddress", Truncate(ip, 40));
            }
        }
        catch (Exception e)
        {
            // Can't log an exception, as this is an exception inside the logging middleware.
            // However, we will add some metadata to the current logging message.
            LogContext.PushProperty("EnrichmentError", e);
        }

        return httpContext;
    }

    private static string Truncate(string value, int maxChars)
    {
        return value.Length <= maxChars ? value : value.Substring(0, maxChars - 2) + "\u2026"; // Unicode ellipsis
    }

    public static void EnrichHttpRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var request = httpContext.Request;

        // Set all the common properties available for every request
        diagnosticContext.Set("Host", request.Host);
        diagnosticContext.Set("Protocol", request.Protocol);
        diagnosticContext.Set("Scheme", request.Scheme);

        // Only set it if available. You're not sending sensitive data in a querystring right?!
        if (request.QueryString.HasValue)
        {
            diagnosticContext.Set("QueryString", request.QueryString.Value);
        }

        // Set the content-type of the Response at this point
        diagnosticContext.Set("ContentType", httpContext.Response.ContentType);

        // Retrieve the IEndpointFeature selected for the request
        var endpoint = httpContext.GetEndpoint();

        if (endpoint is object)
        {
            diagnosticContext.Set("EndpointName", endpoint.DisplayName);
        }
    }

    private static void LogAppSettings(WebApplication app)
    {
        var configurationMessages = new List<string>
          {
            string.Empty,
            "***CONFIGURATION***",
            $" > EnvironmentName: {app.Environment.EnvironmentName}",
            $" > IsDevelopment: {app.Environment.IsDevelopment()}",
            string.Empty,
          };

        foreach (var logSetting in GlobalHosting.AppSettings.Logging)
        {
            configurationMessages.Add($" > Logging: {logSetting.Key} = {logSetting.Value}");
        }

        if (GlobalHosting.AppSettings.Features != null)
        {
            configurationMessages.Add(string.Empty);

            foreach (var featureFlag in GlobalHosting.AppSettings.Features)
            {
                configurationMessages.Add($" > Feature Flag: {featureFlag.Key} = {featureFlag.Value}");
            }
        }

        configurationMessages.Add(string.Empty);

        LogTo.Debug(string.Join(Environment.NewLine, configurationMessages));
    }

    private static ColumnOptions GetColumnOptions(bool includeEnrichedColumns, Action<ColumnOptions>? callback = null)
    {
        var columnOptions = new ColumnOptions();
        columnOptions.Store.Remove(StandardColumn.Properties); // Remove XML metadata
        columnOptions.Store.Add(StandardColumn.LogEvent); // Add JSON metadata

        if (includeEnrichedColumns)
        {
            columnOptions.AdditionalColumns = new Collection<SqlColumn>
        {
            new SqlColumn("IPAddress", SqlDbType.NVarChar),
            new SqlColumn("UserAgent", SqlDbType.NVarChar),
            // TODO: add session id, username, etc. once auth is enabled
        };
        }

        columnOptions.LogEvent.ExcludeStandardColumns = true;
        columnOptions.LogEvent.ExcludeAdditionalProperties = true;

        callback?.Invoke(columnOptions);

        return columnOptions;
    }
}