# Changelog

All notable changes to `PureQL.CSharp.Model.Graph.Neo4j.Migration` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.0.2.0] - 2026-11-11

### Changed

- Depends on `PureQL.CSharp.Model.Graph` `0.1.0-preview.1.1.0` and
  `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.1.1.0`, the
  .NET 11 releases.
- The .NET 11 SDK is installed by version in CI instead of being pinned by
  `global.json`.

## [0.1.0-preview.0.1.0] - 2026-11-09

First release: migrates the query graphs stored in Neo4j from one version of
`PureQL.CSharp.Model` to the next.

### Added

- **`ModelVersion`** — `sealed record ModelVersion(string Value)`: a
  `PureQL.CSharp.Model` version; a null, empty or white-space value throws.
- **`StoredModelVersion`** and **`MarkedModelVersion`** — read and write the
  single `PureQLModelVersion` marker node holding the model version the stored
  queries were written with. Reading returns `null` without a marker and throws
  on two markers or an empty value.
- **`IGraphMigration`** — one step between two model versions: `From`, `To` and
  `ApplyAsync(IAsyncQueryRunner, CancellationToken)`.
- **`MigratedQueries`** — applies the chain of migrations starting at the
  stored version and marks each step's `To`. It checks the whole chain before
  running anything and throws on two migrations with the same `From`, on a
  loop, or when no version is stored.
- **`RelabeledRecords`** — a migration renaming node labels (old → new).
- **`RenamedProperties`** — a migration renaming property keys (old → new) on
  the nodes with a given label. Both migrations accept pairs that swap or chain.
- **`StoredModelVersionOf`**, **`MarkedModelVersionOf`** and
  **`MigratedQueriesOf`** — the same over an `INeo4jStoredQueries`, each in its
  own session; `MigratedQueriesOf` runs the whole chain in one write
  transaction, so a failed step changes nothing.
- Dynamic labels and property keys need Neo4j 5.26 or later.
- Depends on `PureQL.CSharp.Model.Graph` `0.1.0-preview.1.0.0`,
  `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.1.0.0` and
  `Neo4j.Driver` `6.5.1`; targets .NET 11 and is AOT-compatible.
