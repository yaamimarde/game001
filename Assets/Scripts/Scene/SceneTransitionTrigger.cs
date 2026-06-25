using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 触发区域：玩家进入后自动加载目标场景，并设置目标场景的出生点 ID。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] string targetSceneName = "HouseInterior";
    [SerializeField] string spawnPointId = "InteriorEntrance";

    bool isLoading;

    void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading || !other.CompareTag("Player"))
            return;

        isLoading = true;
        SceneTransitionContext.NextSpawnPointId = spawnPointId;
        SceneManager.LoadScene(targetSceneName);
    }
}
