# 🎒 TT — 背包乱斗 (Backpack Brawl)

> 在有限的背包格子里塞满装备，然后自动战斗吧！ (╯°□°)╯︵ ┻━┻

基于 **Unity 2022.3 LTS** 开发的抖音（Douyin/TikTok）小游戏，使用 **HybridCLR** + **YooAsset** 实现代码和资源热更新。

---

## 🛠 技术栈

| 层级 | 技术 |
|------|------|
| 🎮 游戏引擎 | Unity 2022.3.62f3c1 |
| 🖼 渲染管线 | URP（Universal Render Pipeline） |
| 🔥 热更新 | [HybridCLR](https://github.com/focus-creative-games/hybridclr_unity) |
| 📦 资源管理 | [YooAsset](https://github.com/tuyoogame/YooAsset) 3.0.3-beta |
| ☁️ CDN | 火山引擎 TOS |
| 📱 小程序 SDK | ByteDance StarkSDK 6.7.7（TTSDK） |
| 🌐 目标平台 | WebGL（抖音小游戏） |
| ⚙️ 脚本后端 | IL2CPP |
| 🧩 UI 系统 | 自研 MVVM 框架（UGUI + TextMeshPro） |

---

## 🚀 快速开始

### 开发环境

1. 使用 **Unity 2022.3.62f3c1** 打开项目
2. 打开 `Assets/Scenes/MainScene.unity`
3. 点击 Play — YooAsset 自动以 EditorSimulateMode 运行 ✨

### 构建抖音小游戏

1. 通过 Unity 编辑器菜单 **ByteDance → Build** 构建
2. 构建配置位于 `Assets/Editor/StarkBuilderSetting.asset`
3. 产物为 WebGL zip 包，输出到 `E:\TT\output\`
4. 将 zip 导入「抖音开发者工具」预览/发布

### 构建资源包 & 上传 CDN

1. **YooAsset → AssetBundle Builder** — 构建资源包（输出到 `Bundles/`）
2. **Tools → YooAsset Uploader** — 扫描版本目录 → 一键同步到火山引擎 TOS ☁️
3. CDN 版本清单（`version.json`）自动生成在 TOS 根目录  (｀・ω・´)b

---

## ⚔️ 游戏玩法

**背包乱斗**：在有限的背包格子中拖拽放置不同形状的装备，聚合战斗属性后与敌人自动战斗。

```
  🗡️ 长剑  🛡️ 盾牌  💍 戒指
  ┌──┬──┬──┐
  │🟦│🟦│  │   ← 把装备塞进背包！
  │  │🟦│  │
  └──┴──┴──┘
    背包网格
```

游戏循环：**MainMenu → Prepare → Battle → Reward → ...** 🔄

| 阶段 | 说明 |
|------|------|
| 🏠 MainMenu | 主菜单，进入准备阶段 |
| 🎒 Prepare | 背包网格管理，从商店购买/拖拽放置装备 |
| ⚔️ Battle | 回合制自动战斗，瞬间模拟完成 |
| 🎁 Reward | 战斗结算，展示掉落/奖励 |

### 核心类型

| 类型 | 说明 |
|------|------|
| `GameManager` | 全局状态机，回合管理 |
| `ConfigLoader` | JSON 配置加载（items / enemies / balance） |
| `SaveManager` | 存档（TTSDK TT.Save） |
| `DragDropManager` | 物品拖拽、旋转、碰撞检测 🔄 |
| `BagGrid` | 背包网格：放置、碰撞、移除 |
| `CombatStats` | 战斗属性聚合（ATK / DEF / HP / CRIT） |
| `BattleResolver` | 回合制自动战斗模拟 ⚡ |

---

## 🏗 程序集架构

```
Entry ──► UIFramework ──► YooAsset
              ▲
              │
         HotUpdate  (autoReferenced: false) ──► TTLitJson
```

| 程序集 | 类型 | 职责 |
|--------|------|------|
| `Entry` | AOT | `HotUpdateBootstrap` + `YooAssetBootstrap` |
| `UIFramework` | AOT | UI 框架 + `YooAssetProvider` |
| `HotUpdate` | **热更** 🔥 | 游戏逻辑（面板、ViewModel、Widget、Manager） |
| `TTLitJson` | 内置 | JSON 序列化 |
| `TTWebGL` | 内置 | ByteDance WebGL 平台桥接 |

> ⚠️ **关键约束**：`HotUpdate.asmdef` 设置 `autoReferenced: false`，AOT 绝不直接引用 HotUpdate 类型。

---

## 🧩 UI 框架（MVVM）

| 组件 | 说明 |
|------|------|
| `UIManager` | 5 层 Canvas 栈，按类名反射发现面板 |
| `UIPanel` | 面板基类：`OnInit → OnOpen → OnShow ⇄ OnHide → OnClose` |
| `BindableProperty<T>` | 响应式属性，委托驱动，IL2CPP 安全 💚 |
| `UIBindingExtensions` | 一行绑定，返回 `Action` 解绑委托 |
| `LoopScrollView` | 虚拟滚动列表（KingSoft.UI） |
| `YooAssetProvider` | 基于 YooAsset 的 `IResourceProvider` 实现 |

### 新建面板

```
1. Panel.cs 继承 UIPanel，重写 OnOpen/OnClose
2. ViewModel.cs 继承 ObservableObject，定义 BindableProperty
3. OnOpen 中绑定 UI（收集 Action），OnClose 中 invoke 解绑
4. Prefab 放 Resources/UI/Panels/{Name}.prefab
5. UIManager.Instance.Open("PanelName", data)
```

---

## 📋 可用面板

| Panel ID | 状态 | 说明 |
|----------|------|------|
| `MainMenuPanel` | 🏠 MainMenu | 主菜单入口 |
| `PreparePanel` | 🎒 Prepare | 背包网格、物品拖拽 |
| `BattlePanel` | ⚔️ Battle | 战斗动画/日志 |
| `RewardPanel` | 🎁 Reward | 结算界面 |
| `TestPanel` | 🧪 — | 热更管线验证面板 |

---

## 📐 开发约定

1. **协程异步**：使用 `IEnumerator`，不用 `async/await`
2. **绑定返回 Action**：收集到 `List<Action>`，`OnClose` 全部 invoke
3. **Widget 自更新**：Widget 通过 `Bind(data)` 或 `SetIndex(i)` 自己更新 UI
4. **面板按类名发现**：`UIManager.Open("PanelName")` 自动反射查找
5. **MonoSingleton<T>**：框架单例，线程安全，DontDestroyOnLoad

---

> 📝 仅供学习和参考使用 — 祝你背包满满，战无不胜！ (๑•̀ㅂ•́)و✧
