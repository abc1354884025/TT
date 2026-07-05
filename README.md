# TT — 抖音小游戏益智合集

基于 **Unity 2022.3 LTS** 开发的抖音（Douyin/TikTok）小游戏项目，使用 **HybridCLR** 实现代码热更新（hot-update），定位为包含多种经典益智游戏的合集。

---

## 技术栈

| 层级 | 技术 |
|------|------|
| 游戏引擎 | Unity 2022.3.62f3c1 |
| 渲染管线 | URP（Universal Render Pipeline） |
| 热更新 | [HybridCLR](https://github.com/focus-creative-games/hybridclr_unity) |
| 小程序 SDK | ByteDance StarkSDK 6.7.7（TTSDK） |
| 目标平台 | WebGL（抖音小游戏） |
| 脚本后端 | IL2CPP |
| UI 系统 | 自研 MVVM 框架（UGUI + TextMeshPro） |
| 测试框架 | Unity Test Framework 1.1.33 |

---

## 快速开始

### 开发环境

1. 使用 **Unity 2022.3.62f3c1** 打开项目
2. 打开 `Assets/Scenes/SampleScene.unity`
3. 点击 Play 即可运行

### 构建抖音小游戏

1. 通过 Unity 编辑器菜单 **ByteDance → Build** 构建
2. 构建配置位于 `Assets/Editor/StarkBuilderSetting.asset`
3. 产物为 WebGL zip 包，输出到 `E:\TT\output\`
4. 将 zip 导入「抖音开发者工具」预览/发布

### CDN 热更（生产环境）

HotUpdateBootstrap 启动流程：
1. 初始化 AOT UI 框架（Canvas 层级）
2. 从 CDN 下载 HotUpdate DLL → `Assembly.Load` 注入
3. 下载 UI AssetBundle → 切换资源加载器
4. 打开首个面板（`MainMenuPanel`）

CDN 不可用时自动回退到本地 `Resources` 模式。

---

## 程序集架构

```
Entry (AOT) ──► UIFramework (AOT) ──► Unity.TextMeshPro
                      ▲
                      │
                 HotUpdate  (autoReferenced: false)
```

| 程序集 | 类型 | 职责 |
|--------|------|------|
| `Entry` | AOT | 启动引导 `HotUpdateBootstrap` |
| `UIFramework` | AOT | UI 框架（MVVM 绑定、面板管理、资源加载） |
| `HotUpdate` | **热更** | 游戏逻辑（面板、ViewModel、关卡、规则） |
| `TTLitJson` | 内置 | JSON 序列化（TTSDK 附带） |
| `TTWebGL` | 内置 | ByteDance WebGL 平台桥接 |
| `com.bytedance.ttsdk.ref` | 内置 | 字节跳动 SDK 引用 |

**关键约束**：`HotUpdate.asmdef` 设置 `autoReferenced: false`，AOT 绝不直接引用 HotUpdate 类型。通信通过 `UIPanel` 基类和反射完成。

---

## UI 框架（MVVM）

框架位于 `Assets/Scripts/Framework/UI/`，核心设计：

| 组件 | 说明 |
|------|------|
| `UIManager` | 单例，管理 5 层 Canvas 栈（Background/Normal/Popup/Top/System），通过反射按类名发现面板 |
| `UIPanel` | 面板抽象基类，生命周期：`OnInit → OnOpen → OnShow ⇄ OnHide → OnClose` |
| `BindableProperty<T>` | 泛型响应式属性，委托驱动，IL2CPP 安全 |
| `ObservableObject` | ViewModel 基类，`PropertyChanged` 通知 |
| `UIBindingExtensions` | 一行绑定（`text.BindTo(vm.Title)`），所有绑定返回 `Action` 解绑委托 |
| `UIList` | 对象池化滚动列表 |
| `IResourceProvider` | 资源加载抽象（`ResourcesProvider` 本地 / `AssetBundleProvider` CDN） |

### 新建面板流程

```
1. 创建 Panel.cs 继承 UIPanel，重写 OnOpen/OnClose
2. 创建 ViewModel.cs 继承 ObservableObject，定义 BindableProperty 字段
3. OnOpen 中创建 VM 并绑定 UI（收集解绑 Action）
4. OnClose 中逐一 invoke 解绑，释放 VM
5. 创建 Prefab 放在 Resources/UI/Panels/{PanelName}.prefab
6. 通过 UIManager.Instance.Open("PanelName", data) 打开
```

---

## 益智游戏合集

当前包含 **四种益智游戏**，全部实现在 HotUpdate 程序集中：

| 游戏 | 规则 | 交互 | 网格 |
|------|------|------|------|
| **数回** (Number Link) | 用不相交路径连接相同数字对，填满所有格子 | 拖拽画线，回溯擦除 | 5×5 ~ 12×12 |
| **数独** (Sudoku) | 每行/列/宫填入 1-9 不重复 | 点击格子 → 点击数字 | 固定 9×9 |
| **数墙** (Nurikabe) | 涂黑格子形成岛屿，岛屿大小=数字，墙壁全连通 | 点击切换白/黑 | 5×5 ~ 15×15 |
| **数桥** (Hashiwokakero) | 数字岛之间建桥，数字=桥总数，最多2座/对 | 拖拽岛之间建桥 | 稀疏岛屿 |

### 游戏代码结构

```
Assets/Scripts/HotUpdate/Game/
├── Puzzle/
│   ├── Common/          ← 公共基础（PuzzleGrid, GridInputHandler, IPuzzleRuleEngine）
│   ├── Sudoku/          ← 数独（Grid, LevelData, RuleEngine, SaveData）
│   ├── Nurikabe/        ← 数墙
│   ├── NumberLink/      ← 数回
│   ├── HashiBridge/     ← 数桥
│   └── Data/            ← SaveManager, LevelDatabase
├── UI/
│   ├── Panels/          ← 面板（MainMenu, LevelSelect, 4×游戏面板, Settings）
│   ├── ViewModels/      ← ViewModel（PuzzleGameViewModel 基类 + 各游戏 VM）
│   └── Widgets/         ← 可复用控件（GridCell, NumberButton, VictoryPopup…）
```

每个游戏实现：
- **Grid**：棋盘状态
- **LevelData**：关卡数据解析
- **RuleEngine**：规则验证（`IPuzzleRuleEngine` 接口）
- **SaveData**：存档序列化
- **ViewModel**：游戏逻辑 + 撤销栈
- **Panel**：渲染 + 交互

---

## 项目结构

```
Assets/
├── Scenes/
│   └── SampleScene.unity          ← 主场景
├── Scripts/
│   ├── Entry/                     ← AOT 引导
│   ├── Framework/                 ← AOT UI 框架
│   └── HotUpdate/                 ← 热更游戏代码
├── Prefabs/UI/                    ← 预制体（Panels/ Widgets/）
├── Resources/UI/Panels/           ← Resources 加载路径
├── Settings/                      ← URP 配置文件
└── Plugins/ByteGame/              ← 字节跳动 SDK
```

---

## 配置与工具

- **`project.config.json`**：抖音小游戏项目配置
- **`game.json`**：小游戏运行配置
- **`.gitignore`**：忽略 Library/Temp/Logs/UserSettings 等

---

## 开发约定

1. **协程异步**：资源加载/初始化使用 `IEnumerator`，不使用 `async/await`
2. **IL2CPP 安全**：不使用 `System.Reflection` 操作值类型，绑定通过泛型委托实现
3. **所有绑定返回解绑 Action**：收集到 `List<Action>`，`OnClose` 中全部 invoke
4. **面板按类名发现**：`UIManager.Open("TestPanel")` 在 HotUpdate 程序集中按类名反射查找
5. **MonoSingleton**：框架单例使用 `MonoSingleton<T>`（线程安全，跨场景持久）

---

## 可用面板（通过 UIManager.Open 打开）

| Panel ID | 说明 | 所在程序集 |
|----------|------|-----------|
| `MainMenuPanel` | 主菜单（四个游戏入口） | HotUpdate |
| `LevelSelectPanel` | 关卡选择（传入 PuzzleType） | HotUpdate |
| `SudokuPanel` | 数独游戏面板 | HotUpdate |
| `NurikabePanel` | 数墙游戏面板 | HotUpdate |
| `NumberLinkPanel` | 数回游戏面板 | HotUpdate |
| `HashiBridgePanel` | 数桥游戏面板 | HotUpdate |
| `SettingsPanel` | 设置面板 | HotUpdate |
| `TestPanel` | 测试/示例面板 | HotUpdate |

---

## License

项目仅供学习和参考使用。
