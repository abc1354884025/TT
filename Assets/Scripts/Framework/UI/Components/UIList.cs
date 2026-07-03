using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对象池滚动列表。适合 < 100 条的背包、商店、排行榜。
///
/// 用法：
///   list.SetData(myData, (go, item, i) => {
///       go.GetComponent<ShopItemWidget>().Init(item);
///   });
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class UIList : MonoBehaviour
{
    [Header("列表设置")]
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;

    [Header("布局")]
    [SerializeField] private float _itemSize = 100f;
    [SerializeField] private float _spacing = 0f;
    [SerializeField] private bool _isHorizontal = false;

    [Header("池")]
    [SerializeField] private int _prewarmCount = 0;

    private ObjectPool<GameObject> _itemPool;
    private readonly List<GameObject> _activeItems = new List<GameObject>();

    private void Awake()
    {
        if (!_scrollRect) _scrollRect = GetComponent<ScrollRect>();
        if (!_content && _scrollRect) _content = _scrollRect.content;
        if (_itemPrefab) InitPool();
    }

    private void InitPool()
    {
        _itemPool = GameObjectPool.Create(_itemPrefab, _content, 200);
        if (_prewarmCount > 0)
        {
            var temp = new List<GameObject>();
            for (int i = 0; i < _prewarmCount; i++) temp.Add(_itemPool.Get());
            foreach (var t in temp) _itemPool.Release(t);
        }
    }

    /// <summary>设置列表数据</summary>
    public void SetData<T>(IList<T> data, Action<GameObject, T, int> onBindItem)
    {
        if (!_itemPrefab) { Debug.LogError("[UIList] 未设置 ItemPrefab"); return; }
        if (_itemPool == null) InitPool();

        Clear();
        if (data == null || data.Count == 0) return;

        for (int i = 0; i < data.Count; i++)
        {
            var go = _itemPool.Get();
            _activeItems.Add(go);

            var rt = go.GetComponent<RectTransform>();
            if (rt)
            {
                if (_isHorizontal)
                {
                    rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = new Vector2(i * (_itemSize + _spacing), 0);
                    rt.sizeDelta = new Vector2(_itemSize, 0);
                }
                else
                {
                    rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    rt.anchoredPosition = new Vector2(0, -(i * (_itemSize + _spacing)));
                    rt.sizeDelta = new Vector2(0, _itemSize);
                }
            }

            onBindItem?.Invoke(go, data[i], i);
        }

        UpdateContentSize(data.Count);
    }

    public void Clear()
    {
        foreach (var item in _activeItems)
        {
            var li = item.GetComponent<UIListItem>();
            if (li) li.OnRecycle();
            _itemPool?.Release(item);
        }
        _activeItems.Clear();
    }

    public int ItemCount => _activeItems.Count;

    public GameObject GetItemAt(int i) => i >= 0 && i < _activeItems.Count ? _activeItems[i] : null;

    private void UpdateContentSize(int count)
    {
        if (!_content) return;
        float total = count * _itemSize + Mathf.Max(0, count - 1) * _spacing;
        if (_isHorizontal) _content.sizeDelta = new Vector2(total, _content.sizeDelta.y);
        else _content.sizeDelta = new Vector2(_content.sizeDelta.x, total);
    }
}

/// <summary>列表项基类。热更层继承此类做具体 Item。</summary>
public abstract class UIListItem : MonoBehaviour
{
    public int Index { get; protected set; }

    public virtual void SetData(object data, int index) { Index = index; }
    public virtual void OnRecycle() { Index = -1; }
}
