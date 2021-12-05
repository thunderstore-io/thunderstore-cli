namespace ThunderstoreCLI.Config;

/// <summary>Helper for validating command-specific configurations</summary>
public class Validator
{
    private List<string> _errors;
    private string _name;

    public Validator(string commandName, List<string>? errors = null)
    {
        _name = commandName;
        _errors = errors ?? new List<string>();
    }

    /// <summary>Add given errorMessage if isError is true</summary>
    /// <returns>Value of passed isError</returns>
    public bool Add(bool isError, string errorMessage)
    {
        if (isError)
        {
            _errors.Add(errorMessage);
        }
        return isError;
    }

    /// <summary> Add error if given value is null or empty-ish string</summary>
    /// <returns>True if value is empty</returns>
    public bool AddIfEmpty(string? value, string settingName)
    {
        return Add(
            String.IsNullOrWhiteSpace(value),
            $"{settingName} setting can't be empty"
        );
    }
    public bool AddIfNotSemver(string? version, string settingName)
    {
        if (AddIfEmpty(version, settingName))
        {
            return true;
        }

        return Add(
            !StringUtils.IsSemVer(version!),
            $"Invalid package version number \"{version}\". Version numbers must follow the Major.Minor.Patch format (e.g. 1.45.320)"
        );
    }


    /// <summary>Add error if given value is null</summary>
    /// <returns>True if value is null</returns>
    public bool AddIfNull<T>(T value, string settingName)
    {
        return Add(
            value is null,
            $"{settingName} setting can't be null"
        );
    }

    public List<string> GetErrors() => _errors;

    /// <summary>Output any added error messages to Console</summary>
    /// <exception cref="CommandException">Throw if any errors were added</exception>
    public void ThrowIfErrors()
    {
        if (_errors.Count > 0)
        {
            Write.ErrorExit($"Invalid configuration to run '{_name}' command", _errors.ToArray());
            throw new CommandException("Invalid config for InitCommand");
        }
    }
}
