# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2022.3.62f3c1 **背包乱斗 (Backpack Brawl)** game targeting the **Douyin (TikTok) mini-game platform** via ByteDance TTSDK 6.7.7. Uses URP rendering, HybridCLR for code hot-update, and YooAsset (3.0.3-beta) for asset management.

## Build & Run

- **Develop**: Open project in Unity Editor 2022.3.62f3c1, open `Assets/Scenes/MainScene.unity`, press Play.
- **Build for Douyin**: Use ByteDance StarkSDK build tools in Unity Editor menu (configured via `Assets/Editor/StarkBuilderSetting.asset`). Outputs WebGL zip to `E:\TT\output\`.
- **Run on Douyin**: Import the output zip into the Douyin Developer Tools IDE (`抖音开发者工具`).
- **Tests**: Unity Test Framework 1.1.33 is installed but no tests exist. Run via Unity Test Runner window.

## Assembly Architecture

```
Entry ──► UIFramework
              ▲
              │
         HotUpdate  (autoReferenced: false) ──► TTLitJson
```

| Assembly | Tier | Purpose |
|----------|------|---------|
| `UIFramework` | AOT | UI framework — MVVM bindings, panel lifecycle, resource loading, LoopScrollView |
| `Entry` | AOT | `HotUpdateBootstrap` — downloads hot-update DLLs from CDN, injects into UIManager |
| `HotUpdate` | **Hot‑update** | All game logic — panels, ViewModels, widgets, managers, core data, config |
| `TTLitJson` | Built-in | JSON serialization (TTSDK bundled) |
| `TTWebGL` | Built-in | ByteDance WebGL platform bridge |
| `com.bytedance.ttsdk.ref` / `com.bytedance.ttsdk-editor` | Built-in | ByteDance SDK assemblies |

**Critical constraint**: `HotUpdate.asmdef` has `autoReferenced: false`. AOT code (Entry, UIFramework) must NEVER reference HotUpdate types directly. All cross-tier communication goes through `UIPanel` base class (in AOT) and reflection in `UIManager.FindPanelType`.

## Game Architecture: Backpack Brawl

### Game Loop

```
MainMenu ──► Prepare ──► Battle ──► Reward ──► Prepare ──► ...
```

**State machine** is managed by `GameManager` ([GameManager.cs](Assets/Scripts/HotUpdate/Game/Managers/GameManager.cs)):
- `MainMenu` — entry point, navigation to Prepare
- `Prepare` — backpack grid management, drag-and-drop item placement
- `Battle` — auto-resolved turn-based combat (instant simulation)
- `Reward` — post-battle loot/result display

### Key Data Flow

1. **Config Loading**: At startup, `ConfigLoader.LoadAll()` reads JSON configs from `Resources/Config/` into `ConfigLoader.Items`, `ConfigLoader.Balance`, `ConfigLoader.Enemies`.
2. **Prepare Phase**: Player drags items onto a backpack grid (`BagGrid`). `DragDropManager` handles drag preview, rotation, and placement validation. `PrepareViewModel` bridges UI ↔ state.
3. **Battle Phase**: `BattleResolver.Simulate()` runs a turn-based auto-battle using `CombatStats` aggregated from placed items + enemy config. Produces a `BattleResult` with logs.
4. **Reward Phase**: Shows battle outcome and any earned items.
5. **Save/Load**: `SaveManager` wraps TTSDK `TT.Save`/`TT.LoadSaving`, keyed as `"backpack_brawl_save"`. Saves gold, inventory, round progress.

### Core Types

| Class | Path | Purpose |
|-------|------|---------|
| `GameManager` | `Managers/GameManager.cs` | Global state machine, round tracking |
| `ConfigLoader` | `Managers/ConfigLoader.cs` | Static config loader from JSON |
| `SaveManager` | `Managers/SaveManager.cs` | Persistent save/load via TTSDK |
| `DragDropManager` | `Managers/DragDropManager.cs` | Item drag-and-drop with collision preview |
| `ItemData` | `Core/ItemData.cs` | Item definition — shape matrix, rarity, combat stats |
| `ItemDatabase` | `Core/ItemDatabase.cs` | Item lookup utilities |
| `BagGrid` | `Core/BagGrid.cs` | Backpack grid — placement, collision, removal |
| `PlacedItem` | `Core/PlacedItem.cs` | An item placed on the grid (position + rotation) |
| `ItemShape` | `Core/ItemShape.cs` | Shapes defined by JSON `int[][]` matrix |
| `ItemRarity` | `Core/ItemRarity.cs` | Common/Rare/Epic/Legendary enum |
| `ItemType` | `Core/ItemType.cs` | Weapon/Armor/Accessory enum |
| `CombatStats` | `Core/CombatStats.cs` | Aggregated battle stats (ATK/DEF/HP/CRIT) |
| `BattleResolver` | `Core/BattleResolver.cs` | Turn-based auto-battle simulator |
| `BattleResult` | `Core/BattleResult.cs` | Battle outcome + log entries |

### Config JSONs

Located at `Assets/Resources/Config/`:
- `items.json` — Array of `ItemData` definitions (shape matrices, stats, rarity)
- `enemies.json` — Enemy definitions per round (name, stats, loot table)
- `balance.json` — Global scaling values (round multipliers, stat curves)

## Startup Flow

`GameManager.Awake()` (in MainScene) initiates the startup sequence:
1. `SaveManager.Load()` — load persistent progress
2. `ConfigLoader.LoadAll()` — load config JSONs into memory
3. `UIManager.Instance` — initialize AOT UI framework (5-layer Canvas stack with `GraphicRaycaster`)
4. `UIManager.SetHotUpdateAssembly(typeof(GameManager).Assembly)` — register hot-update assembly for panel discovery
5. `UIManager.Open("MainMenuPanel")` — open the main menu

> Note: `GameManager` replaces the old `HotUpdateBootstrap` as the primary entry point. `HotUpdateBootstrap` still exists for HybridCLR CDN download scenarios in production builds but is not used during Editor development.

### HotUpdateBootstrap (Production CDN Flow)

For production builds, `HotUpdateBootstrap` in `SampleScene.unity` handles CDN hot-update:
- `_devMode = true` (default) skips all CDN downloads — uses local Resources and already-loaded assemblies
- `_devMode = false` — fetches a version manifest (`version.json`) from CDN to determine the latest version, then downloads DLLs and AssetBundles. Falls back to `ResourcesProvider` if CDN is unavailable.
- CDN version manifest format: `{"version": "2.0.0"}`

## UI Framework (MVVM)

The UI framework in `Assets/Scripts/Framework/UI/` is shared across AOT and hot-update layers.

### Core Types

- **`UIManager`** (`Core/UIManager.cs`): MonoSingleton managing a 5-layer Canvas stack (`Background=0`, `Normal=100`, `Popup=200`, `Top=300`, `System=400`). `Open("PanelName")` resolves the panel type via reflection — searches hot-update assembly first, then AOT. Canvases auto-created with `GraphicRaycaster`.

- **`UIPanel`** (`Core/UIPanel.cs`): Abstract base class. Lifecycle: `OnInit()` (coroutine) → `OnOpen(data)` → `OnShow()` ⇄ `OnHide()` → `OnClose()`. `CacheOnClose` enables panel pooling. `AutoBindings` for inspector-driven field assignment.

- **`UIWidget`** (`Components/UIWidget.cs`): Lightweight sub-component for reusable UI pieces. Not managed by the layer stack.

- **`LoopScrollView`** (`Components/LoopScrollView.cs`): Virtualized scrolling list (KingSoft.UI namespace). Replaces the removed `UIList`. Used for level select, backpack grids, and any scrollable item lists. Key API: `Initialize(prefab, count)`, `ReloadData(count)`, `OnCellInit`/`OnCellUpdate` events. Supports `cellNumOfColumn` for grid layouts.

### Data Binding (IL2CPP-Safe)

- **`BindableProperty<T>`** (`Binding/BindableProperty.cs`): Generic reactive property using delegates (no reflection). Fires `OnChanged` only on value change.
- **`ObservableObject`** (`Binding/ObservableObject.cs`): ViewModel base class with `PropertyChanged` event.
- **`UIBindingExtensions`** (`Binding/UIBindingExtensions.cs`): One-liner bind methods on Unity UI components. Every method returns `Action` (unsubscribe delegate). `BindTwoWay` returns `void`. Supports `TMP_Text`, `Image`, `Button`, `Slider`, `Toggle`, `TMP_InputField`, `GameObject.SetActive`.

### Resource Loading

- **`IResourceProvider`** (`Resource/IResourceProvider.cs`): Abstraction. Two implementations: `ResourcesProvider` (local/fallback) and `AssetBundleProvider` (CDN hot-update).
- **`PanelCache`** pools closed panels by PanelId (max 3 per type).

### Prefab Locations

Panels: `Assets/Resources/UI/Panels/{PanelName}.prefab`
Widgets: `Assets/Resources/UI/Widgets/{WidgetName}.prefab`

Panel path convention: `"UI/Panels/{PanelId}"` → resolved by current `IResourceProvider`.

## UI Panels (HotUpdate)

| Panel | PanelId | State | Purpose |
|-------|---------|-------|---------|
| `MainMenuPanel` | `MainMenuPanel` | MainMenu | Game entry, navigate to Prepare |
| `PreparePanel` | `PreparePanel` | Prepare | Backpack grid, drag-and-drop item placement |
| `BattlePanel` | `BattlePanel` | Battle | Battle animation/log display |
| `RewardPanel` | `RewardPanel` | Reward | Victory/defeat result, loot display |
| `TestPanel` | `TestPanel` | — | Demo panel proving hot-update pipeline |

## Key Conventions

1. **Panels found by class name**: `UIManager.Open("PreparePanel")` looks for type `PreparePanel` in the hot-update assembly (exact match, then namespace-agnostic).

2. **All bindings return unsubscribe Actions**: Collect in `List<Action> _unbind`, invoke all in `OnClose()`. Exception: `BindTwoWay` returns `void`.

3. **Coroutine-based async**: Use `IEnumerator` with `yield return new WaitUntil(...)`. Not `async/await`.

4. **HotUpdate types NOT referenced in AOT code**: AOT code communicates through `UIPanel` base class and reflection.

5. **Widget self-update pattern**: Widgets like `BackpackItemWidget`, `ShopItemWidget` take a data object via a `Bind()` or `Init()` method and update their own UI internally. Panels only pass data, not UI manipulation logic.

6. **MonoSingleton<T>** for framework singletons: Thread-safe, `DontDestroyOnLoad`.

## Platform Constraints

- Target: **WebGL** (Douyin mini-game runs in a WebView/JS environment)
- IL2CPP AOT compilation — no runtime reflection on generic types. `BindableProperty<T>` works via generic delegates.
- ByteDance SDK provides custom WebGL interop (`.jslib`), custom AssetBundle loading (`TTAssetBundle`), file system APIs (`TTFileSystemManager`), and save APIs (`TT.Save`/`TT.LoadSaving`).
- YooAsset 3.0.3-beta handles asset bundle packaging and delivery alongside HybridCLR hot-update DLLs.
