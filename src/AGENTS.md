# AGENTS.md

## Fast Orientation
- Main shipped packages: `ConcurrentChunking/` (core loader), `ConcurrentChunking.Linq/` (query extensions), `ConcurrentChunking.DependencyInjection/` (factory registration).
- Core execution path is `ConcurrentChunking/ChunkedEntityLoader.cs`: count -> chunk math -> schedule producers -> `Skip/Take` query per producer -> write `Chunk<TEntity>` to channel -> consumer reads async stream.
- Ordering behavior is read-side, not producer-side: `OrderedChannelReader` buffers gaps by `ChunkIndex`; `UnorderedChannelReader` yields immediately.
- LINQ extension replays query expressions on newly created contexts (`QueryableExtensions.ApplyQueryToDbContext` + `DbSetAccessorFactory`), so expression shape and orderability must survive replay.

## Agent Modes (Use One Explicitly)
- `onboarding`: prioritize understanding and minimal safe edits; read `README.md`, then `ChunkedEntityLoader.cs`, then corresponding tests in `tests/ConcurrentChunking.Testing/`.
- `autonomous-refactor`: keep behavior identical unless requested; preserve public signatures/optional params and cancellation semantics.
- `pr-review`: prioritize regressions in ordering, cancellation, and channel completion; verify tests cover early-stop and error propagation paths.
- `feature-implementation`: add/adjust tests first in `tests/ConcurrentChunking.Testing/` base suites, then implement smallest production diff.

## Non-Negotiable Invariants
- Queries must be deterministic and uniquely ordered (`OrderBy(e => e.Id)` pattern) because paging is `Skip/Take`-based.
- `ChunkedEntityLoader<TDbContext,TEntity>` is single-use (`AssertSingleUsage()`); never introduce reuse paths.
- Effective producer concurrency is capped at `Math.Min(maxConcurrentProducerCount, maxPrefetchCount)`.
- Early consumer stop must cancel producers and still complete channel cleanly (no hangs/leaked waits).
- If `allowUncommittedReads` is set, each producer context opens `ReadUncommitted` (`BeginUncommittedReadTransactionIfRequestedAsync`).

## Where To Change What
- Concurrency/backpressure/cancellation: `ConcurrentChunking/ChunkedEntityLoader.cs`.
- Ordered vs unordered output behavior: `ConcurrentChunking/OrderedChannelReader.cs`, `ConcurrentChunking/UnorderedChannelReader.cs`.
- LINQ validation/replay rules: `ConcurrentChunking.Linq/QueryableExtensions.cs`, `QueryExpressionChecker.cs`, `EntityQueryRootExpressionExtractor.cs`.
- DI integration surface: `ConcurrentChunking.DependencyInjection/ServiceProviderExtensions.cs`, `ChunkedEntityLoaderFactory.cs`.

## Build/Test Workflow (Repo-Specific)
- Run commands from `src/`.
- CI sequence in `.github/workflows/ci-cd.yml`: restore -> build (Release) -> all test projects -> pack.
- Test projects are forced to `net10.0` by `Directory.Build.targets`.
- Quick checks:
  - `dotnet test src/tests/ConcurrentChunking.Tests/ConcurrentChunking.Tests.csproj -c Release`
  - `dotnet test src/tests/ConcurrentChunking.DependencyInjection.Tests/ConcurrentChunking.DependencyInjection.Tests.csproj -c Release`
  - `dotnet test src/tests/ConcurrentChunking.Linq.Tests/ConcurrentChunking.Linq.Tests.csproj -c Release`
- Integration suites (`ConcurrentChunking.IntegrationTests`, `ConcurrentChunking.Linq.IntegrationTests`) rely on Docker/Testcontainers SQL Server.

## Change Protocol (High-Safety Edits)
- For `ChunkedEntityLoader.cs`, treat semaphore release paths as critical: check both success and exception/cancellation branches.
- Preserve `TryComplete(completionException)` behavior so producer failures propagate to consumers.
- When touching LINQ replay code, ensure result remains `IOrderedQueryable<T>` after reconstruction (`ApplyQueryToDbContext`).
- Do not weaken explicit order validation (`QueryExpressionChecker.HasOrderBy`) unless tests and API behavior intentionally change.

## Test Infrastructure Patterns
- Shared scaffolding is in `tests/ConcurrentChunking.Testing/` (`TestBase`, `ChunkedEntityLoaderTestBase`, `OrderedQueryableExtensionsTestBase`).
- xUnit v3 assembly fixtures bootstrap data/container startup via `tests/*/Properties/AssemblyInfo.cs`.
- `IntegrationTestStartupFixture` keeps SQL container running for Rider/Visual Studio sessions; CLI runs tear it down.
- Integration dataset is intentionally large (`100_001` rows) and seeded with `SqlBulkCopy` in `SqlServerTestData`.

## Review Hotspots (Before Merging)
- `LoadAsync` early-stop flow in `ChunkedEntityLoader`: consumer exit must cancel producers without deadlock.
- `_prefetchLimiterSemaphore.Release()` in `ProduceAsync` catch blocks: removing this can hang loader completion.
- Ordered buffering logic in `OrderedChannelReader`: missing index handling must still throw ordering violation.
- Public API surface in `IChunkedEntityLoader`, `IChunkedEntityLoaderFactory`, and `QueryableExtensions`: optional params are contract-sensitive.

## Conventions To Preserve
- Analyzer policy is strict (`TreatWarningsAsErrors=true`); `NoWarn` list in `Directory.Build.props` is intentional.
- Central package versions live in `Directory.Packages.props` (do not version-bump in individual `.csproj` files).
- Optional parameters in public APIs are deliberate; avoid overload explosion unless specifically requested.
- Packaging wiring in shipped projects depends on `README.md`, `LICENSE.txt`, and `icon.png` being pack-included.

## Practical Smoke-Test Entry Points
- `Playground/Program.cs` and `PlaygroundWithNugetPackage/Program.cs` are fastest manual behavior checks.



