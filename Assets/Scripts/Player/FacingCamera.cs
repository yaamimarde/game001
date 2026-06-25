using UnityEngine;

[DefaultExecutionOrder(100)]
public class FacingCamera : MonoBehaviour
{
    UnityEngine.Camera mainCamera;

    void Awake()
    {
        mainCamera = UnityEngine.Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = UnityEngine.Camera.main;

        if (mainCamera == null)
            return;

        transform.rotation = mainCamera.transform.rotation;
    }
}
