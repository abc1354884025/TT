# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 2022.3.62f3c1 Douyin/TikTok mini-game. Source lives under `Assets/Scripts/`: `Entry` contains AOT bootstrap code, `Framework` contains shared UI/resource infrastructure, and `HotUpdate` contains game logic, managers, panels, view models, widgets, and config models. Scenes are in `Assets/Scenes/`; use `MainScene.unity` for editor development and `SampleScene.unity` for production bootstrap validation. Runtime config is in `Assets/Resources/Config/`, UI prefabs are in `Assets/Resources/UI/`, bundles are in `Bundles/`, and tools are in `Tools/`.

## Build, Test, and Development Commands

- Open Unity `2022.3.62f3c1`, load `Assets/Scenes/MainScene.unity`, then press Play for local development.
- Use Unity menu `HybridCLR/Generate/All` before refreshing hot-update DLL artifacts.
- Use Unity menu `YooAsset/AssetBundle Builder` to build bundles into `Bundles/`.
- Use Unity menu `Tools/YooAsset Uploader` to copy fallback catalog files and sync bundles plus `version.json` to TOS/CDN.
- Use ByteDance/StarkSDK build tools to create the WebGL zip configured by `Assets/Editor/StarkBuilderSetting.asset`.

## Coding Style & Naming Conventions

Write C# in the existing Unity style: four-space indentation, PascalCase for types/properties/methods, camelCase for locals and parameters, and `_camelCase` for private fields when used nearby. Prefer coroutines (`IEnumerator`, `yield return`) over `async/await`. Panel IDs must match class and prefab names: `PreparePanel` maps to `Assets/Resources/UI/Panels/PreparePanel.prefab` and `UIManager.Open("PreparePanel")`.

Keep the assembly boundary intact: `HotUpdate.asmdef` has `autoReferenced: false`; AOT assemblies (`Entry`, `UIFramework`) must not reference HotUpdate types directly. Communicate through AOT base types, reflection, or injected assemblies.

## Testing Guidelines

Unity Test Framework `1.1.33` is installed, but no tests are committed. Add EditMode tests for pure logic such as `BagGrid`, `ItemShape`, `CombatStats`, and `BattleResolver`; add PlayMode tests only for scene/UI flows. Place tests in `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`, and name files after the subject, for example `BagGridTests.cs`. Run tests from the Unity Test Runner before submitting changes.

## Commit & Pull Request Guidelines

Recent history uses Conventional Commit-style prefixes, often with Chinese descriptions, such as `fix: ...` and `refactor: ...`. Keep commits focused and use prefixes like `fix:`, `feat:`, `refactor:`, `test:`, or `docs:`.

Pull requests should describe the behavior change, list Unity validation, mention affected scenes/prefabs/configs, and include screenshots or clips for UI changes. Call out HybridCLR, YooAsset, CDN upload, or ByteDance integration changes explicitly.

## Agent-Specific Instructions

Preserve Unity `.meta` files with asset moves or additions. Do not edit generated `.csproj`, `Library/`, `Temp/`, or `Logs/` files unless the task explicitly requires it. When changing resource addresses, remember collectors use YooAsset `AddressByFileName`, so provider compatibility matters.
