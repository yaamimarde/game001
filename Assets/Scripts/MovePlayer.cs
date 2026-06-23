using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayer : MonoBehaviour
{
    [Header("2D 移动设置")]
    public float moveSpeed = 6f;

    void Start()
    {

    }

    void Update()
    {
        // 1. 获取键盘输入
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D 或 左/右 键
        float verticalInput = Input.GetAxis("Vertical");     // W/S 或 上/下 键
        Vector3 moveDirection = new Vector3(horizontalInput, verticalInput, 0f);

        // 3. 让 2D 物体在平面的上下左右移动
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
    }
}