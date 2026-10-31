using UnityEngine;
// Required header for Unity's Modern Input System
using UnityEngine.InputSystem;

public class MoveDrone : MonoBehaviour
{
    [Header("Movement State")]
    // Kept public so your wing script can still read it automatically
    public bool IsDroneMoving = false;

    [Header("Height Control")]
    public float targetLandingHeight = 0.2f;
    public float targetFlightHeight = 15f;
    public float travelTime = 15f;

    private Rotate wingScript;
    private float currentTargetY;

    void Start()
    {
        wingScript = GetComponentInChildren<Rotate>();

        if (wingScript == null)
        {
            Debug.LogError($"MoveDrone on {gameObject.name} cannot find a Rotate script on its child objects!");
        }

        // Initialize starting position at the landing height
        Vector3 startPos = transform.position;
        startPos.y = targetLandingHeight;
        transform.position = startPos;

        currentTargetY = targetLandingHeight;
    }

    void Update()
    {
        // 1. READ MODERN INPUT (Checks if keyboard is plugged in first)
        if (Keyboard.current != null)
        {
            // If the 'U' key is currently being pressed down
            if (Keyboard.current.uKey.wasPressedThisFrame)
            {
                IsDroneMoving = true;
            }

            // If the 'L' key is currently being pressed down
            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                IsDroneMoving = false;
            }
        }

        // 2. CALCULATE SPEED AND HEIGHT MOVEMENT
        float totalDistance = targetFlightHeight - targetLandingHeight;
        float moveSpeed = totalDistance / travelTime;
        Vector3 currentPosition = transform.position;

        if (IsDroneMoving)
        {
            // Only climb once the wings have reached maximum speed
            if (wingScript != null && wingScript.isMaxSpeedReached)
            {
                currentTargetY = targetFlightHeight;
            }
        }
        else
        {
            // Bring target back to ground height immediately
            currentTargetY = targetLandingHeight;
        }

        // Smoothly move the drone's Y coordinate toward the current target height
        currentPosition.y = Mathf.MoveTowards(currentPosition.y, currentTargetY, moveSpeed * Time.deltaTime);
        transform.position = currentPosition;
    }

    public bool IsDroneCompletelyGrounded()
    {
        return Mathf.Approximately(transform.position.y, targetLandingHeight);
    }
}
