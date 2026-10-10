using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏管理器——保留启动流程，等待接入新玩法。
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Loading, MainMenu }

    public static GameManager Instance { get; private set; }
    public State CurrentState { get; private set; } = State.Loading;

    [SerializeField] private string _startPanel = "MainMenuPanel";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(InitSequence());
    }

    private IEnumerator InitSequence()
    {
        // 等待 YooAsset 初始化完成（YooAssetBootstrap 在 Entry 中并行执行）
        Debug.Log("[GameManager] 等待 YooAsset 初始化...");
        yield return new WaitUntil(() => ResourceManager.IsYooAssetReady);
        Debug.Log("[GameManager] YooAsset 已就绪");

        SaveManager.Load();
        UIManager.Instance.SetHotUpdateAssembly(typeof(GameManager).Assembly);

        CurrentState = State.MainMenu;
        UIManager.Instance.Open(_startPanel);
    }
}
