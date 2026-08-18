# Architecture Governance R001

## 目标与边界

本轮目标是降低运行时代码熵：减少重复状态、重复访问入口、无效抽象和无效公开 API；保持现有游戏行为与 Unity 序列化兼容性。项目事实来自 5 个只读侦察方向的完整扫描：25 个 C# 文件、1 个场景、8 个 prefab、Packages、ProjectSettings、资源及设计文档。侦察阶段未修改文件、未读取 Git 历史。

修复子代理必须完成“本轮实施清单”，并在修改后完成编译级静态验证和可执行的 Unity 验证。`.architecture-governance/plan.md` 之外的现有治理状态文件不属于本轮输入，不应读取其内容。

## 审查结果汇总

### 1. 死代码与过宽 API

1. `Assets/Scripts/Common/Tween.cs` 的 `Tween.Punch(MonoBehaviour, Transform, float, float)` 便捷重载只被自身转发；唯一外部调用使用带 `restScale` 的重载。可删除便捷重载。
2. `Assets/Scripts/Manager/CameraManager.cs` 的 `TryGetPosition` 只被同类 `TryGetSpawnPosition` 调用，可改为私有辅助或内联；外部只保留生成点入口。
3. `Assets/Scripts/Component/Player/Player.cs` 的 `AlignBodyHorizontally` 只由 `Player` 自己调用，应从 `public` 收窄为 `private`。
4. `Assets/Scripts/Component/EnemyBase.cs` 的 `StartIdleBob` 与 `SpawnDeathCorpse` 默认空实现，当前仅有 `Enemy_1`、`Enemy_2` 两个子类且均覆盖，应改为抽象钩子，避免无效默认行为。
5. `Assets/Scripts/UI/GameManagerBinding.cs` 的 `OnManagerBound` 当前默认空实现，现有 `ScoreHUD`、`PauseController`、`GameOverPanel` 均覆盖，应改为抽象契约。
6. `Enemy_2` prefab 仍保存基类 `Body` 引用，但 `Enemy_2.cs` 不读取该字段；只能在确认 prefab 组件层级不依赖它后清理该孤立序列化值，不能删除 `EnemyBase.Body`，因为 `Enemy_1` 仍使用它。
7. 7 张展示/参考 PNG 未发现任何场景、prefab、GUID 或代码加载引用：`背景参考.png`、`怪物2整体版.png`、`怪物展示.png`、`豌豆效果展示.png`、`主角形态展示.png`、`气泡效果.png`、`重新开始页面.png`。它们是资源清理候选，不应在未确认设计文档依赖前直接删除。

### 2. 重复状态与等价访问点

1. `CameraManager._elapsed/DifficultyRampDuration`、`EnemyManager._elapsed/DifficultyRampDuration`、`PropsManager._elapsed/DifficultyRampDuration` 是同一难度进度的 3 份状态；`MainScene.unity` 在 217、237、270 行重复配置为 90。
2. `EnemyManager.SpawnAheadDistance` 与 `PropsManager.SpawnAheadDistance` 都是 10，且都通过 `CameraManager.TryGetSpawnPosition` 生成；生成前方距离应由相机/生成点入口单独持有。
3. `GameManager.Instance` 与 `GameManagerBinding.Manager` 目前形成业务直接访问和 UI 订阅缓存两种路径。UI 缓存是事件解绑所需的本地生命周期状态，本轮不强行替换为静态单例；业务访问入口保持现状，避免引入未经场景注入验证的组合根。
4. `PlayerManager.TargetPosObj -> Player.SetTargetPosObj` 已有显式注入，但 `Player.Start` 缺失时静默回退为自身；这是两个等价来源，应在后续验证 prefab/场景生命周期后改为缺失时报错并禁用，不在本轮改变运行时容错语义。
5. `GameManagerBinding.OnEnable` 与 `Start` 都尝试绑定，但前者覆盖重新启用生命周期，后者覆盖 GameManager Awake 顺序不确定性；两者不是可直接删除的等价入口，本轮保留并只收窄绑定契约。
6. `Player.TryJump` 分别调用 `IsJumpPressedThisFrame` 与 `IsJumpPointerOverUI`，鼠标和触摸被重复读取，应收敛为一次读取并完成 UI 拦截。
7. `Bullet.OnTriggerEnter2D` 与 `CornBullet.OnTriggerEnter2D` 都执行碰撞体解析和投射物结束；`ProjectileBase` 已是共同生命周期入口，应把敌人命中入口上移到基类，子类只实现命中效果。
8. `Player`、`Props`、`PropsManager` 对同一 `WeaponType` 的投射物、射速、嘴部 Sprite、道具图标存在分散映射；统一 `WeaponDefinition/WeaponCatalog` 需要同步 prefab 序列化字段，本轮先不做破坏性迁移，第二轮继续复查。
9. `PlayerBody` 与 `EnemyCorpse` 的地面接触判断都检查 `BackGround` 与 `normal.y > 0.25f`，但一处使用 `collision.gameObject`、另一处使用 `collision.collider`，层级语义尚未完全等价；本轮不抽取可能改变碰撞行为的公共解析器。

