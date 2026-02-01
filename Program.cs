using System.CommandLine;
using LeagueTools.Commands;

var rootCommand = new RootCommand("League Tools - Generate player matchings for Star Wars: Legion leagues");

rootCommand.AddCommand(new FetchCommand());
rootCommand.AddCommand(new GenerateCommand());
rootCommand.AddCommand(new ViewCommand());
rootCommand.AddCommand(new HistoryCommand());
rootCommand.AddCommand(new SwapCommand());

return await rootCommand.InvokeAsync(args);
