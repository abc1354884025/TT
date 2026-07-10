using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏管理器——全局状态机。挂载在场景常驻 GameObject 上。
/// 状态流转：Login → MainMenu → Prepare → Battle → Reward → Prepare → ...
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Loading, Login, MainMenu, Prepare, Battle, Reward }

    public static GameManager Instance { get; private set; }
    public State CurrentState { get; private set; } = State.Loading;
    public int CurrentRound { get; set; } = 1;

    /// <summary>登录结果（可在其他面板读取）</summary>
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
        StartCoroutine(InitSequence());
    }

    private IEnumerator InitSequence()
    {
        // 1. 加载配置
        ConfigLoader.LoadAll();

        // 2. 初始化 UIManager
        var ui = UIManager.Instance;
        ui.SetHotUpdateAssembly(typeof(GameManager).Assembly);

        // 3. 发起登录，等待结果
        CurrentState = State.Login;
        Debug.Log("[GameManager] 等待登录...");

        if (TTLoginBridge.Instance != null)
        {
            bool loginDone = false;
            TTLoginBridge.Instance.OnLoginComplete += (result) =>
            {
                LoginResult = result;
                loginDone = true;
                Debug.Log($"[GameManager] 登录{(result.success ? "成功" : "失败")}: code={result.code}");
            };
            TTLoginBridge.Instance.Login();

            // 等待登录回调（超时 5 秒兜底）
            float timeout = Time.realtimeSinceStartup + 5f;
            yield return new WaitUntil(() => loginDone || Time.realtimeSinceStartup > timeout);
        }
        else
        {
            Debug.LogWarning("[GameManager] TTLoginBridge 不存在，跳过登录");
        }

        // 4. 打开主菜单
        CurrentState = State.MainMenu;
        ui.Open(_startPanel);
    }

    public void GoToPrepare() { CurrentState = State.Prepare; CurrentRound++; }
    public void GoToBattle()   { CurrentState = State.Battle; }
    public void GoToReward()   { CurrentState = State.Reward; }
    public void GoToMainMenu() { CurrentState = State.MainMenu; CurrentRound = 1; }
}
