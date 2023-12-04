# thunderstore-cli

[![Build & Test](https://github.com/thunderstore-io/thunderstore-cli/actions/workflows/test.yml/badge.svg)](https://github.com/thunderstore-io/thunderstore-cli/actions/workflows/test.yml)
[![codecov](https://codecov.io/gh/thunderstore-io/thunderstore-cli/branch/master/graph/badge.svg)](https://codecov.io/gh/thunderstore-io/thunderstore-cli)
[![NuGet Package](https://img.shields.io/nuget/v/tcli)](https://www.nuget.org/packages/tcli)
[![downloads](https://img.shields.io/nuget/dt/tcli)](https://www.nuget.org/packages/tcli)

Thunderstore CLI (just "TCLI" from here on) is a command line tool for building and uploading mod packages to
[Thunderstore](https://thunderstore.io/) mod database, and installing mods via the command line.

## Installation
If all you're interested in is the package building/publishing capabilities of TCLI, then you can simply do:
```
dotnet tool install -g tcli
```
In your command line of choice, which will install the tool via NuGet.
This version doesn't come with the mod installation functionality however.

Otherwise, just download the latest release from [here](https://github.com/thunderstore-io/thunderstore-cli/releases) and extract the ZIP wherever you'll be using TCLI.

## Usage
For building packages, see [the wiki](https://github.com/thunderstore-io/thunderstore-cli/wiki).

For managing mods via TCLI, see the next section.
## Mod Management

### Installation
TCLI will automatically download and install mods in the correct format for you, and will run the game with whatever arguments your mod loader requires.

For all of these commands, `tcli.exe` can be swapped for `./tcli` on Linux. Everything else should be the same.

To get started, import your game from the Thunderstore API using:
```
tcli.exe import-game {game identifier, e.g. ror2 or valheim}
```
To run the game from a specific file instead of say, through Steam (you probably want this for servers!) use `--exepath {path/to/server/launcher.exe}`. Passing in a script file also works fine.

To install mods, use the command:
```
tcli.exe install {game identifier} {namespace-modname(-version)}
```
You can also add `--profile ProfileName` to set a custom name for the profile. By default it uses the name `DefaultProfile`.

Mod uninstalls are done in a similar fashion:
```
tcli.exe uninstall {game identifier} {namespace-modname}
```
And running the game is done with:
```
tcli.exe run {game identifier}
```
The `--profile` snippet from above still applies to both of those commands.

If you want to run the game with a specific set of arguments, you can use `--args "--flag parameter1 parameter2"`

The installed mods by default will go into `%APPDATA%\ThunderstoreCLI` on Windows and `~/.config/ThunderstoreCLI` on Linux. This is configurable by using `--tcli-directory` with any command.

The ThunderstoreCLI directory will contain a file called `GameDefintions.json`, which contains metadata about the configured games, the profiles for each game, and a copy of the manifests of each installed mod. You shouldn't normally be editing this manually.

The same directory also contains a cache for mod ZIPS named `ModCache`, the actual profile files in a folder called `Profiles`, and a caches for the API responses from the Thunderstore API for packages.

All in all, the structure is very similar to that of TMM/r2mm, but on the command line!

## Contributing

### pre-commit

This project uses [Pre-commit](https://pre-commit.com/) to enforce code style
practices. In addition to having .NET and pre-commit installed locally, you'll
need [dotnet-format](https://github.com/dotnet/format), which can be installed
with:

```
dotnet tool install -g dotnet-format
```

### Versioning

This project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Versioning is handled with [MinVer](https://github.com/adamralph/minver) via Git
tags.

* To create a new pre-release, use alpha suffix, e.g. `git tag 0.1.0-alpha.1`
* Any subsequent commits will automatically be versioned "0.1.0-alpha.1.1",
  where the last number denotes the number of commits since the last version tag
  (a.k.a. "height")
* To create a new release, use e.g. `git tag 0.1.0`
* Any subsequent commits will automatically be versioned "0.1.1-alpha.0.1"
* Remember to push the tags to GitHub, e.g. `git push origin 0.1.0`
