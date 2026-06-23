using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Transform cameraTransform;

    Rigidbody2D rb;
    Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();
    }

    void FixedUpdate()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            rb.velocity = movement * moveSpeed;
            return;
        }

        Vector3 forward = cameraTransform.forward;
        forward.z = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.up;
        else
            forward.Normalize();

        Vector3 right = cameraTransform.right;
        right.z = 0f;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;
        else
            right.Normalize();

        Vector3 worldMove = right * movement.x + forward * movement.y;
        if (worldMove.sqrMagnitude > 1f)
            worldMove.Normalize();

        rb.velocity = worldMove * moveSpeed;
    }
}
