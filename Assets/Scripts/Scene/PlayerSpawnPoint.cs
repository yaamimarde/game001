using UnityEngine;

/// <summary>
/// 挂在 Player 上，场景加载后根据 SceneTransitionContext 移动到对应 SpawnPoint。
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    void Awake()
    {
        if (string.IsNullOrEmpty(SceneTransitionContext.NextSpawnPointId))
            return;

        string targetId = SceneTransitionContext.NextSpawnPointId;
        SceneTransitionContext.NextSpawnPointId = null;

        foreach (SpawnPoint point in FindObjectsOfType<SpawnPoint>())
        {
            if (point.SpawnId != targetId)
                continue;

            transform.position = point.transform.position;
            return;
        }

        Debug.LogError($"PlayerSpawnPoint: 找不到 spawnId=\"{targetId}\"，玩家留在当前位置。");
    }
}
