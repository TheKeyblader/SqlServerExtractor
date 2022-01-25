using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SqlServerExtractor
{
    public class SqlServerExtractorCommand : AsyncCommand<SqlServerExtractorCommand.Settings>
    {
        private SqlConnection _connection;

        public class Settings : CommandSettings
        {
            [CommandArgument(0, "<connectionString>")]
            public string ConnectionString { get; set; }
            [CommandOption("-s|--separator"), DefaultValue('_')]
            public char FolderSeparator { get; set; }
            [CommandOption("-o|--objectTye"), DefaultValue(ObjectType.All)]
            public ObjectType ObjectType { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await AnsiConsole.Status()
                .SpinnerStyle(Style.Parse("olive"))
                .StartAsync("Testing Connection", async ctx =>
                 {
                     if (await TestConnectionAsync(settings.ConnectionString))
                     {
                         AnsiConsole.MarkupLine("Connexion to database [green]OK[/]");
                         _connection = new SqlConnection(settings.ConnectionString);
                     }
                 });

            await _connection.OpenAsync();

            var extractors = GetExtractors(settings.ObjectType);

            AnsiConsole.MarkupLine("Listing Objects...");

            var table = new Table()
                .Centered()
                .AddColumns("Type", "Name")
                .Title("Object List");

            await AnsiConsole.Live(table)
                .StartAsync(async ctx =>
                {
                    ctx.Refresh();
                    foreach (var extractor in extractors)
                    {
                        await foreach (var name in extractor.ListObject())
                        {
                            table.AddRow(extractor.Type.ToString(), name);
                            ctx.Refresh();
                        }
                    }
                });

            await _connection.CloseAsync();

            return 0;
        }


        private async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                var connection = new SqlConnection(connectionString);
                await Task.WhenAll(connection.OpenAsync(), Task.Delay(1000));
                await connection.CloseAsync();
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[maroon]Can't open connection to database ![/]");
                AnsiConsole.WriteException(ex);
                return false;
            }
        }

        private IObjectExtractor[] GetExtractors(ObjectType types)
        {
            var extractors = new List<IObjectExtractor>();
            var objectTypeValues = Enum.GetValues<ObjectType>();
            foreach (ObjectType value in objectTypeValues)
            {
                if ((types & value) == value)
                {
                    switch (value)
                    {
                        case ObjectType.Views:
                            var viewExtractor = new ViewExtractor(_connection);
                            extractors.Add(viewExtractor);
                            break;
                        case ObjectType.StoredProcedure:
                            var spExtractor = new StoredProcedureExtractor(_connection);
                            extractors.Add(spExtractor);
                            break;
                        case ObjectType.Function:
                            var funcExtractor = new FunctionExtractor(_connection);
                            extractors.Add(funcExtractor);
                            break;
                        case ObjectType.Trigger:
                            var triggerExtractor = new TriggerExtractor(_connection);
                            extractors.Add(triggerExtractor);
                            break;
                    }
                }
            }
            return extractors.ToArray();
        }
    }

    [Flags]
    public enum ObjectType
    {
        None = 0,
        StoredProcedure = 1,
        Function = 2,
        Trigger = 4,
        Views = 8,
        All = StoredProcedure | Function | Trigger | Views
    }
}
