
using Spectre.Console.Cli;
using SqlServerExtractor;

var app = new CommandApp<SqlServerExtractorCommand>();

app.Configure(config =>
{
    config.PropagateExceptions();
});

await app.RunAsync(args);