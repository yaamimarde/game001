using UnityEngine;

/// <summary>
/// 集中读取玩家输入，供移动、攻击、相机等系统复用。
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool DashPressedThisFrame { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
    public bool AttackPressedThisFrame { get; private set; }
    public bool RotateLeftPressedThisFrame { get; private set; }
    public bool RotateRightPressedThisFrame { get; private set; }

    void Update()
    {
        Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        MoveInput = move;
        IsRunning = Input.GetKey(KeyCode.LeftShift);
        DashPressedThisFrame = Input.GetKeyDown(KeyCode.Space);
        JumpPressedThisFrame = Input.GetKeyDown(KeyCode.LeftControl);
        AttackPressedThisFrame = Input.GetMouseButtonDown(0);
        RotateLeftPressedThisFrame = Input.GetKeyDown(KeyCode.Q);
        RotateRightPressedThisFrame = Input.GetKeyDown(KeyCode.E);
    }
}