### 3. 重复实现与数据结构熵

1. `EnemyManager.EnemySpawnConfig` 只有一个 `GameObject Prefab` 字段，场景中只保存两个 prefab；它是可去掉一层包装的候选。必须同步处理 `MainScene.unity` 的序列化字段，不能只改 C#。
2. `EnemyManager.GenerateEnemyBatch` 每次生成都执行 `FindAll`，应在 `Awake` 过滤并缓存有效 prefab。
3. `Enemy_2.SpawnDeathCorpse` 对头部和身体重复构造 `CorpseLaunchSpec`；其头/身体各有 6 个相似参数。`Enemy_1` 也共享同类尸块配置。可以通过数据驱动部件列表降低重复，但需要 prefab 字段迁移，本轮先保留字段、只合并已安全的参数传递路径。
4. `EnemyBase.CorpseLaunchSpec` 已封装 6 个发射参数，但 `EnemyBase.SpawnCorpse` 又逐字段展开传入 `EnemyCorpse.Create` 的长参数列表；应在不破坏 Unity 反射/序列化的前提下收敛为一个运行时传输对象。
5. `PropsManager`、`MapManager`、`Enemy_1`、`CornExplosionSettings` 多处使用成对的 Min/Max 字段和边界规范化；可后续抽取 `FloatRange`，本轮不为少量配置新增全局泛型抽象。
6. `EnemyBody` 是空组件。它仍被 `EnemyBase.Body` 与 `Enemy_1` 用于查找/浮动，且 prefab 中存在组件，因此不是可直接删除的死类型；本轮不删除。
7. `CameraManager.TryGetPosition` 仅服务于 `TryGetSpawnPosition`，列入本轮删除/收窄。
8. `Props.OnTriggerEnter2D` 直接 `GetComponentInParent<Player>()`，而敌人碰撞已使用 `EnemyTargetResolver`；可新增 Player 解析入口，但本轮先不为单个调用点扩展类型体系。

### 4. 文件结构与项目卫生

1. `Assets/Resources` 同时承载 8 个 prefab 与 38 张纹理，代码没有 `Resources.Load`；资源主要由场景/prefab GUID 直接引用。可按 `Prefabs`、`Art`、`UI` 迁移，但需要同步 Unity 资源文件及 `.meta`，本轮不做大规模移动。
2. `Assets/Scripts/Component` 混放普通组件、抽象基类、静态解析器和配置类型；多个文件还包含多个公共类型。可按职责拆分，但涉及文件与 Unity 脚本 meta 迁移，本轮不做批量重排。
3. `MainScene.unity` 同时承载所有运行时管理器和完整 UI；HUD、暂停、结束面板可抽成 prefab，但需要 Unity 编辑器序列化验证，本轮不做大场景拆分。
4. `Library`、`Temp`、`Logs`、`UserSettings` 和生成的 `.csproj/.sln` 已在 `.gitignore` 中，属于工作区生成物，不是源码架构问题，不删除用户本地文件。
5. 根目录设计文档和 `.architecture-governance/state.md` 分类不统一；设计文档迁移及治理状态归属需用户/工具约定，本轮不改。
6. 项目没有 asmdef、`Assets/Tests` 或 `Editor` 目录，但已声明 Unity Test Framework；属于扩展性建议，不为本轮增加空目录或模块边界。
7. 未发现孤立 `.meta`、重复 GUID、完全重复资源；`MainScene` 已加入 Build Settings。

