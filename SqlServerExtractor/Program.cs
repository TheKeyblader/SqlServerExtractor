
using Spectre.Console.Cli;
using SqlServerExtractor;

var app = new CommandApp<SqlServerExtractorCommand>();
await app.RunAsync(args);