# PureQL.CSharp.Model.Graph.Neo4j.Migration

Migrates **PureQL** query graphs stored in Neo4j from one model version to the next.

[![.NET build & test](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Migration/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Migration/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Migration/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Migration/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/PureQL.CSharp.Model.Graph.Neo4j.Migration)](https://www.nuget.org/packages/PureQL.CSharp.Model.Graph.Neo4j.Migration)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

`PureQL.CSharp.Model.Graph.Neo4j.Migration` is the operation package of the
`PureQL.CSharp.Model.Graph` family that keeps stored query graphs in step with
[`PureQL.CSharp.Model`](https://github.com/kudima03/PureQL.CSharp.Model): when a new model
version renames a record or a property, a migration rewrites the graphs written with the
previous version.

## Public API

Namespace `PureQL.CSharp.Model.Graph.Neo4j.Migration`:

| Name | Description |
|------|-------------|
| `ModelVersion` | `sealed record ModelVersion(string Value)`: a `PureQL.CSharp.Model` version such as `0.1.0-preview.14.0.0`; the value must not be empty |
| `IGraphMigration` | A change of the stored graphs from the model version `From` to `To`, applied on an `IAsyncQueryRunner` |
| `StoredModelVersion` | `(IAsyncQueryRunner Runner)`: `ValueAsync` reads the version of the single `PureQLModelVersion` node, `null` when none is stored; more than one throws |
| `MarkedModelVersion` | `(IAsyncQueryRunner Runner, ModelVersion Version)`: `ApplyAsync` merges the single marker node and sets its `value` |
| `MigratedQueries` | `(IAsyncQueryRunner Runner, IEnumerable<IGraphMigration> Migrations)`: `ApplyAsync` applies the migration starting at the stored version, marks its `To`, and repeats until none starts at the current version; throws when no marker is stored, two migrations share a `From` or the chain loops |
| `RelabeledRecords` | `(ModelVersion From, ModelVersion To, IReadOnlyDictionary<string, string> Labels)`: an `IGraphMigration` replacing each old label by its new one |
| `RenamedProperties` | `(ModelVersion From, ModelVersion To, string Label, IReadOnlyDictionary<string, string> Properties)`: an `IGraphMigration` moving each old property of the nodes with `Label` to its new name |
| `StoredModelVersionOf`, `MarkedModelVersionOf`, `MigratedQueriesOf` | The same over an `INeo4jStoredQueries`: each opens a session on its driver for its database; `MigratedQueriesOf` runs the whole chain in one write transaction |

Labels and property keys are dynamic (`$(...)`, `n[key]`), so the server must be Neo4j 5.26
or later.

## Dependencies

- [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) `0.1.0-preview.1.0.0`
- [`PureQL.CSharp.Model.Graph.Neo4j.Abstractions`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Abstractions) `0.1.0-preview.1.0.0`
- [`Neo4j.Driver`](https://www.nuget.org/packages/Neo4j.Driver) `6.5.1`

## Target Frameworks

- .NET 11

## Installation

```
dotnet add package PureQL.CSharp.Model.Graph.Neo4j.Migration
```

## Usage

```csharp
using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Graph.Neo4j.Migration;

INeo4jStoredQueries store = new Neo4jStoredQueries(driver, "neo4j");

// Once, after writing the first graphs: the model version they were written with.
await new MarkedModelVersionOf(store, new ModelVersion("0.1.0-preview.14.0.0")).ApplyAsync();

// On upgrade: every migration from the stored version on, in one transaction.
await new MigratedQueriesOf(
    store,
    [
        new RelabeledRecords(
            new ModelVersion("0.1.0-preview.14.0.0"),
            new ModelVersion("0.1.0-preview.15.0.0"),
            new Dictionary<string, string> { ["OldRecord"] = "NewRecord" }
        ),
        new RenamedProperties(
            new ModelVersion("0.1.0-preview.15.0.0"),
            new ModelVersion("0.1.0-preview.16.0.0"),
            "NewRecord",
            new Dictionary<string, string> { ["Old"] = "New" }
        ),
    ]
).ApplyAsync();

ModelVersion? version = await new StoredModelVersionOf(store).ValueAsync();
// 0.1.0-preview.16.0.0
```

## Related packages

- [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) — maps a `PureQLQuery` to a property graph and back, without a driver
- [`PureQL.CSharp.Model.Graph.Neo4j.Abstractions`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Abstractions) — the stored-queries contract
- [`PureQL.CSharp.Model.Graph.Neo4j.Creation`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Creation) — constraints and indexes
- [`PureQL.CSharp.Model.Graph.Neo4j.Insertion`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion) — writes a query
- [`PureQL.CSharp.Model.Graph.Neo4j.Querying`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Querying) — reads queries back
- [`PureQL.CSharp.Model.Graph.Neo4j.Deletion`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Deletion) — deletes queries
- [`PureQL.CSharp.Model.Graph.Neo4j.Updating`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Updating) — replaces a query in one transaction
- [`PureQL.CSharp.Model.Graph.Neo4j.Existence`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Existence) — checks the schema and queries exist
- [`PureQL.CSharp.Model.Graph.Neo4j.Destruction`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Destruction) — drops the schema and every stored query
