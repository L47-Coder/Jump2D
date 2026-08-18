# Architecture Governance State

## Run
- Protocol: 2
- Status: paused
- Run ID: AG-20260819-4947b7d
- Mode: implement-and-commit
- Branch: codex/architecture-governance-20260819-4947b7d
- Round / phase / scope: R0004 / checkpoint / remove orphaned projectile recycle hooks after the pool abstraction was deleted
- Worktree ownership: R0003 checkpoint is c3c6708; this round owns only the R0004 state/plan metadata and the exact `Bullet.cs`/`CornBullet.cs` lifecycle cleanup described in the plan
- Next action: paused at the user's request after the R0004 checkpoint; resume only after explicit user direction

## Architecture
- Modules and dependencies: `Assets/Scripts/Manager` contains scene/game orchestration; `Assets/Scripts/Component` contains player, enemy, projectile, and prop behaviours; `Assets/Scripts/Common` contains shared utilities and enums; `Assets/Scripts/UI` contains UI controllers; `Assets/Scenes` and `Assets/Resources/Prefab` provide serialized composition. `MainScene` uses one GameObject named `GameManager` as a composition root with direct Manager components. The inert `GameBootManager` facade was removed in R0001, stale manager edges were removed in R0002, and the unowned `ObjectPool` helper was removed in R0003.
- Sources of truth: Unity scene/Prefab serialization owns component references and tuning values; `GameManager` owns score/state events; `Player`, `EnemyBase`, projectile, prop, and Manager components own their local runtime state. Projectile lifetime is currently owned by each projectile component through its own terminal `Destroy`; no pool owner or external callback consumer exists in the repository.
- State and lifecycles: Unity `MonoBehaviour` lifecycles, scene references, object spawning/pooling, pause/game-over flow, and resource ownership are distributed across the components. R0004 changes only the orphaned projectile recycle seam and its terminal method names; movement, animation, collision, effects, one-shot guards, and timing remain unchanged.
- External contracts: Unity 2022.3.60f1c1; build entry `Assets/Scenes/MainScene.unity`; package test framework is installed; no repository test command or documented runtime contract has been found yet. No project assembly-definition or external assembly boundary consumes the projectile hooks.
- Validation: Targeted `rg` reference/assignment checks across source, scene, Prefab, package, and assembly-definition files; Unity 2022.3.60f1c1 batchmode import/quit in the isolated validation copy; generated-project `dotnet build Assembly-CSharp.csproj --nologo` in that copy; `git diff --check`. R0004 has no remaining `OnRecycle`/`Recycle(` project references; isolated Unity script compilation/import exits batchmode with return code 0; generated C# build passes with 0 warnings/errors; `git diff --check` passes. The Unity log contains non-fatal licensing/network messages from the validation environment, but no import or compiler failure. The isolated copy is an owned, ignored validation boundary.
- Generated/vendor boundaries: `Library`, `Temp`, `Logs`, `UserSettings`, generated `*.csproj`/`*.sln`, and Unity-generated import data are not governed directly; `Assets`, `Packages`, `ProjectSettings`, and source-controlled settings remain in scope.

## Coverage
| Scope | Lens | Last round | Evidence/outcome | Revisit trigger |
|---|---|---:|---|---|
| Repository baseline | all five lenses | 0001 | Deleted inert GameBootManager facade and its scene block; no remaining source/scene GUID references; post-change `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passes with 0 warnings/errors; Unity batchmode import/quit exits 0; `git diff --check` passes | After the R0001 checkpoint, or when runtime/serialization evidence changes |
| R0002 manager dependencies | boundaries / dependencies / reduction | 0002 | Removed unused `MapManager.CameraObj` and `EnemyManager.MapManager` fields, fallback/validation branches, and MainScene edges; targeted search is empty; compile passes with 0 warnings/errors; Unity batchmode import/quit exits 0; `git diff --check` passes | When a new manager dependency or scene composition path is introduced |
| R0003 common helper | reduction / contracts | 0003 | Deleted `ObjectPool` source/meta; no remaining project reference; isolated Unity import/script compile and generated-project C# build pass with 0 warnings/errors; current projectile lifecycle still uses direct `Instantiate`/`Destroy` with separate hooks | A pool owner or caller is introduced, or the projectile lifecycle is intentionally migrated |
| R0004 projectile recycle seam | reduction / contracts / boundaries | 0004 | Validated: removed the orphaned `OnRecycle` fields/branches and renamed the private terminal state/method; no project references remain; isolated Unity import/script compile exits 0; generated C# build passes with 0 warnings/errors; `git diff --check` passes | A real pool owner, callback assignment, or external assembly contract is introduced |

## Findings
| ID | Lens | Scope | Status | Priority | Summary | Evidence |
|---|---|---|---|---:|---|---|
| AG-0001 | reduction / boundaries | `GameBootManager.cs`, `MainScene.unity` | validated | 1 | Empty bootstrap facade was attached but never executed initialization or was read by any caller; its fields duplicated direct scene composition. | Pre-change `GameBootManager.cs:3-8` contained only fields; `MainScene.unity:203-217` serialized it; post-change `rg` finds no source/scene reference to the type or GUID, Unity import exits 0, and C# compilation remains clean. |
| AG-0002 | boundaries / dependencies | `EnemyManager.cs`, `MapManager.cs`, `MainScene.unity` | validated | 2 | Two manager dependency fields were serialized and resolved but never used: `EnemyManager.MapManager` and `MapManager.CameraObj`. They created false ownership edges and validation/lookup branches. | Pre-change `EnemyManager.cs:12,25-28` only declared/assigned `MapManager`; `MapManager.cs:6,22-30` only assigned/checked `CameraObj`; post-change targeted search is empty, compile is clean, and Unity import exits 0. |
| AG-0003 | reduction / contracts | `ObjectPool.cs`, projectile lifecycle | validated | 2 | `ObjectPool` was a hollow helper with no caller or owner; leaving it advertised a capability that was not part of the runtime path. Projectile `OnRecycle` hooks remained as a separate public seam for follow-up review. | Pre-change `rg` found only `ObjectPool`'s declaration/constructor; post-change no project reference remains; isolated Unity import/script compile and generated-project C# build pass with 0 warnings/errors. |
| AG-0004 | reduction / contracts / boundaries | `Bullet.cs`, `CornBullet.cs`, projectile Prefabs | validated | 2 | After the pool helper was removed, the projectile `OnRecycle` callbacks were orphaned public seams: each was only declared and checked by its own component, with no assignment or consumer. The null branch advertised a lifecycle owner that did not exist. | Pre-change `rg` found only the declarations, local calls, and local callback checks; no assignment/read outside those files; no Prefab/scene serialization, package consumer, or `.asmdef` boundary referenced the hook. Post-change no `OnRecycle`/`Recycle(` references remain; isolated Unity import/script compilation exits 0 and generated C# build passes with 0 warnings/errors. |

## Blockers
| Finding | Missing fact/authority/tool | Attempts | Independent work remaining |
|---|---|---|---|
