using Xunit;

namespace ThunderstoreCLI.Tests;

[CollectionDefinition(nameof(NoParallel), DisableParallelization = true)]
public sealed class NoParallel { }
