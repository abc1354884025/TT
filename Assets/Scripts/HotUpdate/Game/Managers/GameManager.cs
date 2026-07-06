using UnityEngine;

/// <summary>
/// 游戏管理器——全局状态机。挂载在场景常驻 GameObject 上。
///
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
        // 加载配置
        ConfigLoader.LoadAll();

        // 初始化 UIManager（如果没有）
        var ui = UIManager.Instance;
        ui.SetHotUpdateAssembly(typeof(GameManager).Assembly);

        // 打开主菜单
        CurrentState = State.MainMenu;
        ui.Open(_startPanel);
    }

    public void GoToPrepare()
    {
        CurrentState = State.Prepare;
        CurrentRound++;
    }

    public void GoToBattle()
    {
        CurrentState = State.Battle;
    }

    public void GoToReward()
    {
        CurrentState = State.Reward;
    }

    public void GoToMainMenu()
    {
        CurrentState = State.MainMenu;
        CurrentRound = 1;
    }
}
