using UnityEngine;

/// <summary>
/// 跨场景持久化玩家单例。重复实例会被销毁。
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerBootstrap : MonoBehaviour
{
    public static PlayerBootstrap Instance { get; private set; }

    public static Transform PlayerTransform =>
        Instance != null ? Instance.transform : null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
