
// using UnityEngine;

// public class RooftopIncidentDetector : MonoBehaviour
// {
//     [Header("City References")]
//     public Transform buildingsRoot;
//     public Transform waypointsParent;

//     [Header("Detection Settings")]
//     public float raycastHeight = 100f;
//     public float rooftopHoverHeight = 3f;

//     public Vector3 IncidentPosition { get; private set; }
//     public Transform NearestWaypoint { get; private set; }
//     public bool IncidentFound { get; private set; }

//     void Start()
//     {
//         GenerateIncident();
//     }

//     public void GenerateIncident()
//     {
//         IncidentFound = false;

//         if (buildingsRoot == null || waypointsParent == null)
//         {
//             Debug.LogError(
//                 "Assign Buildings Root and Waypoints Parent in Inspector."
//             );
//             return;
//         }

//         Collider[] buildings =
//             buildingsRoot.GetComponentsInChildren<Collider>();

//         if (buildings.Length == 0)
//         {
//             Debug.LogError("No building Colliders found!");
//             return;
//         }

//         // Try random buildings until a rooftop is detected.
//         for (int attempt = 0; attempt < 30; attempt++)
//         {
//             Collider building =
//                 buildings[Random.Range(0, buildings.Length)];

//             Bounds bounds = building.bounds;

//             float x = Random.Range(bounds.min.x, bounds.max.x);
//             float z = Random.Range(bounds.min.z, bounds.max.z);

//             Vector3 rayOrigin = new Vector3(
//                 x,
//                 bounds.max.y + raycastHeight,
//                 z
//             );

//             Ray ray = new Ray(rayOrigin, Vector3.down);

//             if (Physics.Raycast(
//                 ray,
//                 out RaycastHit hit,
//                 raycastHeight * 2f + bounds.size.y
//             ))
//             {
//                 // Only accept a hit on the selected building.
//                 if (hit.collider != building)
//                     continue;

//                 // Reject walls and accept upward-facing surfaces.
//                 if (Vector3.Dot(hit.normal, Vector3.up) < 0.8f)
//                     continue;

//                 IncidentPosition = hit.point;

//                 NearestWaypoint =
//                     FindNearestWaypoint(IncidentPosition);

//                 if (NearestWaypoint == null)
//                 {
//                     Debug.LogError("No waypoints found!");
//                     return;
//                 }

//                 IncidentFound = true;

//                 Debug.Log("Incident rooftop: " + IncidentPosition);
//                 Debug.Log("Nearest waypoint: " + NearestWaypoint.name);

//                 Debug.DrawRay(
//                     IncidentPosition,
//                     Vector3.up * 10f,
//                     Color.red,
//                     20f
//                 );

//                 return;
//             }
//         }

//         Debug.LogWarning(
//             "Rooftop not detected. Check building Colliders."
//         );
//     }

//     Transform FindNearestWaypoint(Vector3 position)
//     {
//         Transform nearest = null;
//         float shortestDistance = float.MaxValue;

//         foreach (Transform waypoint in waypointsParent)
//         {
//             float distance =
//                 Vector3.Distance(position, waypoint.position);

//             if (distance < shortestDistance)
//             {
//                 shortestDistance = distance;
//                 nearest = waypoint;
//             }
//         }

//         return nearest;
//     }

//     // public Vector3 GetRooftopApproachPosition()
//     // {
//     //     return IncidentPosition +
//     //            Vector3.up * rooftopHoverHeight;
//     // }

//     public Vector3 GetRooftopApproachPosition()
//     {
//         return IncidentPosition +
//             Vector3.up * rooftopHoverHeight;
//     }

//     public Vector3 GetRooftopLandingPosition()
//     {
//         return IncidentPosition;
//     }

// }



// <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>><<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>><<<<<<<<<<<<<<<<<<<<<<<<<<<<<>><<<<<<<<<<<<<<<<<<<<<<<



using System.Collections.Generic;
using UnityEngine;

public class RooftopIncidentDetector : MonoBehaviour
{
    [Header("City References")]
    public Transform buildingsRoot;
    public Transform waypointsParent;

    [Header("Detection Settings")]
    public float raycastHeight = 100f;
    public float rooftopHoverHeight = 3f;
    public float minimumIncidentSeparation = 5f;

    public Vector3 IncidentPosition { get; private set; }
    public Transform NearestWaypoint { get; private set; }
    public bool IncidentFound { get; private set; }

    // Called by the fleet manager when a new incident is requested.
    public bool GenerateIncident(List<Vector3> existingIncidents)
    {
        IncidentFound = false;
        NearestWaypoint = null;

        if (buildingsRoot == null || waypointsParent == null)
        {
            Debug.LogError(
                "Assign Buildings Root and Waypoints Parent in Inspector."
            );
            return false;
        }

        Collider[] buildings =
            buildingsRoot.GetComponentsInChildren<Collider>();

        if (buildings.Length == 0)
        {
            Debug.LogError("No building Colliders found!");
            return false;
        }

        for (int attempt = 0; attempt < 100; attempt++)
        {
            Collider building =
                buildings[Random.Range(0, buildings.Length)];

            Bounds bounds = building.bounds;

            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);

            Vector3 rayOrigin = new Vector3(
                x,
                bounds.max.y + raycastHeight,
                z
            );

            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                raycastHeight * 2f + bounds.size.y))
            {
                continue;
            }

            if (hit.collider != building)
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.8f)
                continue;

            // Avoid generating incidents too close to existing ones.
            bool tooClose = false;

            if (existingIncidents != null)
            {
                foreach (Vector3 existing in existingIncidents)
                {
                    if (Vector3.Distance(hit.point, existing)
                        < minimumIncidentSeparation)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }

            if (tooClose)
                continue;

            Transform nearest = FindNearestWaypoint(hit.point);

            if (nearest == null)
            {
                Debug.LogError("No waypoints found!");
                return false;
            }

            IncidentPosition = hit.point;
            NearestWaypoint = nearest;
            IncidentFound = true;

            Debug.Log("New incident rooftop: " + IncidentPosition);
            Debug.Log("Nearest waypoint: " + NearestWaypoint.name);

            Debug.DrawRay(
                IncidentPosition,
                Vector3.up * 10f,
                Color.red,
                20f
            );

            return true;
        }

        Debug.LogWarning(
            "Could not find a new rooftop sufficiently far from existing incidents."
        );

        return false;
    }

    Transform FindNearestWaypoint(Vector3 position)
    {
        Transform nearest = null;
        float shortestDistance = float.MaxValue;

        foreach (Transform waypoint in waypointsParent)
        {
            float distance =
                Vector3.Distance(position, waypoint.position);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = waypoint;
            }
        }

        return nearest;
    }

    public Vector3 GetRooftopApproachPosition()
    {
        return IncidentPosition + Vector3.up * rooftopHoverHeight;
    }

    public Vector3 GetRooftopLandingPosition()
    {
        return IncidentPosition;
    }
}