## 本轮实施清单（修复子代理必须完成）

按以下顺序实施，所有涉及场景序列化的字段必须同步更新：

1. 在现有 `GameManager` 中集中持有唯一的 `DifficultyRampDuration`，提供只读的归一化难度进度；移除 `CameraManager`、`EnemyManager`、`PropsManager` 的重复 `_elapsed` 与 `DifficultyRampDuration`，三者读取同一进度并保持各自的速度/生成曲线。使用当前项目的缩放时间语义，不引入新的静态状态。
2. 将 `SpawnAheadDistance` 收敛到 `CameraManager` 的生成点服务；移除敌人和道具管理器的重复配置，并同步 `MainScene.unity`。
3. 将 `EnemySpawnConfig` 简化为直接的 `List<GameObject>` prefab 列表；在 `Awake` 缓存有效 prefab，生成时禁止重复 `FindAll`；同步 `MainScene.unity` 的 `EnemyConfigs` 序列化结构。
4. 将玩家跳跃输入改成一个读取入口：一次读取鼠标/键盘/触摸，并在同一入口完成 EventSystem UI 拦截；`TryJump` 只消费该结果，保持原有输入设备和 UI 阻断行为。
5. 将 `ProjectileBase` 作为敌人碰撞解析入口，统一 `EnemyTargetResolver.TryResolve` 与结束流程；`Bullet`、`CornBullet` 只保留各自的命中/爆炸效果，保持单体伤害、范围伤害、震屏行为不变。
6. 删除确认无调用的 `Tween` 便捷重载；收窄 `CameraManager.TryGetPosition`、`Player.AlignBodyHorizontally` 的可见性；将 `EnemyBase` 的两个默认空钩子和 `GameManagerBinding.OnManagerBound` 改为抽象契约，并确保所有现有子类编译通过。
7. 将 `Player.GroundContact` 的硬编码跳跃次数改为 `MaxJumpCount` 的规范化值，消除同一配置的第二来源。
8. 不删除任何 PNG、组件或 prefab，不抽取尚未验证等价的地面接触/玩家目标/武器目录体系；这些候选必须在修复代理结果中明确记录为 deferred，而不能被误改成破坏性迁移。

## 验证要求

1. `rg` 检查已移除字段、方法和旧序列化键不再被源码/场景引用；确认 `DifficultyRampDuration`、`SpawnAheadDistance` 各只有一个配置入口。
2. 检查所有 `EnemyBase` 与 `GameManagerBinding` 子类均实现新的抽象成员；检查投射物只有基类碰撞入口，子类命中行为仍存在。
3. 使用 Unity 可用的编译/刷新方式验证无 C# 编译错误；若当前环境无 Unity 编辑器，至少执行项目级静态引用检查并说明限制。
4. 检查 `git diff --check`，确认没有临时文件、生成物或计划文件之外的意外改动。

## 完成判定

本轮完成时，运行时代码应少一组难度状态、少一组生成距离配置、少一层敌人 prefab 包装、少一组重复投射物碰撞入口和一个无调用 Tween 重载；现有可复用入口不再被无依据地继续抽象。若任何序列化迁移无法安全验证，应保持旧结构并在修复结果中说明，而不是留下半迁移状态。
