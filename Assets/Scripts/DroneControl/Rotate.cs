using UnityEngine;

public class Rotate : MonoBehaviour
{
    [Header("Speed Settings")]
    public float maxSpinSpeed = 1500f;

    [Header("Time Settings (Seconds)")]
    public float timeToMaxSpeed = 2f;
    public float timeToStop = 5f;

    [Header("Status")]
    public bool isMaxSpeedReached = false;

    private MoveDrone drone;
    private float currentSpinSpeed = 0f;

    private void Start()
    {
        drone = GetComponentInParent<MoveDrone>();

        if (drone == null)
        {
            Debug.LogError($"Rotate script on {gameObject.name} cannot find MoveDrone on its parent!");
        }
    }

    void Update()
    {
        if (drone == null) return;

        if (drone.IsDroneMoving)
        {
            // Accelerate normally to full speed
            currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, maxSpinSpeed, (maxSpinSpeed / timeToMaxSpeed) * Time.deltaTime);
        }
        else
        {
            // NEW RULE: Only decelerate if the drone is NOT moving AND it has physically hit the ground (0.2 height)
            if (drone.IsDroneCompletelyGrounded())
            {
                currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, 0f, (maxSpinSpeed / timeToStop) * Time.deltaTime);
            }
            // Note: If it's descending but hasn't touched the ground yet, currentSpinSpeed stays at maxSpinSpeed!
        }

        // Apply rotation
        if (currentSpinSpeed > 0f)
        {
            transform.Rotate(0f, currentSpinSpeed * Time.deltaTime, 0f, 0);
        }

        isMaxSpeedReached = Mathf.Approximately(currentSpinSpeed, maxSpinSpeed);
    }
}
