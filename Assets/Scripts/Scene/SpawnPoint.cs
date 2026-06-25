using UnityEngine;

/// <summary>
/// 标记场景中的玩家出生点，由 PlayerSpawnPoint 按 spawnId 匹配。
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] string spawnId;

    public string SpawnId => spawnId;
}
