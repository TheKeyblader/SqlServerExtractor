using Microsoft.Data.SqlClient;

namespace SqlServerExtractor
{
    public class ViewExtractor : IObjectExtractor
    {
        private readonly SqlConnection _connection;

        public ObjectType Type => ObjectType.Views;

        public ViewExtractor(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<string> GetObjectDefinition(string name)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT name FROM sys.views";

            var value = await command.ExecuteScalarAsync();
            return null;
        }

        public async IAsyncEnumerable<string> ListObject()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT OBJECT_SCHEMA_NAME(views.object_id), name FROM sys.views";

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
