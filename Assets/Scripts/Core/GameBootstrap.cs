using UnityEngine;

/// <summary>
/// 确保 GameManager 单例存在。挂在 MainMenu 或首个加载场景。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] GameObject gameManagerPrefab;

    void Awake()
    {
        if (GameManager.Instance != null) return;

        if (gameManagerPrefab != null)
        {
            Instantiate(gameManagerPrefab);
            return;
        }

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        go.AddComponent<SceneAutoSave>();
    }
}
