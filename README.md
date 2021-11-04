# thunderstore-cli

[![codecov](https://codecov.io/gh/thunderstore-io/thunderstore-cli/branch/master/graph/badge.svg)](https://codecov.io/gh/thunderstore-io/thunderstore-cli)

Command line tool for building and uploading mod packages to
[Thunderstore](https://thunderstore.io/) mod database.

## pre-commit

This project uses [Pre-commit](https://pre-commit.com/) to enforce code style
practices. In addition to having .NET and pre-commit installed locally, you'll
need [dotnet-format](https://github.com/dotnet/format), which can be installed
with:

```
dotnet tool install -g dotnet-format
```

## Versioning

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
