using Microsoft.Data.SqlClient;

namespace SqlServerExtractor
{
    public class FunctionExtractor : IObjectExtractor
    {
        private readonly SqlConnection _connection;
        public ObjectType Type => ObjectType.Function;

        public FunctionExtractor(SqlConnection connection)
        {
            _connection = connection;
        }

        public Task<string> GetObjectDefinition(string name)
        {
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<string> ListObject()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT SCHEMA_NAME(schema_id), name FROM sys.objects WHERE type IN ('AF','FN','FS','FT','IF','TF')";

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
