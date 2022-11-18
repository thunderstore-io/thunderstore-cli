using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static class ImportGameCommand
{
    public static int Run(Config config)
    {
        R2mmGameDescription? desc;
        try
        {
            desc = R2mmGameDescription.Deserialize(File.ReadAllText(config.GameImportConfig.FilePath!));
        }
        catch (Exception e)
        {
            throw new CommandFatalException($"Failed to read game description file: {e}");
        }
        if (desc is null)
        {
            throw new CommandFatalException("Game description file was empty");
        }

        var def = desc.ToGameDefintion(config);
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
