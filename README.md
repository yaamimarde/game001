# game001

Unity 2022.3 LTS 俯视 2D 游戏项目模板。

## 环境要求

- [Unity Hub](https://unity.com/download)（中国版：[unity.cn](https://unity.cn/releases)）
- Unity **2022.3.62f3c1**（Unity 中国版 LTS，推荐）
- 全球版用户可使用 **2022.3.62f3**，两者兼容

## 快速开始

1. 打开 Unity Hub，点击 **Add** → 选择本文件夹
2. 等待 Unity 首次导入（会自动生成 `Library/`，属正常现象）
3. 打开场景 `Assets/Main.unity`（与 `001.unity` 同级，在 Assets 根目录）
4. 点击 **Play** 运行

## 操作说明

- **WASD** 或 **方向键**：四向移动玩家
- **Q / E**：相机步进旋转 45°
- 透视相机平滑跟随玩家

## 项目结构

```text
Assets/
├── Scenes/          # 场景文件（Main.unity 为示例场景）
├── Scripts/
│   ├── Player/      # 玩家逻辑（PlayerMovement2D）
│   └── Camera/      # 相机逻辑（CameraFollow2D）
├── Prefabs/         # 预制体（Player.prefab）
└── Sprites/         # 精灵图资源
```

## 核心脚本

| 脚本 | 说明 |
|------|------|
| `PlayerMovement2D` | 俯视四向移动，基于 Rigidbody2D，无重力 |
| `CameraFollow2D` | 2.5D 透视相机平滑跟随目标，Q/E 步进旋转 |
| `FacingCamera` | Billboard，精灵始终面向主相机 |

## 注意事项

- 请勿将 `Library/`、`Temp/`、`Logs/` 提交到 Git（已在 `.gitignore` 中排除）
- 首次打开若提示升级项目版本，使用 2022.3.62f3c1（或 2022.3.62f3）即可
- 替换 `Assets/Sprites/` 中的占位图即可更换玩家与地面外观
