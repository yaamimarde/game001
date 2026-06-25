using UnityEngine;

/// <summary>
/// 追踪玩家移动距离，用于 Athletics 属性成长。
/// </summary>
public class PlayerProgressionTracker : MonoBehaviour
{
    Vector3 lastPos;

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.Session.IsActive) return;

        float dist = Vector3.Distance(transform.position, lastPos);
        if (dist > 0.01f)
            GameManager.Instance.Session.Progression?.RegisterDistanceMoved(dist);

        lastPos = transform.position;
    }
}
