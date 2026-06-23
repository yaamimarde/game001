using UnityEngine;

[DefaultExecutionOrder(100)]
public class FacingCamera : MonoBehaviour
{
    Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        transform.rotation = mainCamera.transform.rotation;
    }
}
