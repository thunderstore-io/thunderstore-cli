namespace ThunderstoreCLI.Configuration;

public static class DotEnv
{
    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (value.Length >= 2)
            {
                if ((value.StartsWith('"') && value.EndsWith('"')) ||
                    (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public static void LoadAll(string? environment = null)
    {
        var filesToLoad = new List<string> { ".env" };

        if (!string.IsNullOrEmpty(environment))
        {
            filesToLoad.Add($".env.{environment}");
            filesToLoad.Add($".env.{environment.ToLowerInvariant()}");
            filesToLoad.Add($".{environment}.env");
            filesToLoad.Add($".{environment.ToLowerInvariant()}.env");
        }

        filesToLoad.Add(".env.local");
        filesToLoad.Add(".local.env");

        foreach (var file in filesToLoad)
        {
            Load(file);
        }
    }
}
