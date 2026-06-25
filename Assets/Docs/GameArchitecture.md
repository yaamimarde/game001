# 游戏架构说明

本文档描述 `game001` 项目的整体架构、模块划分、数据流与扩展点。

## 架构概览

游戏采用 **单例 GameManager + GameSession** 的持久化会话模型。主菜单与世界场景分离，世界场景切换时通过 `SceneTransitionContext` 传递出生点，完整游戏状态保存在 `GameSession` 并由 JSON 存档持久化。

```
MainMenu → GameManager.StartNewGame / LoadGame
         → GameSession（属性/背包/交易/成长/同伴）
         → Main / HouseInterior 世界场景
         → HUD / 背包 / 商店 / 雇佣 UI
```

## 分层结构

| 层级 | 目录 | 职责 |
|------|------|------|
| 启动层 | `Core/`, `Save/`, `Settings/` | 生命周期、存档、设置 |
| 会话层 | `Core/GameSession.cs` | 当前局运行时状态 |
| 玩法层 | `Inventory/`, `Trade/`, `Progression/`, `Companion/` | 背包、交易、练级、雇佣 |
| AI 层 | `enemy/`, `AI/Friendly/` | 敌对与友方行为 |
| UI 层 | `UI/` | 菜单、HUD、各系统面板 |
| 数据层 | `Data/` | ScriptableObject 与 DTO |

## 场景流程

| 场景 | 路径 | 作用 |
|------|------|------|
| MainMenu | `Assets/Scenes/MainMenu.unity` | 开始/读档/设置/退出 |
| Main | `Assets/Main.unity` | 主世界 |
| HouseInterior | `Assets/Scenes/HouseInterior.unity` | 室内 |

Build Settings 顺序：MainMenu → Main → HouseInterior。

**首次 setup**：在 Unity 菜单执行 `Game → Setup → Create All Game Assets And Scenes`。

## 存档格式

- 路径：`Application.persistentDataPath/saves/slot_{0|1|2}.json`
- 内容：`SaveData` — 玩家名、属性、金币、网格背包、同伴、场景名、出生点、世界 flag
- 触发：手动（暂停菜单）、场景加载后自动、定时（5 分钟）、退出游戏

## 网格背包（参考紫色晶石）

- 主背包为 **W×H 网格**，物品占多格，可 **旋转**
- **重量上限**，超重时无法放入
- **装备槽** 独立于网格（武器/护甲/饰品）
- 数据：`ItemDefinition` (SO) + `InventoryItemInstance` + `GridInventory`

## 交易系统（参考 Kenshi）

- 商店 NPC + `ShopDefinition` 定义库存与收购品类
- 价格：`basePrice × townModifier × buybackRate`
- MVP 仅合法买卖；偷窃/商队为二期扩展

## 升级系统（参考 Kenshi）

属性通过 **使用成长**，非经验条升级：

| 属性 | 触发 |
|------|------|
| 力量/近战/敏捷 | 近战命中 |
| 韧性 | 受到伤害 |
| 跑动 | 移动距离 |

`StatProgressionSystem` 计算有效 HP/攻击/防御并写回 `Character`。

## 雇佣与友方 AI

- `RecruitTemplate` 定义费用、日薪、预制体
- `CompanionManager` 生成 `DontDestroyOnLoad` 同伴，跨场景跟随
- `FriendlyMoveAI`：Follow / Hold / CombatAssist
- `CompanionCombatAI`：近战协助攻击

## 事件总线

`GameEventBus` 提供：`OnInventoryChanged`、`OnGoldChanged`、`OnStatChanged`、`OnCompanionHired`、`OnTradeCompleted`、`OnPlayerDied`、`OnPauseChanged` 等。UI 订阅这些事件刷新，不直接修改底层数据。

## 扩展点（二期）

- 偷窃与派系声望
- 商队与动态供需
- 任务与对话树
- 同伴独立小背包 UI
- Newtonsoft JSON 替代 JsonUtility

## 关键 Prefab / SO 清单

| 资产 | 路径 |
|------|------|
| ItemDatabase | `Resources/Game/ItemDatabase` |
| RecruitDatabase | `Resources/Game/RecruitDatabase` |
| StatProgressionConfig | `Resources/Game/StatProgressionConfig` |
| 示例商店 | `Resources/Game/Shops/general_store` |
| 示例招募 | `Resources/Game/Recruits/guard_recruit` |

## 操作说明（游戏中）

| 按键 | 功能 |
|------|------|
| I / Tab | 打开/关闭背包 |
| E | 与商店/招募/拾取交互 |
| Esc | 暂停菜单 |
| P | 队伍面板 |
