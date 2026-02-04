// Single instance for all tests in assembly
[assembly: AssemblyFixture(typeof(CreateMatrixTests.SingleInstanceFixture))]

namespace CreateMatrixTests;

// Sequential execution fixture
[CollectionDefinition("Sequential Test Collection", DisableParallelization = true)]
public class SequentialCollectionDefinition;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1063:Implement IDisposable Correctly",
    Justification = "Demonstration only"
)]
public class SingleInstanceFixture : IDisposable
{
    public void Dispose() => GC.SuppressFinalize(this);
}
