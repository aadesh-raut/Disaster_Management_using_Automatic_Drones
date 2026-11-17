
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Networking;

// public class DroneFleetManager : MonoBehaviour
// {
//     [Header("City References")]
//     public Transform waypointsParent;
//     public RooftopIncidentDetector incidentDetector;

//     [Header("Drone Fleet")]
//     public DroneRouteController[] drones;

//     [Header("Dispatch Settings")]
//     public int baseSector = 0;
//     public KeyCode incidentKey = KeyCode.Space;

//     private readonly List<Vector3> existingIncidents =
//         new List<Vector3>();

//     private bool isProcessingIncident = false;

//     void Start()
//     {
//         if (waypointsParent == null ||
//             incidentDetector == null)
//         {
//             Debug.LogError(
//                 "Assign Waypoints Parent and Incident Detector."
//             );
//             enabled = false;
//             return;
//         }

//         Transform basePoint =
//             waypointsParent.Find("Sector_" + baseSector);

//         if (basePoint == null)
//         {
//             Debug.LogError("Base waypoint not found.");
//             enabled = false;
//             return;
//         }

//         if (drones == null || drones.Length == 0)
//         {
//             Debug.LogError("Add your drones to the Drone Fleet list.");
//             enabled = false;
//             return;
//         }

//         foreach (DroneRouteController drone in drones)
//         {
//             if (drone == null)
//                 continue;

//             drone.waypointsParent = waypointsParent;
//             drone.SetBase(basePoint);
//         }

//         Debug.Log(
//             "Drone fleet ready. Press " + incidentKey +
//             " in the Game view to create an incident."
//         );
//     }

//     void Update()
//     {
//         if (Input.anyKeyDown)
//         {
//             if (!isProcessingIncident)
//             {
//                 StartCoroutine(CreateAndDispatchIncident());
//             }
//             else
//             {
//                 Debug.Log(
//                     "Please wait for the current dispatch calculation."
//                 );
//             }
//         }
//     }

//     IEnumerator CreateAndDispatchIncident()
//     {
//         isProcessingIncident = true;

//         // Create a fresh rooftop incident.
//         bool found = incidentDetector.GenerateIncident(
//             existingIncidents
//         );

//         if (!found)
//         {
//             Debug.LogWarning("No new incident could be created.");
//             isProcessingIncident = false;
//             yield break;
//         }

//         Vector3 incidentPosition =
//             incidentDetector.IncidentPosition;

//         Transform nearestWaypoint =
//             incidentDetector.NearestWaypoint;

//         if (!int.TryParse(
//             nearestWaypoint.name.Replace("Sector_", ""),
//             out int destinationSector))
//         {
//             Debug.LogError("Invalid incident waypoint.");
//             isProcessingIncident = false;
//             yield break;
//         }

//         // Reserve this location so it won't be selected again.
//         existingIncidents.Add(incidentPosition);

//         Debug.Log(
//             "Incident created at Sector_" + destinationSector
//         );

//         // Request the outbound route.
//         RouteResponse outbound = null;

//         yield return StartCoroutine(RequestRoute(
//             baseSector,
//             destinationSector,
//             result => outbound = result
//         ));

//         if (outbound == null)
//         {
//             isProcessingIncident = false;
//             yield break;
//         }

//         // Request the return route.
//         RouteResponse returning = null;

//         yield return StartCoroutine(RequestRoute(
//             destinationSector,
//             baseSector,
//             result => returning = result
//         ));

//         if (returning == null)
//         {
//             isProcessingIncident = false;
//             yield break;
//         }

//         // Choose the eligible drone with the lowest battery.
//         DroneRouteController selectedDrone = null;

//         foreach (DroneRouteController drone in drones)
//         {
//             if (drone == null || !drone.IsAvailable)
//                 continue;

//             if (!drone.CanCompleteRoundTrip(
//                 outbound.total_time,
//                 returning.total_time))
//             {
//                 continue;
//             }

