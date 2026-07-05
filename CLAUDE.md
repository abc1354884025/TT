# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2022.3.62f3c1 game targeting the **Douyin (TikTok) mini-game platform** via ByteDance TTSDK 6.7.7. Uses URP rendering and HybridCLR for code hot-update. The project is in early scaffolding stage — a UI framework is built, but no game logic exists beyond a test panel.

## Build & Run

- **Develop**: Open the project in Unity Editor 2022.3.62f3c1, open `Assets/Scenes/SampleScene.unity`, press Play.
- **Build for Douyin**: Use the ByteDance StarkSDK build tools in the Unity Editor menu (configured via `Assets/Editor/StarkBuilderSetting.asset`). Outputs a WebGL zip to `E:\TT\output\`.
- **Run on Douyin**: Import the output zip into the Douyin Developer Tools IDE (`抖音开发者工具`).
- **Tests**: Unity Test Framework 1.1.33 is installed but no tests exist yet. Run via Unity Test Runner window.
- **Linting/Formatting**: No linter or formatter is configured. No `.editorconfig` exists.

## Assembly Architecture (7 Assemblies)

The project is partitioned into 7 Unity assemblies via `.asmdef` files. The critical design constraint is the **AOT / HotUpdate split**:

```
Entry ──► UIFramework ──► Unity.TextMeshPro
               ▲
               │
          HotUpdate  (autoReferenced: false)
```

- **`UIFramework`** — AOT framework layer. UI manager, panel lifecycle, data binding, resource loading. All framework code lives here.
- **`Entry`** — AOT bootstrap. Contains `HotUpdateBootstrap` that downloads and loads the hot-update DLL at runtime.
- **`HotUpdate`** (`autoReferenced: false`) — Hot-update game code. Compiled separately, downloaded from CDN, loaded via `Assembly.Load(byte[])`. References `UIFramework` but is NOT referenced by any AOT assembly.
- **`TTLitJson`** — Bundled JSON library (part of TTSDK).
- **`TTWebGL`** — ByteDance WebGL platform layer for TTSDK.
- **`com.bytedance.ttsdk.ref`** / **`com.bytedance.ttsdk-editor`** — ByteDance SDK assemblies.

`HotUpdate.asmdef` has `autoReferenced: false` — this is essential. It prevents the hot-update code from being compiled into the AOT build; it is intended to be compiled separately and loaded at runtime.

## Core Architecture: HybridCLR Hot-Update

Code is separated into two tiers loaded at different times:

**AOT Layer** (compiled into the base Unity build):
- `UIFramework` assembly — UI system, bindings, resource providers
- `Entry` assembly — `HotUpdateBootstrap` MonoBehaviour

**Hot-Update Layer** (compiled separately, downloaded from CDN at runtime):
- `HotUpdate` assembly — Game panels, ViewModels, game logic

### Startup Flow (`Assets/Scripts/Entry/HotUpdateBootstrap.cs`)

1. **Phase 1**: Initialize AOT framework — `UIManager.Instance` creates layer canvases
2. **Phase 2**: Download hot-update DLLs from CDN (e.g., `http://your-cdn.com/game/1.0.0/HotUpdate.dll`), load via `Assembly.Load(byte[])`, inject into `UIManager` via `SetHotUpdateAssembly()`
3. **Phase 3**: Download UI AssetBundles from CDN, register path mappings, switch `UIManager` to `AssetBundleProvider`
4. **Phase 4**: Open the initial panel by name — type is found via reflection from the hot-update assembly

Fallback: if CDN is unavailable, falls back to `ResourcesProvider` (local `Resources/` folder).

## UI Framework (MVVM-Style)

The UI framework in `Assets/Scripts/Framework/UI/` is the heart of the project.

### Core Types

- **`UIManager`** (`Core/UIManager.cs`): MonoSingleton that manages a 5-layer Canvas stack (`Background=0`, `Normal=100`, `Popup=200`, `Top=300`, `System=400`). After `SetHotUpdateAssembly()` is called, `Open("PanelName")` uses reflection to find the panel type in the hot-update DLL and instantiate it. Supports `Open<T>()` for AOT-known types and `Open(string)` for hot-update types.

- **`UIPanel`** (`Core/UIPanel.cs`): Abstract base class for all panels. Lifecycle: `OnInit()` (coroutine) → `OnOpen(data)` → `OnShow()` ⇄ `OnHide()` → `OnClose()`. Supports `CacheOnClose` for panel pooling, and `AutoBindings` for inspector-driven field assignment (sets fields by name via reflection on `OnOpen`).

- **`UIWidget`** (`Components/UIWidget.cs`): Lightweight sub-component for reusable UI pieces within panels. Not managed by the layer stack.

- **`UIList`** (`Components/UIList.cs`): Object-pooled scrollable list using `ObjectPool<GameObject>`. Configurable horizontal/vertical direction. For shops, inventories, leaderboards, etc.

### Data Binding (Zero-Reflection, IL2CPP-Safe)

- **`BindableProperty<T>`** (`Binding/BindableProperty.cs`): Generic reactive property using delegates, not reflection — safe for IL2CPP AOT compilation. Fires `OnChanged` only when the value actually changes (equality check via `EqualityComparer<T>.Default`).

- **`ObservableObject`** (`Binding/ObservableObject.cs`): ViewModel base class.

- **`UIBindingExtensions`** (`Binding/UIBindingExtensions.cs`): One-liner binding methods on Unity UI components. Every bind method returns an `Action` unsubscribe delegate — panels collect these in a `List<Action> _unbind` and invoke all on close. Supports `TMP_Text`, `Text`, `Image`, `Button`, `Slider`, `Toggle`, `TMP_InputField`, `GameObject.SetActive`. Includes two-way binding for `Slider` and `Toggle`.

### Resource Loading

- **`IResourceProvider`** (`Resource/IResourceProvider.cs`): Abstraction over resource loading. Two implementations:
  - `ResourcesProvider` — Unity `Resources` API (dev/local/fallback)
  - `AssetBundleProvider` — CDN-downloaded AssetBundles (hot-update/production)
- Both support async loading via coroutines. `PanelCache` pools closed panels for reuse.

## Key Conventions

1. **Panels are found by class name**: `Open("TestPanel")` looks for a class literally named `TestPanel` in the hot-update assembly (exact name match first, then namespace-agnostic match).

2. **All bindings return unsubscribe Actions**: Every bind method returns `Action` — collect them and invoke on close to prevent leaks.

3. **Coroutine-based async, not async/await**: Resource loading and panel initialization use `IEnumerator` coroutines with `yield return new WaitUntil(...)` patterns.

4. **HotUpdate types must NOT be referenced in AOT code**: Since `HotUpdate.asmdef` has `autoReferenced: false` and no AOT assembly references it, AOT code cannot directly reference hot-update types. Communication goes through `UIPanel` base class (in AOT) and reflection.

5. **MonoSingleton<T>** for framework singletons: Thread-safe, survives scene loads via `DontDestroyOnLoad`.

## Platform Constraints

- Target platform is **WebGL** (Douyin mini-game runs in a WebView/JS environment).
- IL2CPP AOT compilation means **no runtime reflection on generic types** — `BindableProperty<T>` works because it uses generic delegates, not `System.Reflection` on value types.
- ByteDance SDK provides custom WebGL interop (`.jslib` files), custom AssetBundle loading (`TTAssetBundle`), and file system APIs (`TTFileSystemManager`).
