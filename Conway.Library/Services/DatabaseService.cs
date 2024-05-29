using Anotar.Serilog;
using Conway.Library.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Conway.Library.Services
{
    public interface IDatabaseService
    {
        /// <summary>
        /// Executes arbitrary SQL. Updates, Inserts, etc.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="sqlParameters">Any parameters you wish to pass along.</param>
        /// <param name="timeoutSeconds">If omitted, the standard query timeout applies (30s).</param>
        /// <returns>An awaitable Task.</returns>
        Task ExecuteAsync(string sql, object? sqlParameters = null, int? timeoutSeconds = null);

        /// <summary>
        /// Retrieves a list of objects from the database, using the provided sql.
        /// If nested entities are needed, use GetWithRelationshipsAsync instead.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="sqlParameters">Any parameters you wish to pass along.</param>
        /// <param name="timeoutSeconds">If omitted, the standard query timeout applies (30s).</param>
        /// <returns>A collection of objects of the specified type.</returns>
        Task<IEnumerable<T>> GetAsync<T>(string sql, object? sqlParameters = null, int? timeoutSeconds = null);

        /// <summary>
        /// Retrieves a list of objects from the database, using the provided sql.
        /// Can nest entities if compatible SQL is provided.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="sqlParameters">Any parameters you wish to pass along.</param>
        /// <param name="timeoutSeconds">If omitted, the standard query timeout applies (30s).</param>
        /// <returns>A collection of objects of the specified type.</returns>
        Task<IEnumerable<T>> GetWithRelationshipsAsync<T>(string sql, object sqlParameters = null, int? timeoutSeconds = null);

        /// <summary>
        /// Retrieves an object from the database, using the provided sql.
        /// If nested entities are needed, use GetFirstOrDefaultWithRelationshipsAsync instead.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="sqlParameters">Any parameters you wish to pass along.</param>
        /// <returns>An object of the specified type.</returns>
        Task<T> GetFirstOrDefaultAsync<T>(string sql, object? sqlParameters = null);

        /// <summary>
        /// Retrieves an objects from the database, using the provided sql.
        /// Can nest entities if compatible SQL is provided.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="sqlParameters">Any parameters you wish to pass along.</param>
        /// <returns>An object of the specified type.</returns>
        Task<T> GetFirstOrDefaultWithRelationshipsAsync<T>(string sql, object? sqlParameters = null);

        /// <summary>
        /// Creates a SQL Select statement for the model of type T passed
        /// </summary>
        /// <typeparam name="T">The model</typeparam>
        /// <param name="tableAlias">The table alias</param>
        /// /// <param name="staticValues">[OPTIONAL] static values to return instead of column values</param>
        /// <returns>A SQL Select statement</returns>
        string GetSelectStatementFromModel<T, Source>(string tableAlias, string propertyName = null, Dictionary<string, string> staticValues = null);

        /// <summary>
        /// Creates a SQL Select statement for the model of type T passed, aliased as a property of Source
        /// </summary>
        /// <typeparam name="T">The model</typeparam>
        /// <typeparam name="Source">The model which contains T</typeparam>
        /// <param name="tableAlias">The table alias</param>
        /// <param name="propertyName">[OPTIONAL] the name of the property where T is found on Source</param>
        /// <param name="staticValues">[OPTIONAL] static values to return instead of column values</param>
        /// <returns>A SQL Select statement</returns>
        string GetSelectStatementFromModel<T>(string tableAlias, Dictionary<string, string> staticValues = null);
    }

    public class DatabaseService : IDatabaseService
    {
        static ILogger Logger = Log.ForContext<DatabaseService>();

        public delegate Task Execute(string sql);

        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task ExecuteAsync(string sql, object sqlParameters = null, int? timeoutSeconds = null)
        {
            using (var connection = DatabaseConnection())
            {
                await ExecAndLogQueryAsync(async () => await connection.ExecuteAsync(sql, sqlParameters, commandTimeout: timeoutSeconds), sql, sqlParameters, timeoutSeconds);
            }
        }

        public async Task<IEnumerable<T>> GetAsync<T>(string sql, object sqlParameters = null, int? timeoutSeconds = null)
        {
            using (var connection = DatabaseConnection())
            {
                return await ExecAndLogQueryAsync(async () => await connection.QueryAsync<T>(sql, sqlParameters, commandTimeout: timeoutSeconds), sql, sqlParameters, timeoutSeconds);
            }
        }

        public async Task<IEnumerable<T>> GetWithRelationshipsAsync<T>(string sql, object sqlParameters = null, int? timeoutSeconds = null)
        {
            using (var connection = DatabaseConnection())
            {
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    return await ExecAndLogQueryAsync(async () => await connection.QueryAsync<T>(sql, sqlParameters, commandTimeout: timeoutSeconds), sql, sqlParameters, timeoutSeconds);
                }

                var results = await ExecAndLogQueryAsync(async () => await connection.QueryAsync<dynamic>(sql, sqlParameters, commandTimeout: timeoutSeconds), sql, sqlParameters, timeoutSeconds);
                Slapper.AutoMapper.Cache.ClearInstanceCache();

                return Slapper.AutoMapper.MapDynamic<T>(results);
            }
        }

        public async Task<T> GetFirstOrDefaultAsync<T>(string sql, object? sqlParameters = null)
        {
            using (var connection = DatabaseConnection())
            {
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    return await ExecAndLogQueryAsync(async () => await connection.QueryFirstOrDefaultAsync<T>(sql, sqlParameters), sql, sqlParameters, null);
                }

                var result = await ExecAndLogQueryAsync(async () => await connection.QueryFirstOrDefaultAsync<T>(sql, sqlParameters), sql, sqlParameters, null);

                if (result == null)
                {
                    return default;
                }

                return result;
            }
        }

        public async Task<T> GetFirstOrDefaultWithRelationshipsAsync<T>(string sql, object? sqlParameters = null)
        {
            using (var connection = DatabaseConnection())
            {
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    return await ExecAndLogQueryAsync(async () => await connection.QueryFirstOrDefaultAsync<T>(sql, sqlParameters), sql, sqlParameters, null);
                }

                var result = await ExecAndLogQueryAsync(async () => await connection.QueryFirstOrDefaultAsync<dynamic>(sql, sqlParameters), sql, sqlParameters, null);

                if (result == null)
                {
                    return default;
                }

                Slapper.AutoMapper.Cache.ClearInstanceCache();

                return Slapper.AutoMapper.MapDynamic<T>(result);
            }
        }

        public string GetSelectStatementFromModel<T>(string tableAlias, Dictionary<string, string>? staticValues = null)
        {
            return GetSelectStatementFromModel<T, T>(tableAlias, staticValues: staticValues);
        }

        public string GetSelectStatementFromModel<T, Source>(string tableAlias, string? propertyName = null, Dictionary<string, string>? staticValues = null)
        {
            /*
             * I wrote this snippet many years ago and have kept it on hand ever since. 
             * Very handy for generating SQL Select statements from models, and reduces friction when updating property names.
            */

            tableAlias = Bracketize(tableAlias);
            var segments = typeof(Source) == typeof(T) ? [] : GetPathTo<T>(typeof(Source), null, 0, propertyName);
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var allStaticValues = staticValues != null ? PopulateDefaultPropertyValues<T>(staticValues) : null;
            var columns = properties.Where(IsMapped).Select(p =>
            {
                var name = GetColumnName(p);
                var path = string.Join("_", segments);
                var alias = segments.Length > 0 ? $"{path}_{p.Name}" : p.Name;

                if (allStaticValues != null && allStaticValues.TryGetValue(p.Name, out var value))
                {
                    return $"{value} AS {WrapWithSingleQuotes(alias)}";
                }

                return $"{tableAlias}.{Bracketize(name)} AS {WrapWithSingleQuotes(alias)}";
            });

            var selectStatement = "\r\n\t\t" + string.Join(",\r\n\t\t", columns);

            return selectStatement;
        }

        private IDbConnection DatabaseConnection()
        {
            IDbConnection db = new SqlConnection(_connectionString);

            db.Open();

            return db;
        }

        private async Task<SqlConnection> OpenConnectionAsync([CallerMemberName] string context = "")
        {
            try
            {
                var cn = new SqlConnection(_connectionString);

                try
                {
                    await cn.OpenAsync();
                }
                catch (Exception e)
                {
                    LogTo.Error($"Error accessing SQL Database: {_connectionString} Error: {e}");

                    throw;
                }

                try
                {
                    return cn;
                }
                catch (Exception e)
                {
                    LogTo.Error($"Error connecting: {e.Message}");

                    throw;
                }
            }
            catch (SqlException sqlException)
            {
                LogTo.Error($"{context} caused an exception.");

                foreach (SqlError error in sqlException.Errors)
                {
                    LogTo.Error($"Native Error : {error.Message}");
                }

                throw;
            }
            catch (Exception ex)
            {
                LogTo.Error(ex.Message);

                throw;
            }
        }

        private static string? Bracketize(string value)
        {
            return WrapWithCharacters(value, '[', ']');
        }

        private static string? WrapWithSingleQuotes(string value)
        {
            return WrapWithCharacters(value, '\'', '\'');
        }

        private static string? WrapWithCharacters(string value, char start, char end)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!value.StartsWith(start))
            {
                value = $"{start}{value}";
            }

            if (!value.EndsWith(end))
            {
                value = $"{value}{end}";
            }

            return value;
        }

        private static string[] GetPathTo<Destination>(Type sourceType,
          string[]? segments = null,
          int depth = 0,
          string? name = null)
        {
            if (depth > 4)
            {
                return segments;
            }

            if (segments == null)
            {
                segments = new string[0];
            }

            foreach (var prop in sourceType
              .GetProperties(BindingFlags.Instance | BindingFlags.Public)
              .Where(p => !p.PropertyType.IsPrimitive && (p.PropertyType != typeof(string))))
            {
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    if (prop.PropertyType.IsGenericType)
                    {
                        var genericType = prop.PropertyType.GetGenericArguments()[0];
                        if ((genericType == typeof(Destination)) && (string.IsNullOrWhiteSpace(name) ||
                                                                     prop.Name.Equals(name,
                                                                       StringComparison.InvariantCultureIgnoreCase)))
                        {
                            return segments.Concat(new[] { prop.Name }).ToArray();
                        }

                        var len = segments.Length;
                        var pth = GetPathTo<Destination>(genericType,
                          segments.Concat(new[] { prop.Name }).ToArray(),
                          depth + 1,
                          name);
                        if (pth.Length == len + 2)
                        {
                            return pth;
                        }
                    }
                }

                if ((prop.PropertyType == typeof(Destination)) && (string.IsNullOrWhiteSpace(name) ||
                                                                   prop.Name.Equals(name,
                                                                     StringComparison.InvariantCultureIgnoreCase)))
                {
                    return segments.Concat(new[] { prop.Name }).ToArray();
                }

                var length = segments.Length;
                var path = GetPathTo<Destination>(prop.PropertyType,
                  segments.Concat(new[] { prop.Name }).ToArray(),
                  depth + 1,
                  name);
                if (path.Length == length + 2)
                {
                    return path;
                }
            }

            return new string[0];
        }

        public static bool IsMapped(PropertyInfo property)
        {
            return property.GetCustomAttributes(false).All(a => a.GetType().Name != nameof(NotMappedAttribute));
        }

        public static string GetColumnName(MemberInfo property)
        {
            var attr = property.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == nameof(ColumnAttribute));

            return attr == null
              ? property.Name
              : (attr is ColumnAttribute columnAttribute ? columnAttribute.Name : property.Name);
        }

        public static string GetColumnDefault(PropertyInfo property)
        {
            var type = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
              ? Nullable.GetUnderlyingType(property.PropertyType)
              : property.PropertyType;

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Object:
                    if (property.PropertyType == typeof(Guid))
                    {
                        return $"CONVERT(uniqueidentifier, '{Guid.Empty}')";
                    }

                    if (type == typeof(DateTimeOffset))
                    {
                        return "CONVERT(DATETIMEOFFSET, '')";
                    }

                    throw new NotImplementedException();

                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return "-1";

                case TypeCode.String:
                    return "''";

                default:
                    throw new NotImplementedException();
            }
        }

        private static Dictionary<string, string> PopulateDefaultPropertyValues<T>(IDictionary<string, string> seedValues)
        {
            var values = new Dictionary<string, string>(seedValues);
            var mappedProperties = typeof(T)
              .GetProperties(BindingFlags.Instance | BindingFlags.Public)
              .Where(IsMapped)
              .ToArray();

            foreach (var propertyInfo in mappedProperties.Where(p => !values.ContainsKey(p.Name)))
            {
                if (IsNullable(propertyInfo))
                {
                    values[propertyInfo.Name] = "NULL";
                    continue;
                }

                switch (Type.GetTypeCode(propertyInfo.PropertyType))
                {
                    case TypeCode.Byte:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Single:
                    case TypeCode.Boolean:
                        values[propertyInfo.Name] = "0";
                        continue;
                    case TypeCode.DateTime:
                        values[propertyInfo.Name] = "1753-01-01";
                        continue;
                    case TypeCode.String:
                        values[propertyInfo.Name] = "''";
                        continue;
                }

                if (propertyInfo.PropertyType == typeof(DateTimeOffset))
                {
                    values[propertyInfo.Name] = "1753-01-01";
                    continue;
                }

                if (propertyInfo.PropertyType == typeof(Guid))
                {
                    values[propertyInfo.Name] = "00000000-0000-0000-0000-000000000000";
                    continue;
                }

                if (propertyInfo.PropertyType == typeof(TimeSpan))
                {
                    values[propertyInfo.Name] = "00:00:00";
                    continue;
                }
            }

            return values;
        }

        public static bool IsNullable(PropertyInfo propertyInfo)
        {
            var hasNotNullableAttribute = propertyInfo.GetCustomAttributes(false)
              .Any(a => a.GetType().Name == nameof(NotNullableSqlAttribute));
            var hasNullableAttribute = propertyInfo.GetCustomAttributes(false)
              .Any(a => a.GetType().Name == nameof(NullableSqlAttribute));
            var isString = Type.GetTypeCode(propertyInfo.PropertyType) == TypeCode.String;

            return (isString && !hasNotNullableAttribute) // Strings are assumed nullable by default
                   || hasNullableAttribute;
        }

        private async Task<T> ExecAndLogQueryAsync<T>(Func<Task<T>> func, string sql, object? sqlParameters, int? timeoutSeconds, Func<string[]> getMessages = null)
        {
            var sw = Stopwatch.StartNew();
            var results = await func();
            sw.Stop();

            var messages = new string[0];

            if (getMessages != null)
            {
                messages = getMessages();
            }

            Logger
              .ForContext("QueryElapsed", sw.ElapsedMilliseconds)
              .ForContext("QueryTimeout", timeoutSeconds)
              .ForContext("QueryParameters", sqlParameters)
              .ForContext("QueryMessages", messages)
              .Debug(sql);

            return results;
        }
    }
}
