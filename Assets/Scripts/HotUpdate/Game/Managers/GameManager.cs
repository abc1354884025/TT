using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏管理器——全局状态机。
/// 由 HotUpdateBootstrap 完成框架初始化后调用 Init()。
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Loading, Login, MainMenu, Prepare, Battle, Reward }

    public static GameManager Instance { get; private set; }
    public State CurrentState { get; private set; } = State.Loading;
    public int CurrentRound { get; set; } = 1;
    public TTLoginResult LoginResult { get; private set; }

    [SerializeField] private string _startPanel = "MainMenuPanel";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // UIManager 在 HotUpdateBootstrap.Awake 中已初始化，Start 晚于 Awake
        StartCoroutine(InitSequence());
    }

    private IEnumerator InitSequence()
    {
        // 1. 加载配置
        ConfigLoader.LoadAll();

        // 2. 注入热更 Assembly（此时 HotUpdateBootstrap 已加载完 DLL）
        UIManager.Instance.SetHotUpdateAssembly(typeof(GameManager).Assembly);

        // 3. 发起登录
        CurrentState = State.Login;
        Debug.Log("[GameManager] 等待登录...");

        if (TTLoginBridge.Instance != null)
        {
            bool done = false;
            TTLoginBridge.Instance.OnLoginComplete += (r) => { LoginResult = r; done = true; };
            TTLoginBridge.Instance.Login();
            float timeout = Time.realtimeSinceStartup + 5f;
            yield return new WaitUntil(() => done || Time.realtimeSinceStartup > timeout);
            Debug.Log($"[GameManager] 登录{(LoginResult?.success == true ? "成功" : "失败/超时")}");
        }

        // 4. 打开主菜单
        CurrentState = State.MainMenu;
        UIManager.Instance.Open(_startPanel);
    }

    public void GoToPrepare() { CurrentState = State.Prepare; CurrentRound++; }
    public void GoToBattle()   { CurrentState = State.Battle; }
    public void GoToReward()   { CurrentState = State.Reward; }
    public void GoToMainMenu() { CurrentState = State.MainMenu; CurrentRound = 1; }
}
