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
    }

    private void Start()
    {
        StartCoroutine(InitSequence());
    }

    private IEnumerator InitSequence()
    {
        // 1. 加载配置
        ConfigLoader.LoadAll();

        // 2. 注入热更 Assembly
        UIManager.Instance.SetHotUpdateAssembly(typeof(GameManager).Assembly);

        // 3. 打开主菜单
        CurrentState = State.MainMenu;
        UIManager.Instance.Open(_startPanel);
        yield break;
    }

    public void GoToPrepare() { CurrentState = State.Prepare; CurrentRound++; }
    public void GoToBattle()   { CurrentState = State.Battle; }
    public void GoToReward()   { CurrentState = State.Reward; }
    public void GoToMainMenu() { CurrentState = State.MainMenu; CurrentRound = 1; }
}