//             if (selectedDrone == null ||
//                 drone.battery < selectedDrone.battery)
//             {
//                 selectedDrone = drone;
//             }
//         }

//         if (selectedDrone == null)
//         {
//             Debug.LogWarning(
//                 "No available drone has enough battery for " +
//                 "the round trip. Incident remains unassigned."
//             );

//             isProcessingIncident = false;
//             yield break;
//         }

//         Debug.Log(
//             "Selected drone: " + selectedDrone.name +
//             " | Battery: " +
//             selectedDrone.battery.ToString("F1") + "%" +
//             " | Outbound cost: " + outbound.total_time +
//             " | Return cost: " + returning.total_time
//         );

//         // Dispatch immediately, including both routes.
//         selectedDrone.AssignMission(
//             incidentPosition,
//             destinationSector,
//             outbound.route,
//             returning.route,
//             outbound.total_time,
//             returning.total_time
//         );

//         isProcessingIncident = false;
//     }

//     IEnumerator RequestRoute(
//         int source,
//         int destination,
//         System.Action<RouteResponse> onComplete)
//     {
//         string url =
//             "http://127.0.0.1:8080/route?source=" +
//             source + "&destination=" + destination;

//         using (UnityWebRequest request = UnityWebRequest.Get(url))
//         {
//             yield return request.SendWebRequest();

//             if (request.result != UnityWebRequest.Result.Success)
//             {
//                 Debug.LogError(
//                     "Route request failed: " + request.error
//                 );

//                 onComplete(null);
//                 yield break;
//             }

//             RouteResponse response =
//                 JsonUtility.FromJson<RouteResponse>(
//                     request.downloadHandler.text
//                 );

//             if (response == null ||
//                 response.route == null ||
//                 response.route.Length == 0 ||
//                 response.source != source ||
//                 response.destination != destination ||
//                 response.route[0] != source ||
//                 response.route[response.route.Length - 1]
//                     != destination)
//             {
//                 Debug.LogError(
//                     "The API returned an invalid route."
//                 );

//                 onComplete(null);
//                 yield break;
//             }

//             onComplete(response);
//         }
//     }
// }


