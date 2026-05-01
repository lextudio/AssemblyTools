# AssemblyTools

LeXtudio.Metadata.Mutable — a small, reusable library for reading, mutating, and
writing .NET assembly metadata and IL. This project consolidates mutable
metadata/IL model code so tools like Obfuscar and other consumers can reuse a
single, well-tested implementation.

## Overview

- Purpose: Provide a mutable object model for metadata and method bodies plus
	reader/writer implementations built on `System.Reflection.Metadata`.
- Primary project: `src/LeXtudio.Metadata.Mutable`.
- Supported TFMs: `net8.0` (primary) and `net462` (legacy compatibility).

## Key features

- Mutable metadata and IL model (types, methods, fields, instructions).
- `MutableAssemblyReader` / `MutableAssemblyWriter` for PE/metadata read/write.
- Designed for reuse across multiple tooling projects; lightweight compatibility
	shims are provided in `Support`.
- Tests demonstrating round-trip behavior are included.

Currently used in both [Obfuscar](https://github.com/obfuscar/obfuscar) and [WXSG](https://github.com/lextudio/wxsg) projects.

## Getting started

Add the NuGet package from nuget.org (preferred):

`dotnet add <your-project>.csproj package LeXtudio.Metadata.Mutable`

If you need to test locally before publishing, pack the library and add the
local folder as a package source:

`dotnet pack src/LeXtudio.Metadata.Mutable/LeXtudio.Metadata.Mutable.csproj -c Release`

`dotnet nuget add source "src\LeXtudio.Metadata.Mutable\bin\Release" -n local-pkgs`

`dotnet add <your-project>.csproj package LeXtudio.Metadata.Mutable --source local-pkgs`

Build the library (example, `net8.0`):

`dotnet build src/LeXtudio.Metadata.Mutable/LeXtudio.Metadata.Mutable.csproj -f net8.0`

Run the tests:

`dotnet test tests/LeXtudio.Metadata.Mutable.Tests/LeXtudio.Metadata.Mutable.Tests.csproj`

## Usage

- Reference the [![NuGet](https://img.shields.io/nuget/v/LeXtudio.Metadata.Mutable.svg?label=LeXtudio.Metadata.Mutable&&style=flat-square)](https://www.nuget.org/packages/LeXtudio.Metadata.Mutable)
 package. Use the public API surface under the `LeXtudio.Metadata.Mutable`
	namespace — for example the reader/writer classes (`MutableAssemblyReader`,
	`MutableAssemblyWriter`), `MutableTypeSystem`, and `PersistedAssemblyBuilder`.
- Concrete usage examples are available in the test projects; see:
	- [tests/LeXtudio.Metadata.Mutable.Tests/MutableAssemblyWriterRoundTripTests.cs](tests/LeXtudio.Metadata.Mutable.Tests/MutableAssemblyWriterRoundTripTests.cs)
	- [tests/LeXtudio.Metadata.Mutable.Tests/RoundtripTests.cs](tests/LeXtudio.Metadata.Mutable.Tests/RoundtripTests.cs)

## Documentation

Design notes and rationale: [docs/design.md](docs/design.md)

## Supported Target Frameworks

- `net8.0` (primary)
- `net462` (legacy compatibility)

Note: Some tests exercise `net9.0` features and are located under the
`tests` folder.

## Contributing

- Fork, implement changes in a feature branch, and open a pull request.
- Run the relevant tests locally (`dotnet test ...`) and include tests for
	new behavior.
- Keep changes focused and add documentation when you change public APIs.

## License

This repository is licensed under the MIT License — see [LICENSE](LICENSE).
