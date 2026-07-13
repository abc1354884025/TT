# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2022.3.62f3c1 **背包乱斗 (Backpack Brawl)** game targeting the **Douyin (TikTok) mini-game platform** via ByteDance TTSDK 6.7.7. Uses URP rendering, HybridCLR for code hot-update, and YooAsset (3.0.3-beta) for asset management.

## Build & Run

- **Develop**: Open project in Unity Editor 2022.3.62f3c1, open `Assets/Scenes/MainScene.unity`, press Play. GameManager runs directly — no CDN or YooAsset init needed (Editor Simulate mode).
- **Build for Douyin**: Use ByteDance StarkSDK build tools in Unity Editor menu (configured via `Assets/Editor/StarkBuilderSetting.asset`). Outputs WebGL zip to `E:\TT\output\`.
- **Run on Douyin**: Import the output zip into the Douyin Developer Tools IDE (`抖音开发者工具`).
- **Tests**: Unity Test Framework 1.1.33 is installed but no tests exist. Run via Unity Test Runner window.

### CDN Deploy Pipeline (Production)

The production build deploys through a 3-step pipeline in **Tools → YooAsset Uploader**:

1. **编译热更 DLL** — Runs `HybridCLR/Generate/All` to compile `HotUpdate.dll`
2. **扫描版本目录** — Auto-discovers the latest YooAsset build version under `Bundles/`
3. **同步到 TOS** — Copies HotUpdate.dll + BuiltinCatalog to StreamingAssets, uploads all bundles + `version.json` to 火山引擎 TOS via `tosutil` CLI

The Uploader also auto-copies `BuiltinCatalog.bytes`/`.hash`/`.version` files into `Assets/StreamingAssets/yoo/DefaultPackage/` for local fallback.

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

1. **Config Loading**: At startup, `ConfigLoader.LoadAll()` reads JSON configs from `Resources/Config/` (balance + enemies). Items are hardcoded in `ItemDatabase` static constructor (MVP).
2. **Prepare Phase**: Player drags items onto a backpack grid (`BagGrid`). `DragDropManager` handles drag preview (green/red ghost), rotation (right-click / R key, cycles 0→90→180→270→0), and placement validation. Supports both inventory→grid and grid→grid dragging. `PrepareViewModel` bridges UI ↔ state.
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
| `ItemType` | `Core/ItemType.cs` | Weapon/Armor/Accessory/Trinket enum |
| `CombatStats` | `Core/CombatStats.cs` | Aggregated battle stats (ATK/DEF/HP/CRIT) |
| `BattleResolver` | `Core/BattleResolver.cs` | Turn-based auto-battle simulator |
| `BattleResult` | `Core/BattleResult.cs` | Battle outcome + log entries |

### Config JSONs

Located at `Assets/Resources/Config/`:
- `balance.json` — `BalanceConfig`: grid dimensions (`gridWidth`/`gridHeight`), starting gold, shop config, damage formula params
- `enemies.json` — `EnemyConfig` with `EnemyEntry[]`: name, stats, difficulty, loot table
- Items are **hardcoded** in `ItemDatabase` static constructor (MVP). Hot-update can override via `ConfigLoader.LoadItems(overrideBytes)`.

## Scenes

| Scene | Entry Point | When Used |
|-------|-------------|-----------|
| `MainScene.unity` | `GameManager.Awake()` | Editor development — no CDN, no YooAsset init needed |
| `SampleScene.unity` | `HotUpdateBootstrap.Start()` | Production builds — CDN hot-update flow |

## Startup Flow

### Editor Development (MainScene)

`GameManager.Start()` initiates the startup coroutine:
1. **Wait for YooAsset**: `yield return new WaitUntil(() => ResourceManager.IsYooAssetReady)` — blocks until HotUpdateBootstrap signals completion (or immediately in Editor since UIManager's Awake sets ResourcesProvider, making `IsBootstrapDone` redundant)
2. `SaveManager.Load()` — load persistent progress
3. `ConfigLoader.LoadAll()` — load config JSONs into memory
4. `UIManager.SetHotUpdateAssembly(typeof(GameManager).Assembly)` — register hot-update assembly for panel discovery
5. `UIManager.Open("MainMenuPanel")` — open the main menu

### Production CDN Flow (SampleScene)

`HotUpdateBootstrap.Start()` handles the full CDN bootstrap:

**Phase 1: Init AOT Framework** — `UIManager.Instance` creates 5-layer Canvas stack, sets `ResourcesProvider` as default.

**Phase 2: Init YooAsset** (skipped in `_devMode`) — Creates `DefaultPackage` with `WebPlayModeOptions` using `WebNetworkFileSystemParameters` (pure CDN, no local filesystem). `CdnRemoteService` provides versioned CDN URLs: `{baseUrl}/{version}/{fileName}`. Fetches version manifest (`version.json`) from CDN first.

**Phase 3: Load HotUpdate DLL** — Tries YooAsset asset load first (3 address format fallbacks: `HotUpdate.bytes` → `HotUpdate` → `HotUpdate` without extension), then falls back to direct CDN download (`{cdnUrl}/{version}/HotUpdate.dll`). Uses `Assembly.Load(byte[])`.

**Phase 4: Inject Assembly** — Calls `UIManager.SetHotUpdateAssembly()` and sets `ResourceManager.IsBootstrapDone = true` to signal GameManager.

**Fallback**: If `_fallbackToResources = true` and both YooAsset + direct CDN fail, the game continues with local Resources provider.

### ResourceManager Bridge

`ResourceManager` is the static bridge between bootstrap and game startup:
- `IsBootstrapDone` — set `true` by HotUpdateBootstrap at end of bootstrap; `IsYooAssetReady` delegates to this
- `IsYooAssetReady` — GameManager blocks on this before proceeding with Save/Config/Panel loading
- Provider proxy — routes all `LoadSprite`/`LoadAsync` calls to the current `IResourceProvider`

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

- **`IResourceProvider`** (`Resource/IResourceProvider.cs`): Abstraction. Two implementations: `ResourcesProvider` (local/fallback) and `YooAssetProvider` (CDN hot-update via YooAsset `DefaultPackage`).
- **`YooAssetProvider`** (`Resource/YooAssetProvider.cs`): `InstantiateAsync` tries 4 address formats sequentially to be compatible with `AddressByFileName`: the raw path, filename without extension, filename with extension, and `"Resources/"` + path prefix. Synchronous `Load<T>` uses the path as-is. Maintains reference counting per handle.
- **`PanelCache`** pools closed panels by PanelId (max 3 per type).

### YooAsset Bundle Groups

Defined in `Assets/BundleCollectorSetting.asset`:

| Group | Path | Pack Rule | Contents |
|-------|------|-----------|----------|
| DLL | `StreamingAssets/HotUpdateDlls/HotUpdate.bytes` | `PackRawFile` | HotUpdate DLL bytecode |
| UI | `Resources/UI/Panels`, `Resources/UI/Prefabs`, `Resources/UI/Widgets` | `PackDirectory` | All UI prefabs |
| Config | `Resources/Config` | `PackDirectory` | JSON config files |

All groups use `AddressByFileName` — the only format compatible with both `PackRawFile` (DLL group) and `PackDirectory` (UI/Config groups). `EnableAddressable: 0`, `UniqueBundleName: 1`.

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
- **YooAsset address format constraint**: All collectors in `BundleCollectorSetting.asset` use `AddressByFileName` — the only format compatible with both PackRawFile (DLL group) and PackDirectory (UI/Config groups). The provider must try multiple address formats to handle edge cases (e.g., `InstantiateAsync` tries `path`, `fileName`, `fileWithExt`, and `Resources/` + path prefix).
