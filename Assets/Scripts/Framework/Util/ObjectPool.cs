using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型对象池。支持 GameObject 和普通 C# 对象。
/// </summary>
public class ObjectPool<T> where T : class
{
    private readonly Queue<T> _pool = new Queue<T>();
    private readonly Func<T> _createFunc;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;
    private readonly Action<T> _onDestroy;
    private readonly int _maxSize;

    public ObjectPool(
        Func<T> createFunc,
        Action<T> onGet = null,
        Action<T> onRelease = null,
        Action<T> onDestroy = null,
        int maxSize = int.MaxValue)
    {
        _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        _onGet = onGet;
        _onRelease = onRelease;
        _onDestroy = onDestroy;
        _maxSize = maxSize;
    }

    public int Count => _pool.Count;

    public T Get()
    {
        T obj = _pool.Count > 0 ? _pool.Dequeue() : _createFunc();
        _onGet?.Invoke(obj);
        return obj;
    }

    public void Release(T obj)
    {
        if (obj == null) return;
        if (_pool.Count >= _maxSize) { _onDestroy?.Invoke(obj); return; }
        _onRelease?.Invoke(obj);
        _pool.Enqueue(obj);
    }

    public void Clear()
    {
        while (_pool.Count > 0) _onDestroy?.Invoke(_pool.Dequeue());
    }
}

/// <summary>
/// GameObject 专用对象池工厂
/// </summary>
public static class GameObjectPool
{
    public static ObjectPool<GameObject> Create(GameObject prefab, Transform parent = null, int maxSize = int.MaxValue)
    {
        return new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = parent ? UnityEngine.Object.Instantiate(prefab, parent) : UnityEngine.Object.Instantiate(prefab);
                return go;
            },
            onGet: go => { if (go) go.SetActive(true); },
            onRelease: go =>
            {
                if (!go) return;
                go.SetActive(false);
                if (parent) go.transform.SetParent(parent);
            },
            onDestroy: go => { if (go) UnityEngine.Object.Destroy(go); },
            maxSize: maxSize
        );
    }
}
