**LeXtudio.Metadata.Mutable — Design Overview**

- **Purpose:** Consolidate duplicated mutable metadata/IL model and reader/writer code into a single shared library so multiple consumers (wxsg, Obfuscar, others) can reuse the same implementation.
- **Location:** `AssemblyTools/src/LeXtudio.Metadata.Mutable`
- **NuGet/TFM choices:** library targets `net8.0`. Tests that exercise PersistedAssemblyBuilder require `net9.0` and live under `AssemblyTools/tests/LeXtudio.Metadata.Mutable.Tests`.

**Project Structure**

- `Metadata/Abstractions` — small interfaces and helper types that describe a minimal metadata model surface (types, methods, fields, modules, assemblies). These were preserved from the original sources but renamed to `LeXtudio.Metadata.Abstractions`.
- `Metadata/Mutable` — the mutable object model (types, methods, instructions, IL processor), plus `MutableAssemblyReader` and `MutableAssemblyWriter` which implement PE/metadata read and write using `System.Reflection.Metadata`.
- `Support` — minimal compatibility shims introduced to avoid pulling the entire application runtime into the shared library. Currently contains `LoggerService` (thin wrapper around `Microsoft.Extensions.Logging.NullLogger`) and `Helper` helpers.

**Namespaces and Compatibility**

- Public namespaces in the shared project use the `LeXtudio.Metadata.*` prefix (e.g. `LeXtudio.Metadata.Mutable`, `LeXtudio.Metadata.Abstractions`). This avoids leaking the original `Obfuscar.*` application namespace and makes the assembly clearly reusable.
- To ease migration, small shims (in `Support`) are provided. If a consumer still expects `Obfuscar.*` types, consider one of:
	- Adding a tiny compatibility package or wrapper assembly with type forwards from `Obfuscar.*` → `LeXtudio.Metadata.*`.
	- Multi-targeting the shared library (e.g. add `netstandard2.0` or `net462`) if legacy consumers require older runtimes.

**Important Implementation Notes**

- The mutable reader/writer implement low-level encoding/decoding of method bodies, tokens, and metadata tables; they rely on `System.Reflection.Metadata` and `ManagedPEBuilder` patterns.
- The type system (`MutableTypeSystem`) provides import helpers for CLR types and caches imports for performance.
- Nullability warnings remain present across moved code (these were existing and not fully cleaned up during the extraction). They do not block compilation but should be triaged as a follow-up.

**Testing**

- Tests live in `AssemblyTools/tests/LeXtudio.Metadata.Mutable.Tests`. Two important tests are included:
	- `RoundtripTests` — basic import test for `MutableTypeSystem`.
	- `MutableAssemblyWriterRoundTripTests` — round-trip through reader/writer using `PersistedAssemblyBuilder` (hence the test project targets `net9.0`).

**Migration / Consumer Integration Guidance**

- For modern consumers targeting `net8.0` or later: add a `ProjectReference` to `AssemblyTools/src/LeXtudio.Metadata.Mutable/LeXtudio.Metadata.Mutable.csproj` and update `using` directives to `LeXtudio.Metadata.*`.
- For legacy consumers (multi-targeting `net462`, `netstandard`, etc.): either
	- Multi-target `LeXtudio.Metadata.Mutable` to include a compatible TFM (recommended if you want a single shared package consumers can reference), or
	- Keep legacy files in the consumer until you can incrementally migrate and add conditional `ProjectReference` entries for modern TFMs.

**Known Issues & Next Steps**

- There are numerous nullability and SYSLIB warnings inherited from the moved code; triage and fix in follow-up PRs.
- Decide whether to provide `Obfuscar.*` → `LeXtudio.Metadata.*` compatibility wrappers or convert consumers to the new namespaces. I can add type-forwards or a small wrapper assembly if you want.
- Add documentation examples showing how to reference and call into `MutableAssemblyReader`/`MutableAssemblyWriter` from a consumer project.

**Contact / Ownership**

- Owner: LeXtudio team (for consolidation and future maintenance).
- Where to change: `AssemblyTools/src/LeXtudio.Metadata.Mutable` for code; `AssemblyTools/tests/LeXtudio.Metadata.Mutable.Tests` for tests; `AssemblyTools/docs/design.md` for design notes (this file).

