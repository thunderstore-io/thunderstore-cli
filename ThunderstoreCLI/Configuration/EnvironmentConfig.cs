using static System.Environment;

namespace ThunderstoreCLI.Configuration;

class EnvironmentConfig : EmptyConfig
{
    private const string AUTH_TOKEN = "TCLI_AUTH_TOKEN";

    public override AuthConfig GetAuthConfig()
    {
        return new AuthConfig
        {
            AuthToken = ReadEnv(AUTH_TOKEN)
        };
    }

    private string? ReadEnv(string variableName)
    {
        // Try to read the value from user-specific env variables.
        // This should result with up-to-date value on Windows, but
        // doesn't work on Linux/Mac.
        var value = GetEnvironmentVariable(AUTH_TOKEN, EnvironmentVariableTarget.User);


        // Alternatively try to read the value from process-specific
        // env variables. This works on Linux/Mac, but results in
        // outdated values if the env variable has been updated
        // after the shell was launched.
        if (String.IsNullOrWhiteSpace(value))
        {
            value = GetEnvironmentVariable(AUTH_TOKEN, EnvironmentVariableTarget.Process);
        }

        return value;
    }
}
