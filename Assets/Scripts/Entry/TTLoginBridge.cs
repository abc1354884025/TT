using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// tt.login JS 桥接（AOT 层）。WebGL 运行时调用 .jslib 中的 TTLogin 函数。
/// 结果通过 SendMessage 回调到指定的 GameObject 上。
/// </summary>
public class TTLoginBridge : MonoBehaviour
{
    public static TTLoginBridge Instance { get; private set; }

    /// <summary>登录完成回调</summary>
    public event Action<TTLoginResult> OnLoginComplete;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void TTLogin(string gameObjectName, string callbackMethod);
#else
    private static void TTLogin(string gameObjectName, string callbackMethod)
    {
        // Editor/非WebGL 环境模拟登录成功
        Debug.Log("[TTLogin] 模拟登录（非 WebGL 环境）");
        var go = GameObject.Find(gameObjectName);
        if (go)
            go.SendMessage(callbackMethod, "{\"success\":true,\"code\":\"editor_mock_code\"}");
    }
#endif

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>发起登录</summary>
    public void Login()
    {
        Debug.Log("[TTLogin] 发起 tt.login...");
        TTLogin(gameObject.name, "OnTTLoginResult");
    }

    /// <summary>JS 回调入口（由 SendMessage 调用）</summary>
    private void OnTTLoginResult(string json)
    {
        Debug.Log($"[TTLogin] 收到回调: {json}");
        var result = JsonUtility.FromJson<TTLoginResult>(json);
        OnLoginComplete?.Invoke(result);
    }
}

/// <summary>tt.login 返回结果</summary>
[Serializable]
public class TTLoginResult
{
    public bool success;
    public string code;
    public string anonymousCode;
    public string errMsg;
}
