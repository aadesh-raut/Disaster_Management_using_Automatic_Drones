using UnityEngine;

/// <summary>
/// Controls the charging pad visual state for an assigned drone.
/// The ring (charging-pad-round_4) turns ON when the drone lands and is charging (battery < 100%).
/// The ring turns OFF when the drone reaches 100% or is dispatched on a mission.
/// </summary>
public class ChargingPadController : MonoBehaviour
{
    [Header("Assigned Drone")]
    [Tooltip("The drone that parks on this charging pad.")]
    public DroneRouteController targetDrone;

    [Header("Visual Components")]
    [Tooltip("The child mesh representing the glow ring (charging-pad-round_2).")]
    public GameObject glowRing;

    [Tooltip("Optional: A point light on the pad for real-time ground/drone lighting.")]
    public Light glowLight;

    private bool isGlowActive = false;

    void Start()
    {
        // Auto-find charging-pad-round_4 if not manually dragged into inspector
        if (glowRing == null)
        {
            Transform ringTransform = transform.Find("charging-pad-round_2");
            if (ringTransform == null)
            {
                // Deeper search if model has a nested root
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("round_2") || child.name.ToLower().Contains("glow"))
                    {
                        ringTransform = child;
                        break;
                    }
                }
            }

            if (ringTransform != null)
            {
                glowRing = ringTransform.gameObject;
            }
        }

        // Start with the ring turned OFF
        SetGlow(false);
    }

    void Update()
    {
        if (targetDrone == null)
            return;

        // targetDrone.IsCharging is true ONLY when:
        // 1. The drone is parked at its home position
        // 2. The drone is NOT on a mission
        // 3. The battery is < 100%
        bool shouldGlow = targetDrone.IsCharging;

        if (shouldGlow != isGlowActive)
        {
            SetGlow(shouldGlow);
        }
    }

    private void SetGlow(bool active)
    {
        isGlowActive = active;

        // Turn the ring mesh on/off
        if (glowRing != null)
        {
            glowRing.SetActive(active);
        }

        // Turn optional light on/off
        if (glowLight != null)
        {
            glowLight.enabled = active;
        }
    }
}