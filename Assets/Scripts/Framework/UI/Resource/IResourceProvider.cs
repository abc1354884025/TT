using System;
using UnityEngine;

/// <summary>
/// 资源加载抽象接口。解耦加载方式，方便切换 Resources / AssetBundle / Addressables。
/// </summary>
public interface IResourceProvider
{
    void LoadAsync<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object;
    T Load<T>(string path) where T : UnityEngine.Object;
    void InstantiateAsync(string path, Transform parent, Action<GameObject> onLoaded);
    /// <summary>同步加载 Sprite（图标等小资源）</summary>
    Sprite LoadSprite(string path);

    void Release(string path);
    void DestroyInstance(GameObject instance);
}
