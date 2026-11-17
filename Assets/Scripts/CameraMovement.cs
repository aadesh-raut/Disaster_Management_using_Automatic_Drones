using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;

    private Vector2 moveInput;
    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Keep the cursor visible and free by default so the user can interact with the UI dashboard
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Sync initial rotation with the camera's starting angles
        Vector3 currentRotation = transform.localEulerAngles;
        rotationX = currentRotation.x;
        rotationY = currentRotation.y;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Hold RIGHT MOUSE BUTTON to look around and fly the camera (Unity Editor style).
        // This leaves LEFT CLICK 100% free for dashboard UI (inputs, dropdowns, buttons).
        if (mouse.rightButton.isPressed)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            HandleMouseLook();
            HandleMovement();
        }
        else
        {
            // When right click is released, return cursor to free mode immediately
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void HandleMouseLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;

        rotationY += mouseDelta.x;
        rotationX -= mouseDelta.y;

        // Prevent the camera from flipping upside down
        rotationX = Mathf.Clamp(rotationX, -89f, 89f);

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    private void HandleMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        moveInput = Vector2.zero;

        // WASD fly controls
        if (keyboard.wKey.isPressed) moveInput.y += 1f;
        if (keyboard.sKey.isPressed) moveInput.y -= 1f;
        if (keyboard.aKey.isPressed) moveInput.x -= 1f;
        if (keyboard.dKey.isPressed) moveInput.x += 1f;

        moveInput = moveInput.normalized;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y) + (right * moveInput.x);

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
}
