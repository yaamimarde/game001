# 脚本参考手册

本文档说明项目中各 C# 脚本的职责、挂载位置与协作关系。按文件夹分组。

---

## 阅读约定

- **挂载**：建议挂载的 GameObject 或 Prefab
- **依赖**：需要配合的组件或系统
- **SO**：ScriptableObject，需在 Project 窗口创建或通过 `Game → Setup` 菜单生成

---

### `DefaultGameContent.cs`
运行时 fallback：当 `Resources/Game/` 下 SO 尚未创建时，提供默认物品、商店、招募模板与属性配置。

### `GameWorldBootstrap.cs`
**挂载**：由 `GameManager` 在世界场景自动创建

确保 Gameplay UI 存在，并按需生成演示用拾取/商店/招募交互对象。

### `GameEventBus.cs`
静态事件总线。各系统在数据变化时调用 `Raise*()`，UI 订阅刷新。

| 事件 | 触发时机 |
|------|----------|
| `OnInventoryChanged` | 背包物品增删/移动/旋转 |
| `OnGoldChanged` | 金币变化 |
| `OnStatChanged` | 属性经验/等级变化 |
| `OnCompanionHired` | 成功雇佣同伴 |
| `OnCompanionDismissed` | 解散同伴 |
| `OnCompanionOrderChanged` | 同伴指令变更 |
| `OnTradeCompleted` | 完成买卖 |
| `OnGameSaved` / `OnGameLoaded` | 存档/读档 |
| `OnPlayerDied` | 玩家死亡 |
| `OnPauseChanged(bool)` | 暂停状态切换 |
| `OnSettingsChanged` | 设置保存 |

### `GameManager.cs`
**挂载**：空物体 `GameManager`（`DontDestroyOnLoad`）

全局单例。职责：
- `StartNewGame(slot, name)` / `LoadGame(slot)` / `ContinueLastGame()`
- `SaveCurrentGame()` / `AutoSave()` / `ReturnToMainMenu()`
- `SetPaused(bool)` / `QuitGame()`
- `ApplyPlayerStats()` — 将 Session 属性同步到场景内 `WarriorPlayer`

### `GameSession.cs`
非 MonoBehaviour，由 `GameManager` 持有。当前局状态容器：
- `Save` — 可序列化的 `SaveData`
- `Inventory` / `Equipment` / `Progression` / `Trade` / `Companions`

### `GameBootstrap.cs`
**挂载**：MainMenu 场景或首个加载场景

确保 `GameManager` 单例存在。Awake 时若无 Instance 则创建。

---

## Data — 数据定义

### `StatBlock.cs`
可序列化属性块：力量/敏捷/韧性/跑动/近战/远程等级与经验，以及 HP/攻击/防御基础值。

### `ItemDefinition.cs` (SO)
**创建**：`Create → Game → Item Definition`

物品模板：id、名称、图标、网格尺寸、重量、堆叠、类型、装备槽、攻防加成、买卖价。

### `ItemDatabase.cs` (SO)
**路径**：`Resources/Game/ItemDatabase`

所有 `ItemDefinition` 的注册表，`Get(itemId)` 查询。

### `RecruitTemplate.cs` (SO)
招募模板：id、名称、预制体、雇佣费、日薪、前置 world flag、基础属性。

### `RecruitDatabase.cs` (SO)
**路径**：`Resources/Game/RecruitDatabase`

`RecruitTemplate` 注册表。

### `InventoryItemSave.cs` / `CompanionSave.cs`
存档 DTO，分别描述背包物品与同伴的持久化字段。

---

## Save — 存档

### `SaveData.cs`
完整存档结构：槽位、玩家名、游玩时间、场景/出生点、属性、金币、背包、同伴、世界 flag。

### `SaveManager.cs`
JSON 读写，路径 `persistentDataPath/saves/slot_N.json`。提供 `TryLoad`、`Save`、`Delete`、`GetMostRecentSlot`、`GetSlotSummary`。

