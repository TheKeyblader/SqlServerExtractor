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
            [CommandOption("-t|--type"), DefaultValue(ObjectType.All)]
            public ObjectType ObjectType { get; set; }
            [CommandOption("-d|--directory"), DefaultValue("./Scripts")]
            public string OutputDirectory { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            AnsiConsole.Write(
                new FigletText("Sql Server Extractor")
                    .LeftAligned()
                    .Color(Color.Green));

            await AnsiConsole.Status()
                .SpinnerStyle(Style.Parse("olive"))
                .StartAsync("Testing Connection", async ctx =>
                 {
                     if (await TestConnectionAsync(settings.ConnectionString))
                     {
                         AnsiConsole.MarkupLine("Connexion to database [green]OK[/]");
                         _connection = new SqlConnection(settings.ConnectionString + ";MultipleActiveResultSets=True");
                     }
                 });

            await _connection.OpenAsync();

            var extractors = GetExtractors(settings.ObjectType);

            AnsiConsole.MarkupLine("Listing Objects...");

            var table = new Table()
                .Centered()
                .AddColumns("Type", "Name")
                .Title("Object List");

            var dic = new Dictionary<IObjectExtractor, string[]>();

            await AnsiConsole.Live(table)
                .StartAsync(async ctx =>
                {
                    ctx.Refresh();
                    foreach (var extractor in extractors)
                    {
                        var names = new List<string>();
                        await foreach (var name in extractor.ListObject())
                        {
                            names.Add(name);
                            table.AddRow(extractor.Type.ToString(), name);
                            ctx.Refresh();
                        }
                        dic.Add(extractor, names.ToArray());
                    }
                });

            await ExtractContent(dic, settings);

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
                        case ObjectType.View:
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

        private async Task ExtractContent(Dictionary<IObjectExtractor, string[]> extractors, Settings settings)
        {
            async Task ExtractContent(IObjectExtractor extractor, ProgressTask progress)
            {
                var names = extractors[extractor];
                progress.MaxValue = names.Length;
                foreach (var name in names)
                {
                    var content = await extractor.GetObjectDefinition(name);

                    if (string.IsNullOrEmpty(content))
                        AnsiConsole.MarkupLine($"[olive]WARN :[/] The {extractor.Type} {name} definition is empty");
                    else
                    {
                        var withoutScheme = $"{string.Join("", name.Split(".")[1..])}.sql";
                        var folderPath = string.Join("/", withoutScheme.Split(settings.FolderSeparator)[..^1]);
                        var fileName = withoutScheme.Split(settings.FolderSeparator)[^1];
                        var path = Directory.CreateDirectory(Path.Join(settings.OutputDirectory, extractor.Type + "s", folderPath));

                        await File.WriteAllTextAsync(Path.Join(path.FullName, fileName), content);
                    }
                    progress.Increment(1);
                }
            }

            await AnsiConsole.Progress()
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    var tasks = new Task[extractors.Count];

                    for (int i = 0; i < extractors.Count; i++)
                    {
                        var extractor = extractors.Keys.ElementAt(i);
                        var progress = ctx.AddTask($"Extracting {extractor.Type}s");
                        tasks[i] = ExtractContent(extractor, progress);
                    }

                    await Task.WhenAll(tasks);
                });
        }
    }

    [Flags]
    public enum ObjectType
    {
        None = 0,
        StoredProcedure = 1,
        Function = 2,
        Trigger = 4,
        View = 8,
        All = StoredProcedure | Function | Trigger | View
    }
}
