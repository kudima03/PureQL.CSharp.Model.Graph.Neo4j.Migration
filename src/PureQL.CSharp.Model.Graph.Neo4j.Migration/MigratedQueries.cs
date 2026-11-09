using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// The stored query graphs migrated from the model version they were written with as
/// far as a chain of migrations reaches.
/// </summary>
/// <param name="Runner">
/// The session or transaction to run the migrations on; run it in one transaction (as
/// <see cref="MigratedQueriesOf"/> does) so that a failed chain changes nothing.
/// </param>
/// <param name="Migrations">
/// The migrations to choose from, in any order; at most one may start at each version.
/// </param>
/// <remarks>
/// Starting from the <see cref="StoredModelVersion"/>, it applies the migration whose
/// <see cref="IGraphMigration.From"/> is the current version, marks the graphs with
/// its <see cref="IGraphMigration.To"/> (<see cref="MarkedModelVersion"/>), and
/// continues from there until no migration starts at the current version. Migrations
/// starting at other versions are not applied.
/// </remarks>
public sealed record MigratedQueries(
    IAsyncQueryRunner Runner,
    IEnumerable<IGraphMigration> Migrations
)
{
    /// <summary>Applies the chain of migrations starting at the stored version.</summary>
    /// <param name="ct">A token to cancel the chain between statements.</param>
    /// <returns>
    /// A task that completes once the last migration of the chain is marked.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Two migrations start at the same version, the chain from the stored version
    /// comes back to a version it passed, no model version marker is stored, or more
    /// than one is.
    /// </exception>
    public async Task ApplyAsync(CancellationToken ct = default)
    {
        Dictionary<ModelVersion, IGraphMigration> migrations = ByFrom();
        ModelVersion current =
            await new StoredModelVersion(Runner).ValueAsync(ct)
            ?? throw new InvalidOperationException(
                "No model version marker is stored: mark the stored graphs with the "
                    + "model version they were written with first."
            );
        foreach (IGraphMigration migration in Chain(migrations, current))
        {
            await migration.ApplyAsync(Runner, ct);
            await new MarkedModelVersion(Runner, migration.To).ApplyAsync(ct);
        }
    }

    private static List<IGraphMigration> Chain(
        Dictionary<ModelVersion, IGraphMigration> migrations,
        ModelVersion start
    )
    {
        List<IGraphMigration> chain = [];
        HashSet<ModelVersion> passed = [start];
        ModelVersion current = start;
        while (migrations.TryGetValue(current, out IGraphMigration? migration))
        {
            if (!passed.Add(migration.To))
            {
                throw new InvalidOperationException(
                    $"The migrations from model version {start.Value} come back to "
                        + $"version {migration.To.Value}."
                );
            }
            chain.Add(migration);
            current = migration.To;
        }
        return chain;
    }

    private Dictionary<ModelVersion, IGraphMigration> ByFrom()
    {
        Dictionary<ModelVersion, IGraphMigration> migrations = [];
        foreach (IGraphMigration migration in Migrations)
        {
            if (!migrations.TryAdd(migration.From, migration))
            {
                throw new InvalidOperationException(
                    "More than one migration starts at model version "
                        + $"{migration.From.Value}."
                );
            }
        }
        return migrations;
    }
}