### `SceneAutoSave.cs`
**挂载**：GameManager 或独立物体

监听 `sceneLoaded`，非 MainMenu 场景时自动存档。

---

## Settings — 设置

### `SettingsManager.cs`
由 `GameManager` 持有。音量/全屏存 `PlayerPrefs`，`Apply()` 应用到 `AudioListener` 和 `Screen`。

---

## Inventory — 背包

### `InventoryItemInstance.cs`
运行时物品实例：引用 `ItemDefinition`、堆叠数、网格坐标、旋转、快捷栏标记。

### `GridInventory.cs`
网格背包逻辑：放置/移除/旋转/重量计算/存档互转。构造时从 `Resources/Game/ItemDatabase` 加载模板。

### `EquipmentManager.cs`
装备槽管理：`TryEquip` / `Unequip`，汇总装备攻防 HP 加成。

### `ItemPickup.cs`
**挂载**：世界掉落物（需 Trigger Collider2D）

玩家进入范围按 **E** 拾取，成功则销毁自身。

---

## Trade — 交易

### `ShopDefinition.cs` (SO)
商店模板：库存列表、收购品类、城镇价格系数、回购率。

### `TradeSystem.cs`
由 `GameSession` 持有。`GetBuyPrice` / `GetSellPrice` / `TryBuyFromShop` / `TrySellToShop`。

### `ShopInteractable.cs`
**挂载**：商店 NPC 触发区域

玩家按 **E** 打开 `TradeUI`。

---

## Progression — 属性成长

### `StatProgressionConfig.cs` (SO)
**路径**：`Resources/Game/StatProgressionConfig`

每级经验阈值、属性→战斗数值换算公式。

### `StatProgressionSystem.cs`
Kenshi 式练级：`RegisterMeleeHit`、`RegisterDamageTaken`、`RegisterDistanceMoved`。提供 `GetEffectiveDamage/Defense/MaxHp`。

### `PlayerProgressionTracker.cs`
**挂载**：Player

每帧累计移动距离，驱动 Athletics 成长。

---

## Companion — 雇佣

### `CompanionManager.cs`
由 `GameSession` 持有。雇佣/加载/解散同伴，跨场景 `RepositionNearPlayer`，日薪结算。

### `CompanionCharacter.cs`
**挂载**：同伴 Prefab

继承 `Character`，`Initialize(template, save)` 设置属性。

### `RecruitableNPC.cs`
**挂载**：可招募 NPC 触发区域

按 **E** 打开 `HireUI` 招募面板。

---

## AI — 人工智能

### `HostileMoveAI.cs`（已有）
**挂载**：敌人

独立敌对移动 AI：视野检测、近战环绕/追击、远程风筝、攻击节奏停步。

### `AggressiveBehaviour.cs`（已有）
**挂载**：敌人（旧栈）

攻击节奏 FSM，与 `NpcMove` 配合使用。

### `NpcMove.cs`（已有）
**挂载**：敌人（旧栈）

简单追击/巡逻。

### `IAttackBehaviour.cs`（已有）
攻击行为接口：`AttackRange`、`ShouldStopMovement`。

### `FriendlyMoveAI.cs`
**挂载**：同伴 Prefab

友方移动：Follow / Hold / 发现敌人时战斗协助。

### `CompanionCombatAI.cs`
**挂载**：同伴 Prefab

近战协助，对 `enemyLayers` 内目标造成伤害。实现 `IAttackBehaviour`。

---

## Player — 玩家

### `Character.cs`（已有，已扩展）
抽象角色基类：HP/攻击/防御/受击/死亡。支持从 `GameSession` 注入属性。

### `WarriorPlayer.cs`（已有，已扩展）
玩家子类。死亡时禁用 `PlayerMovement2D` 与攻击，触发 `GameManager.OnPlayerDeath()`。

### `MovePlayer.cs`（已有，已标记 Obsolete）
旧版 WASD 平移，请使用 `PlayerMovement2D`。

