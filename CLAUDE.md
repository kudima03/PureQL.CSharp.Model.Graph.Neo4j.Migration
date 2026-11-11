# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory. The projects need the .NET 11 SDK (for the model's C# 15 unions); where the system SDK is older, use `~/.dotnet/dotnet` or put it first on `PATH` with `DOTNET_ROOT=$HOME/.dotnet`.

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes          # check code style (CI enforces this)
dotnet format                              # auto-fix code style
dotnet test --no-build --verbosity normal  # run tests
dotnet csharpier check .                   # check formatting (CI enforces this)
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`.

## Architecture

This is the **Neo4j migration package** of the `PureQL.CSharp.Model.Graph` family: it migrates stored query graphs between versions of `PureQL.CSharp.Model`. It depends on `PureQL.CSharp.Model.Graph` `0.1.0-preview.1.0.0`, `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.1.0.0` and `Neo4j.Driver` `6.5.1`, and on no other operation package. Namespace `PureQL.CSharp.Model.Graph.Neo4j.Migration`, one public type per file:

- `ModelVersion` — `sealed record (string Value)`: a `PureQL.CSharp.Model` version; a null, empty or white-space value throws, also through `with`.
- `IGraphMigration` — `From`, `To` and `ApplyAsync(IAsyncQueryRunner runner, CancellationToken ct = default)`: one step between two model versions.
- `StoredModelVersion` — `(IAsyncQueryRunner Runner)`, `ValueAsync`: `MATCH (v:PureQLModelVersion) RETURN v.value AS value LIMIT 2`; `null` without a marker, `InvalidOperationException` with two or with a marker holding no non-empty string.
- `MarkedModelVersion` — `(IAsyncQueryRunner Runner, ModelVersion Version)`, `ApplyAsync`: `MERGE (v:PureQLModelVersion) SET v.value = $value`, so there is one marker at most.
- `MigratedQueries` — `(IAsyncQueryRunner Runner, IEnumerable<IGraphMigration> Migrations)`, `ApplyAsync`: indexes the migrations by `From` (a duplicate throws), reads the stored version (none throws), builds the chain from it (a loop throws) before running anything, then applies each step and marks its `To`.
- `RelabeledRecords` — `IGraphMigration` over `Labels` (old → new): one statement, `UNWIND` the pairs, `MATCH (n:$(label.old))`, collect, remove every old label in a `CALL (rows) { … }`, then set the new ones, so pairs may swap or chain.
- `RenamedProperties` — `IGraphMigration` over the nodes with `Label` and `Properties` (old → new): one statement computing the moved values first (`n[p.old]`), nulling every old key, then setting `n[new]`, so pairs may swap or chain; nodes lacking an old key keep their other properties.
- `StoredModelVersionOf`, `MarkedModelVersionOf`, `MigratedQueriesOf` — the same over an `INeo4jStoredQueries`: a session on `Store.Driver` for `Store.Database`, a read transaction for the first, a write transaction for the others (`MigratedQueriesOf` runs the whole chain in one, so a failed step changes nothing).
- `RenamePairs` (internal) — a dictionary as a list of `{old, new}` maps for the statements.

Statements use Neo4j 5.26+'s dynamic labels (`$(...)`) and dynamic property keys (`n[key]`).

The public API is the one the family agreed on for this package: do not rename or reshape it. Every public member has XML documentation (`GenerateDocumentationFile`; a missing comment fails the build under `-warnaserror`).

**Test project:** `PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests` targets `net11.0` and uses xunit with coverlet. `ModelVersionTests` are unit tests; the others run against one `neo4j:5-community` container started by Testcontainers (`Neo4jFixture`, shared through the `Neo4jDatabase` collection; Docker is required, no fixed ports) and clear the database before each test. `RecordingMigration` and `FailingMigration` are test migrations for the chain and the rollback.

**Target framework:** net11.0 only, as the model. `IsAotCompatible` is `true`.

**Package validation:** enabled against `PackageValidationBaselineVersion` `0.1.0-preview.0.1.0`, the first release; the `nuget-publish` skill moves it after each release, and intentional breaks go to `CompatibilitySuppressions.xml` next to the csproj.

**Mutation testing:** the Stryker step of CI is commented out.

**Publishing:** triggered by pushing a semver tag (pattern `*.*.*`). The tag becomes the `PackageVersion`. The workflow publishes to both GitHub Packages and NuGet.org.

## Code Style

Enforced via `.editorconfig` and `dotnet format --verify-no-changes` in CI:

- No `var` — always use explicit types
- No expression-bodied methods or constructors — use block bodies
- Expression-bodied properties and accessors are required
- File-scoped namespace declarations (`namespace Foo.Bar;`)
- Private fields prefixed with `_` in camelCase (e.g., `_options`)
- Braces always required, even for single-statement blocks
- Max line length: 90 characters
- Allman brace style — opening brace on its own line for all blocks
- `System.*` using directives sorted first, no blank line between using groups
- Parentheses required in arithmetic and relational binary operators for clarity

## Commit Messages

Do not mention Claude or AI assistance in commit messages.
