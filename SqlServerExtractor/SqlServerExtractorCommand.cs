using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServerExtractor
{
    public class SqlServerExtractorCommand : AsyncCommand<SqlServerExtractorCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandArgument(0, "<connectionString>")]
            public string ConnectionString { get; set; }
            [CommandOption("-s|--separator"), DefaultValue('_')]
            public char FolderSeparator { get; set; }
            [CommandOption("-o|--objectTye")]
            public ObjectType ObjectType { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await AnsiConsole.Status()
                .SpinnerStyle(Style.Parse("olive"))
                .StartAsync("Testing Connection", async ctx =>
                 {
                     if (await TestConnectionAsync(settings.ConnectionString))
                         AnsiConsole.MarkupLine("Connexion to database [green]OK[/]");
                 });

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
                AnsiConsole.MarkupLine(ex.Message);
                return false;
            }
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
    }
}
