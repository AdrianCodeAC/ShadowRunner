using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseLook : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private Rigidbody playerBody;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent.GetComponent<Rigidbody>();
        }

        Vector3 startAngles = transform.localEulerAngles;
        pitch = NormalizeAngle(startAngles.x);

        if (playerBody != null)
        {
            yaw = playerBody.rotation.eulerAngles.y;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector2 lookInput = ReadLookInput();

        yaw += lookInput.x * sensitivity;
        pitch = Mathf.Clamp(pitch - lookInput.y * sensitivity, minPitch, maxPitch);
    }

    private void FixedUpdate()
    {
        if (playerBody != null)
        {
            playerBody.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        }
    }

    private void LateUpdate()
    {
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue() * 0.01f;
        }

        return Vector2.zero;
#else
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
