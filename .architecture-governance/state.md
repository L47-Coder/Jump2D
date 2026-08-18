# Architecture Governance State

## Run
- Protocol: 2
- Status: active
- Run ID: AG-20260819-4947b7d
- Mode: implement-and-commit
- Branch: codex/architecture-governance-20260819-4947b7d
- Round / phase / scope: R0002 / checkpoint / remove stale manager dependency fields that have no runtime consumers
- Worktree ownership: R0001 checkpoint is 5778d86; this round owns only the R0002 state/plan metadata and the exact `MapManager`/`EnemyManager`/`MainScene` hunks described in the plan
- Next action: stage only the owned files, review the staged diff, and create the R0002 checkpoint commit

## Architecture
- Modules and dependencies: `Assets/Scripts/Manager` contains scene/game orchestration; `Assets/Scripts/Component` contains player, enemy, projectile, and prop behaviours; `Assets/Scripts/Common` contains shared utilities and enums; `Assets/Scripts/UI` contains UI controllers; `Assets/Scenes` and `Assets/Resources/Prefab` provide serialized composition. `MainScene` uses one GameObject named `GameManager` as a composition root with direct Manager components. The inert `GameBootManager` facade was removed in R0001.
- Sources of truth: Unity scene/Prefab serialization owns component references and tuning values; `GameManager` owns score/state events; `Player`, `EnemyBase`, projectile, prop, and Manager components own their local runtime state. `CameraManager.MapManager` is an active dependency; `MapManager.CameraObj` and `EnemyManager.MapManager` are not consumed after assignment and are the R0002 stale edges.
- State and lifecycles: Unity `MonoBehaviour` lifecycles, scene references, object spawning/pooling, pause/game-over flow, and resource ownership are distributed across the components. R0002 removes only unused validation/lookup state and serialized edges; camera movement, background creation, enemy spawning, and their timing paths remain unchanged.
- External contracts: Unity 2022.3.60f1c1; build entry `Assets/Scenes/MainScene.unity`; package test framework is installed; no repository test command or documented runtime contract has been found yet.
- Validation: Targeted `rg` call/reference check; `dotnet build Assembly-CSharp.csproj --no-restore --nologo`; Unity 2022.3.60f1c1 batchmode import/quit; `git diff --check`. R0001 checks passed with no warnings/errors; R0002 must preserve that baseline.
- Generated/vendor boundaries: `Library`, `Temp`, `Logs`, `UserSettings`, generated `*.csproj`/`*.sln`, and Unity-generated import data are not governed directly; `Assets`, `Packages`, `ProjectSettings`, and source-controlled settings remain in scope.

## Coverage
| Scope | Lens | Last round | Evidence/outcome | Revisit trigger |
|---|---|---:|---|---|
| Repository baseline | all five lenses | 0001 | Deleted inert GameBootManager facade and its scene block; no remaining source/scene GUID references; post-change `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passes with 0 warnings/errors; Unity batchmode import/quit exits 0; `git diff --check` passes | After the R0001 checkpoint, or when runtime/serialization evidence changes |
| R0002 manager dependencies | boundaries / dependencies / reduction | 0002 | Removed unused `MapManager.CameraObj` and `EnemyManager.MapManager` fields, fallback/validation branches, and MainScene edges; targeted search is empty; compile passes with 0 warnings/errors; Unity batchmode import/quit exits 0; `git diff --check` passes | When a new manager dependency or scene composition path is introduced |

## Findings
| ID | Lens | Scope | Status | Priority | Summary | Evidence |
|---|---|---|---|---:|---|---|
| AG-0001 | reduction / boundaries | `GameBootManager.cs`, `MainScene.unity` | validated | 1 | Empty bootstrap facade was attached but never executed initialization or was read by any caller; its fields duplicated direct scene composition. | Pre-change `GameBootManager.cs:3-8` contained only fields; `MainScene.unity:203-217` serialized it; post-change `rg` finds no source/scene reference to the type or GUID, Unity import exits 0, and C# compilation remains clean. |
| AG-0002 | boundaries / dependencies | `EnemyManager.cs`, `MapManager.cs`, `MainScene.unity` | validated | 2 | Two manager dependency fields were serialized and resolved but never used: `EnemyManager.MapManager` and `MapManager.CameraObj`. They created false ownership edges and validation/lookup branches. | Pre-change `EnemyManager.cs:12,25-28` only declared/assigned `MapManager`; `MapManager.cs:6,22-30` only assigned/checked `CameraObj`; post-change targeted search is empty, compile is clean, and Unity import exits 0. |
| AG-0003 | reduction / contracts | `ObjectPool.cs`, projectile lifecycle | observed | 2 | `ObjectPool` has no in-repository caller while projectiles expose unused `OnRecycle` seams; deletion or adoption needs an explicit contract decision before changing public surface. | `rg` found only the `ObjectPool` declaration/constructor and no callers; `Bullet.cs` and `CornBullet.cs` define `OnRecycle` but current Managers/Player use `Instantiate`. |

## Blockers
| Finding | Missing fact/authority/tool | Attempts | Independent work remaining |
|---|---|---|---|
