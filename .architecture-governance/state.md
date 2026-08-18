# Architecture Governance State

## Run
- Protocol: 2
- Status: active
- Run ID: AG-20260819-4947b7d
- Mode: implement-and-commit
- Branch: codex/architecture-governance-20260819-4947b7d
- Round / phase / scope: R0003 / checkpoint / delete the unreferenced ObjectPool helper
- Worktree ownership: R0002 checkpoint is 60da442; this round owns only the R0003 state/plan metadata and the exact `ObjectPool.cs` source/meta deletion described in the plan
- Next action: stage only the owned files, review the staged diff, and create the R0003 checkpoint commit

## Architecture
- Modules and dependencies: `Assets/Scripts/Manager` contains scene/game orchestration; `Assets/Scripts/Component` contains player, enemy, projectile, and prop behaviours; `Assets/Scripts/Common` contains shared utilities and enums; `Assets/Scripts/UI` contains UI controllers; `Assets/Scenes` and `Assets/Resources/Prefab` provide serialized composition. `MainScene` uses one GameObject named `GameManager` as a composition root with direct Manager components. The inert `GameBootManager` facade was removed in R0001, and stale manager edges were removed in R0002.
- Sources of truth: Unity scene/Prefab serialization owns component references and tuning values; `GameManager` owns score/state events; `Player`, `EnemyBase`, projectile, prop, and Manager components own their local runtime state. `ObjectPool` has no caller or owner in the current assembly, while projectile `OnRecycle` hooks are separate public seams and remain outside R0003.
- State and lifecycles: Unity `MonoBehaviour` lifecycles, scene references, object spawning/pooling, pause/game-over flow, and resource ownership are distributed across the components. R0002 removes only unused validation/lookup state and serialized edges; camera movement, background creation, enemy spawning, and their timing paths remain unchanged.
- External contracts: Unity 2022.3.60f1c1; build entry `Assets/Scenes/MainScene.unity`; package test framework is installed; no repository test command or documented runtime contract has been found yet.
- Validation: Targeted `rg` reference check across source, scene, Prefab, package, and assembly-definition files; Unity 2022.3.60f1c1 batchmode import/quit in an isolated copy; generated-project `dotnet build Assembly-CSharp.csproj --nologo` in that copy; `git diff --check`. The isolated Unity import/script compile and direct C# build both pass with 0 warnings/errors. The live project's ignored `Assembly-CSharp.csproj` remains stale only because its user Unity instance is open; it is outside the committed source boundary.
- Generated/vendor boundaries: `Library`, `Temp`, `Logs`, `UserSettings`, generated `*.csproj`/`*.sln`, and Unity-generated import data are not governed directly; `Assets`, `Packages`, `ProjectSettings`, and source-controlled settings remain in scope.

## Coverage
| Scope | Lens | Last round | Evidence/outcome | Revisit trigger |
|---|---|---:|---|---|
| Repository baseline | all five lenses | 0001 | Deleted inert GameBootManager facade and its scene block; no remaining source/scene GUID references; post-change `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passes with 0 warnings/errors; Unity batchmode import/quit exits 0; `git diff --check` passes | After the R0001 checkpoint, or when runtime/serialization evidence changes |
| R0002 manager dependencies | boundaries / dependencies / reduction | 0002 | Removed unused `MapManager.CameraObj` and `EnemyManager.MapManager` fields, fallback/validation branches, and MainScene edges; targeted search is empty; compile passes with 0 warnings/errors; Unity batchmode import/quit exits 0; `git diff --check` passes | When a new manager dependency or scene composition path is introduced |
| R0003 common helper | reduction / contracts | 0003 | Deleted `ObjectPool` source/meta; no remaining project reference; isolated Unity import/script compile and generated-project C# build pass with 0 warnings/errors; current projectile lifecycle still uses direct `Instantiate`/`Destroy` with separate hooks | A pool owner or caller is introduced, or the projectile lifecycle is intentionally migrated |

## Findings
| ID | Lens | Scope | Status | Priority | Summary | Evidence |
|---|---|---|---|---:|---|---|
| AG-0001 | reduction / boundaries | `GameBootManager.cs`, `MainScene.unity` | validated | 1 | Empty bootstrap facade was attached but never executed initialization or was read by any caller; its fields duplicated direct scene composition. | Pre-change `GameBootManager.cs:3-8` contained only fields; `MainScene.unity:203-217` serialized it; post-change `rg` finds no source/scene reference to the type or GUID, Unity import exits 0, and C# compilation remains clean. |
| AG-0002 | boundaries / dependencies | `EnemyManager.cs`, `MapManager.cs`, `MainScene.unity` | validated | 2 | Two manager dependency fields were serialized and resolved but never used: `EnemyManager.MapManager` and `MapManager.CameraObj`. They created false ownership edges and validation/lookup branches. | Pre-change `EnemyManager.cs:12,25-28` only declared/assigned `MapManager`; `MapManager.cs:6,22-30` only assigned/checked `CameraObj`; post-change targeted search is empty, compile is clean, and Unity import exits 0. |
| AG-0003 | reduction / contracts | `ObjectPool.cs`, projectile lifecycle | validated | 2 | `ObjectPool` was a hollow helper with no caller or owner; leaving it advertised a capability that was not part of the runtime path. Projectile `OnRecycle` hooks remain unchanged as a separate public-contract question. | Pre-change `rg` found only `ObjectPool`'s declaration/constructor; post-change no project reference remains; isolated Unity import/script compile and generated-project C# build pass with 0 warnings/errors. |

## Blockers
| Finding | Missing fact/authority/tool | Attempts | Independent work remaining |
|---|---|---|---|