// <><><><><><><><><><><><><><><><><><><><><><><><>><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DroneFleetManager : MonoBehaviour
{
    [Header("City References")]
    public Transform waypointsParent;
    public RooftopIncidentDetector incidentDetector;

    [Header("Drone Fleet")]
    public DroneRouteController[] drones;

    [Header("Dispatch Settings")]
    public int baseSector = 0;
    public KeyCode incidentKey = KeyCode.Space;

    private readonly List<Vector3> existingIncidents =
        new List<Vector3>();

    private bool isProcessingIncident = false;

    void Start()
    {
        // Validate city references.
        if (waypointsParent == null ||
            incidentDetector == null)
        {
            Debug.LogError(
                "Assign Waypoints Parent and Incident Detector."
            );

            enabled = false;
            return;
        }

        // Find the shared Dijkstra base waypoint.
        Transform basePoint =
            waypointsParent.Find("Sector_" + baseSector);

        if (basePoint == null)
        {
            Debug.LogError(
                "Base waypoint Sector_" + baseSector +
                " was not found under Waypoints Parent."
            );

            enabled = false;
            return;
        }

        // Validate drone array.
        if (drones == null || drones.Length == 0)
        {
            Debug.LogError(
                "Add your drones to the Drone Fleet list."
            );

            enabled = false;
            return;
        }

        int validDroneCount = 0;

        foreach (DroneRouteController drone in drones)
        {
            if (drone == null)
            {
                Debug.LogWarning(
                    "One drone slot is empty in the Drone Fleet list."
                );

                continue;
            }

            // Assign the waypoint collection.
            drone.waypointsParent = waypointsParent;

            // IMPORTANT:
            // Each drone saves its own current position here.
            // Sector_0 is still the shared route waypoint.
            drone.SetBase(basePoint);

            validDroneCount++;

            Debug.Log(
                drone.name +
                " initialized. Personal parking position: " +
                drone.HomePosition
            );
        }

        if (validDroneCount == 0)
        {
            Debug.LogError(
                "No valid DroneRouteController components were assigned."
            );

            enabled = false;
            return;
        }

        Debug.Log(
            "Drone fleet ready. " + validDroneCount +
            " drone(s) initialized. Press " + incidentKey +
            " in the Game view to create an incident."
        );
    }

    void Update()
    {
        // Create an incident only when the chosen key is pressed.
        if (Input.GetKeyDown(incidentKey))
        {
            if (!isProcessingIncident)
            {
                StartCoroutine(CreateAndDispatchIncident());
            }
            else
            {
                Debug.Log(
                    "Please wait for the current dispatch calculation."
                );
            }
        }
    }

    IEnumerator CreateAndDispatchIncident()
    {
        isProcessingIncident = true;

        // Generate a new rooftop incident.
        bool found = incidentDetector.GenerateIncident(
            existingIncidents
        );

        if (!found)
        {
            Debug.LogWarning(
                "No new incident could be created."
            );

            isProcessingIncident = false;
            yield break;
        }

        Vector3 incidentPosition =
            incidentDetector.IncidentPosition;

        Transform nearestWaypoint =
            incidentDetector.NearestWaypoint;

        if (nearestWaypoint == null)
        {
            Debug.LogError(
                "Incident has no nearest waypoint."
            );

            isProcessingIncident = false;
            yield break;
        }

        // Extract the destination sector ID.
        if (!int.TryParse(
            nearestWaypoint.name.Replace("Sector_", ""),
            out int destinationSector))
        {
            Debug.LogError(
                "Invalid incident waypoint: " +
                nearestWaypoint.name
            );

            isProcessingIncident = false;
            yield break;
        }

        // Avoid selecting the same incident location again.
        existingIncidents.Add(incidentPosition);

        Debug.Log(
            "Incident created at Sector_" + destinationSector
        );

        // Request the outbound route.
        RouteResponse outbound = null;

        yield return StartCoroutine(RequestRoute(
            baseSector,
            destinationSector,
            result => outbound = result
        ));

        if (outbound == null)
        {
            isProcessingIncident = false;
            yield break;
        }

        // Request the return route.
        RouteResponse returning = null;

        yield return StartCoroutine(RequestRoute(
            destinationSector,
            baseSector,
            result => returning = result
        ));

        if (returning == null)
        {
            isProcessingIncident = false;
            yield break;
        }

        // Select the lowest-battery eligible drone.
        DroneRouteController selectedDrone = null;

        foreach (DroneRouteController drone in drones)
        {
            if (drone == null)
                continue;

            // Skip drones currently on a mission.
            if (!drone.IsAvailable)
                continue;

            // Skip drones without a valid saved parking location.
            if (Vector3.Distance(
                drone.transform.position,
                drone.HomePosition) > drone.landingTolerance)
            {
                Debug.Log(
                    drone.name +
                    " is not at its assigned parking position."
                );

                continue;
            }

            // Check if the drone has enough battery for
            // the round trip, including its reserve.
            if (!drone.CanCompleteRoundTrip(
                outbound.total_time,
                returning.total_time))
            {
                continue;
            }

            // Choose the eligible drone with the lowest battery.
            if (selectedDrone == null ||
                drone.battery < selectedDrone.battery)
            {
                selectedDrone = drone;
            }
        }

        if (selectedDrone == null)
        {
            Debug.LogWarning(
                "No parked, available drone has enough battery " +
                "for this round trip. Incident remains unassigned."
            );

            isProcessingIncident = false;
            yield break;
        }

        Debug.Log(
            "Selected drone: " + selectedDrone.name +
            " | Battery: " +
            selectedDrone.battery.ToString("F1") + "%" +
            " | Outbound cost: " + outbound.total_time +
            " | Return cost: " + returning.total_time
        );

        // Dispatch the selected drone.
        selectedDrone.AssignMission(
            incidentPosition,
            destinationSector,
            outbound.route,
            returning.route,
            outbound.total_time,
            returning.total_time
        );

        isProcessingIncident = false;
    }

    /// <summary>
    /// Dispatches an available drone to a specified sector for an incident request.
    /// Called by the interactive dashboard during batch commit.
    /// </summary>
    public IEnumerator DispatchIncidentSector(int destinationSector, int requestId, int priority, System.Action<bool> onComplete)
    {
        Transform targetWaypoint = waypointsParent != null ? waypointsParent.Find("Sector_" + destinationSector) : null;
        if (targetWaypoint == null)
        {
            Debug.LogError($"[FleetManager] Destination waypoint Sector_{destinationSector} not found under Waypoints Parent.");
            onComplete?.Invoke(false);
            yield break;
        }

        Vector3 incidentPosition = targetWaypoint.position;

        Debug.Log($"[FleetManager] Dispatching for Request #{requestId} (Sector {destinationSector}, Priority {priority})");

        // Request outbound route
        RouteResponse outbound = null;
        yield return StartCoroutine(RequestRoute(baseSector, destinationSector, res => outbound = res));

        if (outbound == null)
        {
            Debug.LogError($"[FleetManager] Failed to get outbound route to Sector {destinationSector}");
            onComplete?.Invoke(false);
            yield break;
        }

        // Request returning route
        RouteResponse returning = null;
        yield return StartCoroutine(RequestRoute(destinationSector, baseSector, res => returning = res));

        if (returning == null)
        {
            Debug.LogError($"[FleetManager] Failed to get return route from Sector {destinationSector}");
            onComplete?.Invoke(false);
            yield break;
        }

        // Select the eligible parked drone with lowest sufficient battery
        DroneRouteController selectedDrone = null;
        foreach (DroneRouteController drone in drones)
        {
            if (drone == null || !drone.IsAvailable)
                continue;

            if (Vector3.Distance(drone.transform.position, drone.HomePosition) > drone.landingTolerance)
                continue;

            if (!drone.CanCompleteRoundTrip(outbound.total_time, returning.total_time))
                continue;

            if (selectedDrone == null || drone.battery < selectedDrone.battery)
            {
                selectedDrone = drone;
            }
        }

        if (selectedDrone == null)
        {
            Debug.LogWarning($"[FleetManager] No eligible parked drone with sufficient battery for Sector {destinationSector}. Request #{requestId} remains queued.");
            onComplete?.Invoke(false);
            yield break;
        }

        Debug.Log($"[FleetManager] Dispatched {selectedDrone.name} to Request #{requestId} at Sector_{destinationSector}. Battery: {selectedDrone.battery:F1}%");

        selectedDrone.AssignMission(
            incidentPosition,
            destinationSector,
            outbound.route,
            returning.route,
            outbound.total_time,
            returning.total_time
        );

        onComplete?.Invoke(true);
    }

    IEnumerator RequestRoute(
        int source,
        int destination,
        System.Action<RouteResponse> onComplete)
    {
        string url =
            "http://127.0.0.1:8080/route?source=" +
            source + "&destination=" + destination;

        using (UnityWebRequest request =
            UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Route request failed: " + request.error
                );

                onComplete(null);
                yield break;
            }

            RouteResponse response =
                JsonUtility.FromJson<RouteResponse>(
                    request.downloadHandler.text
                );

            if (response == null ||
                response.route == null ||
                response.route.Length == 0 ||
                response.source != source ||
                response.destination != destination ||
                response.route[0] != source ||
                response.route[response.route.Length - 1]
                    != destination)
            {
                Debug.LogError(
                    "The API returned an invalid route."
                );

                onComplete(null);
                yield break;
            }

            onComplete(response);
        }
    }
}
