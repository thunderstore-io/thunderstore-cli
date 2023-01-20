using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static class ImportGameCommand
{
    public static int Run(Config config)
    {
        var http = new HttpClient();

        var response = http.Send(new HttpRequestMessage(HttpMethod.Get, "https://gcdn.thunderstore.io/static/dev/schema/ecosystem-schema.0.0.2.json"));

        response.EnsureSuccessStatusCode();

        var schema = SchemaResponse.Deserialize(response.Content.ReadAsStream())!;

        if (!schema.games.TryGetValue(config.GameImportConfig.GameId!, out var game))
        {
            throw new CommandFatalException($"Could not find game with ID {config.GameImportConfig.GameId}");
        }

        var def = game.ToGameDefintion(config);
        if (def == null)
        {
            throw new CommandFatalException("Game not installed");
        }

        var collection = GameDefinitionCollection.FromDirectory(config.GeneralConfig.TcliConfig);
        collection.List.Add(def);
        collection.Write();

        Write.Success($"Successfully imported {def.Name} ({def.Identifier}) with install folder \"{def.InstallDirectory}\"");

        return 0;
    }
}
