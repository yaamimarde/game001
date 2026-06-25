using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 2.5D 透视相机：跟随目标，X 轴倾斜俯视，Z 轴轨道旋转（Q/E 按一次转 45°）。
/// </summary>
public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("X 轴俯仰角，负值表示从上往下看")]
        [SerializeField] float tiltAngle = -45f;
        [Tooltip("相机与焦点之间的直线距离")]
        [SerializeField] float followDistance = 14f;
        [Tooltip("焦点相对目标位置的偏移")]
        [SerializeField] Vector3 lookAtOffset = Vector3.zero;
        [SerializeField] float smoothTime = 0.15f;
        [Tooltip("Q/E 每次按下的旋转角度（度）")]
        [SerializeField] float stepAngle = 45f;
        [Tooltip("单次步进旋转的动画时长（秒）")]
        [SerializeField] float stepRotateDuration = 0.25f;

        float orbitAngle;
        float targetOrbitAngle;
        float rotateStartAngle;
        float rotateElapsed;
        bool isRotating;
        Vector3 velocity;
        PlayerInputReader input;

        public bool IsRotating => isRotating;

        void Awake()
        {
            input = FindObjectOfType<PlayerInputReader>();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureTarget();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode) => EnsureTarget();

        void EnsureTarget()
        {
            if (target != null)
                return;

            if (PlayerBootstrap.PlayerTransform != null)
            {
                target = PlayerBootstrap.PlayerTransform;
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        void Update()
        {
            if (!isRotating)
            {
                if (GetRotateLeftPressed())
                    BeginStepRotation(-stepAngle);
                else if (GetRotateRightPressed())
                    BeginStepRotation(stepAngle);
            }

            if (!isRotating)
                return;

            rotateElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(rotateElapsed / stepRotateDuration);
            orbitAngle = Mathf.Lerp(rotateStartAngle, targetOrbitAngle, Mathf.SmoothStep(0f, 1f, t));

            if (t >= 1f)
            {
                orbitAngle = targetOrbitAngle;
                isRotating = false;
            }
        }

        bool GetRotateLeftPressed()
        {
            if (input != null)
                return input.RotateLeftPressedThisFrame;
            return Input.GetKeyDown(KeyCode.Q);
        }

        bool GetRotateRightPressed()
        {
            if (input != null)
                return input.RotateRightPressedThisFrame;
            return Input.GetKeyDown(KeyCode.E);
        }

        void BeginStepRotation(float deltaAngle)
        {
            rotateStartAngle = orbitAngle;
            targetOrbitAngle = orbitAngle + deltaAngle;
            rotateElapsed = 0f;
            isRotating = true;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            Quaternion zOrbit = Quaternion.AngleAxis(orbitAngle, Vector3.forward);
            Quaternion xTilt = Quaternion.Euler(tiltAngle, 0f, 0f);

            Vector3 focusPoint = target.position + lookAtOffset;
            Vector3 offset = zOrbit * (xTilt * Vector3.back * followDistance);
            Vector3 desiredPosition = focusPoint + offset;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref velocity, smoothTime);
            transform.rotation = zOrbit * xTilt;
        }
    }
