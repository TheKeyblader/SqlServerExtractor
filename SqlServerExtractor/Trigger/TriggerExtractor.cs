using Microsoft.Data.SqlClient;

namespace SqlServerExtractor
{
    public class TriggerExtractor : IObjectExtractor
    {
        private readonly SqlConnection _connection;

        public ObjectType Type => ObjectType.Trigger;

        public TriggerExtractor(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<string> GetObjectDefinition(string name)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = $"SELECT definition FROM sys.sql_modules WHERE object_id = object_id('{name}');";

            var value = await command.ExecuteScalarAsync();
            return value as string;
        }

        public async IAsyncEnumerable<string> ListObject()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT OBJECT_SCHEMA_NAME(object_id), name FROM sys.triggers";

            using var reader = await command.ExecuteReaderAsync();

            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    var scheme = reader.GetString(0);
                    var name = reader.GetString(1);
                    yield return $"{scheme}.{name}";
                }
            }
        }
    }
}
