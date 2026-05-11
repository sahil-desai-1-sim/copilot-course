using BookmarkOrganizer.Commands;
using BookmarkOrganizer.Infrastructure;
using BookmarkOrganizer.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

var dbOverride = ParseDbOverride(args);
var databasePath = DataStorePath.Resolve(dbOverride);
var connectionString = $"Data Source={databasePath};Foreign Keys=True";

var services = new ServiceCollection();
services.AddSingleton<DatabaseInitializer>();
services.AddSingleton<MigrationRunner>();
services.AddSingleton<IBookmarkRepository>(_ => new SqliteBookmarkRepository(connectionString));
services.AddSingleton<IBookmarkService, BookmarkService>();

await using (var migrationConnection = new SqliteConnection(connectionString))
{
	await migrationConnection.OpenAsync();
	var migrationRunner = services.BuildServiceProvider().GetRequiredService<MigrationRunner>();
	await migrationRunner.ApplyMigrationsAsync(migrationConnection);
}

using var provider = services.BuildServiceProvider();

var bookmarkService = provider.GetRequiredService<IBookmarkService>();

var dbOption = new Option<string?>("--db")
{
	Description = "Override the default SQLite database path."
};
var rootCommand = new RootCommand("Bookmark Organizer - store, tag, search, and manage bookmarks locally.");
rootCommand.Options.Add(dbOption);

rootCommand.Subcommands.Add(AddCommandFactory.Create(bookmarkService));
rootCommand.Subcommands.Add(ListCommandFactory.Create(bookmarkService));

return rootCommand.Parse(args).Invoke();

static string? ParseDbOverride(string[] args)
{
	for (var i = 0; i < args.Length; i++)
	{
		if (!string.Equals(args[i], "--db", StringComparison.OrdinalIgnoreCase))
		{
			continue;
		}

		if (i + 1 < args.Length)
		{
			return args[i + 1];
		}
	}

	return null;
}
