# Architecture Governance State

## Run
- Protocol: 2
- Status: active
- Run ID: AG-20260819-4947b7d
- Mode: implement-and-commit
- Branch: codex/architecture-governance-20260819-4947b7d
- Round / phase / scope: R0001 / checkpoint / delete the unused GameBootManager facade from the scene composition
- Worktree ownership: baseline was clean at 4947b7d on main; this run owns `.architecture-governance/*`, the deleted GameBootManager source/meta, and the exact MainScene component removal
- Next action: create the R0001 checkpoint commit after the staged diff review

## Architecture
- Modules and dependencies: `Assets/Scripts/Manager` contains scene/game orchestration; `Assets/Scripts/Component` contains player, enemy, projectile, and prop behaviours; `Assets/Scripts/Common` contains shared utilities and enums; `Assets/Scripts/UI` contains UI controllers; `Assets/Scenes` and `Assets/Resources/Prefab` provide serialized composition. `MainScene` uses one GameObject named `GameManager` as a composition root with direct Manager components.
- Sources of truth: Unity scene/Prefab serialization owns component references and tuning values; `GameManager` owns score/state events; `Player`, `EnemyBase`, projectile, prop, and Manager components own their local runtime state. `GameBootManager` has no state reads or writes and is not a source of truth.
- State and lifecycles: Unity `MonoBehaviour` lifecycles, scene references, object spawning/pooling, pause/game-over flow, and resource ownership are distributed across the components. The selected batch removes only an inert scene component, so these lifecycles remain unchanged.
- External contracts: Unity 2022.3.60f1c1; build entry `Assets/Scenes/MainScene.unity`; package test framework is installed; no repository test command or documented runtime contract has been found yet.
- Validation: Targeted `rg` reference check; `dotnet build Assembly-CSharp.csproj --no-restore --nologo`; Unity 2022.3.60f1c1 batchmode import/quit; `git diff --check`. All R0001 checks pass with no warnings/errors and no remaining GameBootManager GUID reference.
- Generated/vendor boundaries: `Library`, `Temp`, `Logs`, `UserSettings`, generated `*.csproj`/`*.sln`, and Unity-generated import data are not governed directly; `Assets`, `Packages`, `ProjectSettings`, and source-controlled settings remain in scope.

## Coverage
| Scope | Lens | Last round | Evidence/outcome | Revisit trigger |
|---|---|---:|---|---|
| Repository baseline | all five lenses | 0001 | Deleted inert GameBootManager facade and its scene block; no remaining source/scene GUID references; post-change `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passes with 0 warnings/errors; Unity batchmode import/quit exits 0; `git diff --check` passes | After the R0001 checkpoint, or when runtime/serialization evidence changes |

## Findings
| ID | Lens | Scope | Status | Priority | Summary | Evidence |
|---|---|---|---|---:|---|---|
| AG-0001 | reduction / boundaries | `GameBootManager.cs`, `MainScene.unity` | validated | 1 | Empty bootstrap facade was attached but never executed initialization or was read by any caller; its fields duplicated direct scene composition. | Pre-change `GameBootManager.cs:3-8` contained only fields; `MainScene.unity:203-217` serialized it; post-change `rg` finds no source/scene reference to the type or GUID, Unity import exits 0, and C# compilation remains clean. |
| AG-0002 | boundaries / dependencies | `CameraManager.cs`, `EnemyManager.cs`, `MapManager.cs`, `MainScene.unity` | ready | 2 | Manager references are both serialized in the composition root and recovered through `FindObjectOfType` fallbacks, leaving two dependency paths. | `MainScene.unity:215-230` serializes `MapManager`; `CameraManager.cs:33-43` and `EnemyManager.cs:25-32` independently discover it when absent. |
| AG-0003 | reduction / contracts | `ObjectPool.cs`, projectile lifecycle | observed | 2 | `ObjectPool` has no in-repository caller while projectiles expose unused `OnRecycle` seams; deletion or adoption needs an explicit contract decision before changing public surface. | `rg` found only the `ObjectPool` declaration/constructor and no callers; `Bullet.cs` and `CornBullet.cs` define `OnRecycle` but current Managers/Player use `Instantiate`. |

## Blockers
| Finding | Missing fact/authority/tool | Attempts | Independent work remaining |
|---|---|---|---|
