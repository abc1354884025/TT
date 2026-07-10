using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏管理器——全局状态机。
/// 状态流转：MainMenu → Prepare → Battle → Reward → Prepare → ...
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Loading, MainMenu, Prepare, Battle, Reward }

    public static GameManager Instance { get; private set; }
    public State CurrentState { get; private set; } = State.Loading;
    public int CurrentRound { get; set; } = 1;

    [SerializeField] private string _startPanel = "MainMenuPanel";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 确保 SideBarManager 存在
        if (!FindObjectOfType<SideBarManager>())
        {
            var go = new GameObject("[SideBarManager]");
            go.transform.SetParent(transform);
            go.AddComponent<SideBarManager>();
        }
    }

    private void Start()
    {
        StartCoroutine(InitSequence());
    }

    private IEnumerator InitSequence()
    {
        // 1. 加载配置
        ConfigLoader.LoadAll();

        // 2. 初始化侧边栏
        SideBarManager.Instance.Init();

        // 3. 注入热更 Assembly
        UIManager.Instance.SetHotUpdateAssembly(typeof(GameManager).Assembly);

        // 4. 打开主菜单
        CurrentState = State.MainMenu;
        UIManager.Instance.Open(_startPanel);
        yield break;
    }

    /// <summary>打开侧边栏</summary>
    public void ShowSideBar()
    {
        SideBarManager.Instance?.Show();
    }

    public void GoToPrepare() { CurrentState = State.Prepare; CurrentRound++; }
    public void GoToBattle()   { CurrentState = State.Battle; }
    public void GoToReward()   { CurrentState = State.Reward; }
    public void GoToMainMenu() { CurrentState = State.MainMenu; CurrentRound = 1; }
}