### `PlayerMovement2D.cs`（已有）
六向 Rigidbody2D 移动：走/跑/冲刺/跳跃，相机相对输入。

### `PlayerAnimation.cs`（已有）
Animator 参数驱动，攻击朝向锁定。

### `FacingCamera.cs`（已有）
Billboard，精灵始终面向相机。

### `PlayerAttackBase.cs` / `PlayerComboAttack.cs`（已有）
三连击近战；命中时调用 `StatProgressionSystem.RegisterMeleeHit()`。

---

## SceneTransition — 场景切换

### `SceneTransitionContext.cs`
静态跨场景数据：`NextSpawnPointId`、`IsLoadingFromSave`。

### `SceneTransitionTrigger.cs`（已有）
触发器加载目标场景并设置出生点 ID。

### `SpawnPoint.cs`（已有）
命名出生点 marker。

### `PlayerSpawnPoint.cs`（已有）
**挂载**：Player

Awake 时按 Context 移动到对应 `SpawnPoint`。

---

## UI — 界面

### `MainMenuUI.cs`
**挂载**：MainMenu Canvas

开始/继续/读档/设置/退出按钮逻辑。

### `SaveSlotPanel.cs` / `SaveSlotButton.cs`
三槽存档选择面板，支持新游戏与读档。

### `SettingsPanel.cs`
音量滑条、全屏开关；主菜单与暂停菜单共用。

### `PauseMenuUI.cs`
**挂载**：游戏 Canvas

**Esc** 暂停：继续/存档/设置/回主菜单。

### `GameplayHUD.cs`
HP 条、金币、属性摘要、Game Over 面板。

### `InventoryUI.cs` / `InventoryCellUI.cs`
**I / Tab** 开关网格背包，点击/右键装备或使用，旋转按钮。

### `TradeUI.cs`
商店买卖列表，打开时暂停游戏。

### `HireUI.cs`
招募对话与队伍指令（Follow/Hold/解散）。

### `GameplayUIController.cs`
**挂载**：游戏 Canvas

聚合 HUD/暂停/背包/交易/雇佣引用；**P** 打开队伍面板。

---

## Cameras — 相机

### `CameraFollow2D.cs`（已有）
2.5D 跟随相机，Q/E 步进旋转，旋转期间禁止玩家移动。

---

## Enemy — 敌人

### `Gebuling.cs`（已有）
哥布林/史莱姆子类，死亡时禁用 AI 组件。

---

## Editor — 编辑器工具

### `PlayerSpriteAnimationBuilder.cs`（已有）
菜单工具：生成占位精灵、六向动画、Animator、Player Prefab。

### `GameSetupEditor.cs`
**菜单**：`Game → Setup → Create All Game Assets And Scenes`

一键创建 Resources 资产、MainMenu 场景、Build Settings、游戏 UI 组件。

---

## 协作关系图

```
GameManager
  ├── SaveManager
  ├── SettingsManager
  └── GameSession
        ├── GridInventory ← ItemDatabase (SO)
        ├── EquipmentManager
        ├── StatProgressionSystem ← StatProgressionConfig (SO)
        ├── TradeSystem ← ShopDefinition (SO)
        └── CompanionManager ← RecruitDatabase (SO)
              └── CompanionCharacter + FriendlyMoveAI + CompanionCombatAI

WarriorPlayer ← ApplyPlayerStats ← GameSession
PlayerComboAttack → StatProgressionSystem.RegisterMeleeHit
ItemPickup / ShopInteractable / RecruitableNPC → 各 UI
```

---

## 首次运行 Checklist

1. Unity 中执行 `Game → Setup → Create All Game Assets And Scenes`
2. 确认 Build Settings 含 MainMenu、Main、HouseInterior
3. 在 Main 场景放置 `ShopInteractable`、`ItemPickup`、`RecruitableNPC`（引用对应 SO 与 UI）
4. Play 从 MainMenu 开始测试新游戏/存档/背包/商店/雇佣
