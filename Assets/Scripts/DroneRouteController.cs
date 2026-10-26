
// using System.Collections;
// using UnityEngine;
// using UnityEngine.Networking;

// [System.Serializable]
// public class RouteResponse
// {
//     public int source;
//     public int destination;
//     public int[] route;
//     public int total_time;
// }

// public class DroneRouteController : MonoBehaviour
// {
//     [Header("References")]
//     public Transform waypointsParent;
//     public RooftopIncidentDetector incidentDetector;

//     [Header("Route Settings")]
//     public int sourceSector = 0;
//     public float speed = 5f;

//     [Header("Landing Settings")]
//     public float landingOffset = 1f;

//     private bool isMoving = false;

//     void Start()
//     {
//         if (waypointsParent == null)
//         {
//             Debug.LogError("Please assign CityWayPoints in the Inspector.");
//             return;
//         }

//         if (incidentDetector == null)
//         {
//             Debug.LogError(
//                 "Please assign IncidentManager to Incident Detector."
//             );
//             return;
//         }

//         StartCoroutine(StartMission());
//     }

//     IEnumerator StartMission()
//     {
//         // Wait one frame for the incident detector to run.
//         yield return null;

//         if (!incidentDetector.IncidentFound ||
//             incidentDetector.NearestWaypoint == null)
//         {
//             Debug.LogError("No rooftop incident or nearest waypoint found.");
//             yield break;
//         }

//         string sectorName = incidentDetector.NearestWaypoint.name;

//         if (!sectorName.StartsWith("Sector_") ||
//             !int.TryParse(
//                 sectorName.Substring("Sector_".Length),
//                 out int destinationSector))
//         {
//             Debug.LogError("Invalid waypoint name: " + sectorName);
//             yield break;
//         }

//         Debug.Log(
//             "Incident destination: " + sectorName
//         );

//         yield return StartCoroutine(
//             GetRoute(sourceSector, destinationSector)
//         );
//     }

//     IEnumerator GetRoute(int source, int destination)
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
//                 yield break;
//             }

//             RouteResponse response =
//                 JsonUtility.FromJson<RouteResponse>(
//                     request.downloadHandler.text
//                 );

//             if (response == null ||
//                 response.route == null ||
//                 response.route.Length == 0)
//             {
//                 Debug.LogError("The API returned an empty route.");
//                 yield break;
//             }

//             // Ensure the API returned the requested destination.
//             if (response.destination != destination ||
//                 response.route[response.route.Length - 1] != destination)
//             {
//                 Debug.LogError(
//                     "The returned route does not end at the incident waypoint."
//                 );
//                 yield break;
//             }

//             Debug.Log(
//                 "Route received: " +
//                 string.Join(" -> ", response.route)
//             );

//             Debug.Log("Total route cost: " + response.total_time);

//             yield return StartCoroutine(FollowRoute(response.route));
//         }
//     }

//     IEnumerator FollowRoute(int[] route)
//     {
//         if (route == null || route.Length == 0)
//             yield break;

//         Transform firstWaypoint = FindWaypoint(route[0]);

//         if (firstWaypoint == null)
//             yield break;

//         // Begin at the starting waypoint.
//         transform.position = firstWaypoint.position;
//         isMoving = true;

//         // Follow the C++ Dijkstra route.
//         for (int i = 1; i < route.Length; i++)
//         {
//             Transform target = FindWaypoint(route[i]);

//             if (target == null)
//             {
//                 isMoving = false;
//                 yield break;
//             }

//             while (Vector3.Distance(transform.position, target.position) > 0.05f)
//             {
//                 transform.position = Vector3.MoveTowards(
//                     transform.position,
//                     target.position,
//                     speed * Time.deltaTime
//                 );

//                 yield return null;
//             }

//             transform.position = target.position;
//         }

//         Debug.Log("Drone reached incident waypoint: Sector_" +
//                   route[route.Length - 1]);

//         // Move above the rooftop first.
//         Vector3 hoverPosition =
//             incidentDetector.GetRooftopApproachPosition();

//         Debug.Log("Approaching rooftop.");

//         while (Vector3.Distance(transform.position, hoverPosition) > 0.05f)
//         {
//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 hoverPosition,
//                 speed * Time.deltaTime
//             );

//             yield return null;
//         }

//         transform.position = hoverPosition;

//         // Descend to the roof surface.
//         Vector3 landingPosition =
//             incidentDetector.GetRooftopLandingPosition()
//             + Vector3.up * landingOffset;

//         Debug.Log("Descending to rooftop.");

//         while (Vector3.Distance(transform.position, landingPosition) > 0.05f)
//         {
//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 landingPosition,
//                 speed * Time.deltaTime
//             );

//             yield return null;
//         }

//         transform.position = landingPosition;
//         isMoving = false;

//         Debug.Log("Drone landed on rooftop!");
//     }

//     Transform FindWaypoint(int sectorID)
//     {
//         string waypointName = "Sector_" + sectorID;

//         Transform waypoint = waypointsParent.Find(waypointName);

//         if (waypoint == null)
//         {
//             Debug.LogError("Waypoint not found: " + waypointName);
//         }

//         return waypoint;
//     }
// }


//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<


// using System.Collections;
// using UnityEngine;
// using UnityEngine.Networking;

// [System.Serializable]
// public class RouteResponse
// {
//     public int source;
//     public int destination;
//     public int[] route;
//     public int total_time;
// }

// public class DroneRouteController : MonoBehaviour
// {
//     [Header("References")]
//     public Transform waypointsParent;
//     public RooftopIncidentDetector incidentDetector;

//     [Header("Route Settings")]
//     public int sourceSector = 0;
//     public float speed = 5f;

//     [Header("Landing Settings")]
//     public float landingOffset = 1f;

//     private bool isMoving = false;

//     void Start()
//     {
//         if (waypointsParent == null)
//         {
//             Debug.LogError("Please assign CityWayPoints in the Inspector.");
//             return;
//         }

//         if (incidentDetector == null)
//         {
//             Debug.LogError(
//                 "Please assign IncidentManager to Incident Detector."
//             );
//             return;
//         }

//         StartCoroutine(StartMission());
//     }

//     IEnumerator StartMission()
//     {
//         // Wait one frame for the incident detector to run.
//         yield return null;

//         if (!incidentDetector.IncidentFound ||
//             incidentDetector.NearestWaypoint == null)
//         {
//             Debug.LogError("No rooftop incident or nearest waypoint found.");
//             yield break;
//         }

//         string sectorName = incidentDetector.NearestWaypoint.name;

//         if (!sectorName.StartsWith("Sector_") ||
//             !int.TryParse(
//                 sectorName.Substring("Sector_".Length),
//                 out int destinationSector))
//         {
//             Debug.LogError("Invalid waypoint name: " + sectorName);
//             yield break;
//         }

//         Debug.Log("Incident destination: " + sectorName);

//         yield return StartCoroutine(
//             GetRoute(sourceSector, destinationSector)
//         );
//     }

//     IEnumerator GetRoute(int source, int destination)
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
//                 yield break;
//             }

//             RouteResponse response =
//                 JsonUtility.FromJson<RouteResponse>(
//                     request.downloadHandler.text
//                 );

//             if (response == null ||
//                 response.route == null ||
//                 response.route.Length == 0)
//             {
//                 Debug.LogError("The API returned an empty route.");
//                 yield break;
//             }

//             if (response.destination != destination ||
//                 response.route[response.route.Length - 1] != destination)
//             {
//                 Debug.LogError(
//                     "The returned route does not end at the incident waypoint."
//                 );
//                 yield break;
//             }

//             Debug.Log(
//                 "Route received: " +
//                 string.Join(" -> ", response.route)
//             );

//             Debug.Log("Total route cost: " + response.total_time);

//             yield return StartCoroutine(FollowRoute(response.route));
//         }
//     }

//     IEnumerator FollowRoute(int[] route)
//     {
//         if (route == null || route.Length == 0)
//             yield break;

//         Transform firstWaypoint = FindWaypoint(route[0]);

//         if (firstWaypoint == null)
//             yield break;

//         // Start at the existing starting waypoint.
//         transform.position = firstWaypoint.position;
//         isMoving = true;

//         // STEP 1: Rise vertically to 3 units above the incident's Y.
//         float targetY = incidentDetector.IncidentPosition.y + 3f;

//         Vector3 takeoffPosition = new Vector3(
//             transform.position.x,
//             targetY,
//             transform.position.z
//         );

//         Debug.Log("Drone taking off to Y = " + targetY);

//         while (Mathf.Abs(transform.position.y - targetY) > 0.05f)
//         {
//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 takeoffPosition,
//                 speed * Time.deltaTime
//             );

//             yield return null;
//         }

//         transform.position = takeoffPosition;

//         Debug.Log("Drone reached takeoff altitude: Y = " + targetY);

//         // STEP 2: Follow the existing C++ Dijkstra route.
//         for (int i = 1; i < route.Length; i++)
//         {
//             Transform target = FindWaypoint(route[i]);

//             if (target == null)
//             {
//                 isMoving = false;
//                 yield break;
//             }

//             while (Vector3.Distance(
//                 transform.position, target.position) > 0.05f)
//             {
//                 transform.position = Vector3.MoveTowards(
//                     transform.position,
//                     target.position,
//                     speed * Time.deltaTime
//                 );

//                 yield return null;
//             }

//             transform.position = target.position;
//         }

//         Debug.Log(
//             "Drone reached incident waypoint: Sector_" +
//             route[route.Length - 1]
//         );

//         // STEP 3: Move above the rooftop.
//         Vector3 hoverPosition =
//             incidentDetector.GetRooftopApproachPosition();

//         Debug.Log("Approaching rooftop.");

//         while (Vector3.Distance(
//             transform.position, hoverPosition) > 0.05f)
//         {
//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 hoverPosition,
//                 speed * Time.deltaTime
//             );

//             yield return null;
//         }

//         transform.position = hoverPosition;

//         // STEP 4: Descend to the rooftop landing position.
//         Vector3 landingPosition =
//             incidentDetector.GetRooftopLandingPosition()
//             + Vector3.up * landingOffset;

//         Debug.Log("Descending to rooftop.");

//         while (Vector3.Distance(
//             transform.position, landingPosition) > 0.05f)
//         {
//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 landingPosition,
//                 speed * Time.deltaTime
//             );

//             yield return null;
//         }

//         transform.position = landingPosition;
//         isMoving = false;

//         Debug.Log("Drone landed on rooftop!");
//     }

//     Transform FindWaypoint(int sectorID)
//     {
//         string waypointName = "Sector_" + sectorID;

//         Transform waypoint = waypointsParent.Find(waypointName);

//         if (waypoint == null)
//         {
//             Debug.LogError("Waypoint not found: " + waypointName);
//         }

//         return waypoint;
//     }
// }


// <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>


// using System.Collections;
// using UnityEngine;
// using UnityEngine.Networking;

// [System.Serializable]
// public class RouteResponse
// {
//     public int source;
//     public int destination;
//     public int[] route;
//     public int total_time;
// }

// public class DroneRouteController : MonoBehaviour
// {
//     [Header("References")]
//     public Transform waypointsParent;
//     public RooftopIncidentDetector incidentDetector;

//     [Header("Route Settings")]
//     public int sourceSector = 0;
//     public float speed = 5f;

//     [Header("Rooftop Settings")]
//     public float rooftopClearance = 3f;
//     public float landingOffset = 0.1f;

//     private bool isMoving = false;

//     void Start()
//     {
//         if (waypointsParent == null)
//         {
//             Debug.LogError("Please assign CityWayPoints in the Inspector.");
//             return;
//         }

//         if (incidentDetector == null)
//         {
//             Debug.LogError("Please assign IncidentManager to Incident Detector.");
//             return;
//         }

//         StartCoroutine(StartMission());
//     }

//     IEnumerator StartMission()
//     {
//         // Wait for the incident detector to find the incident.
//         yield return null;

//         if (!incidentDetector.IncidentFound ||
//             incidentDetector.NearestWaypoint == null)
//         {
//             Debug.LogError("No incident or nearest waypoint found.");
//             yield break;
//         }

//         string sectorName = incidentDetector.NearestWaypoint.name;

//         if (!sectorName.StartsWith("Sector_") ||
//             !int.TryParse(
//                 sectorName.Substring("Sector_".Length),
//                 out int destinationSector))
//         {
//             Debug.LogError("Invalid waypoint name: " + sectorName);
//             yield break;
//         }

//         Debug.Log("Incident destination: " + sectorName);

//         yield return StartCoroutine(
//             GetRoute(sourceSector, destinationSector)
//         );
//     }

//     IEnumerator GetRoute(int source, int destination)
//     {
//         string url =
//             "http://127.0.0.1:8080/route?source=" +
//             source + "&destination=" + destination;

//         using (UnityWebRequest request = UnityWebRequest.Get(url))
//         {
//             yield return request.SendWebRequest();

//             if (request.result != UnityWebRequest.Result.Success)
//             {
//                 Debug.LogError("Route request failed: " + request.error);
//                 yield break;
//             }

//             RouteResponse response =
//                 JsonUtility.FromJson<RouteResponse>(
//                     request.downloadHandler.text
//                 );

//             if (response == null ||
//                 response.route == null ||
//                 response.route.Length == 0)
//             {
//                 Debug.LogError("The API returned an empty route.");
//                 yield break;
//             }

//             if (response.destination != destination ||
//                 response.route[response.route.Length - 1] != destination)
//             {
//                 Debug.LogError(
//                     "The returned route does not end at the incident waypoint."
//                 );
//                 yield break;
//             }

//             Debug.Log(
//                 "Route received: " +
//                 string.Join(" -> ", response.route)
//             );

//             Debug.Log("Total route cost: " + response.total_time);

//             yield return StartCoroutine(FollowRoute(response.route));
//         }
//     }

//     IEnumerator FollowRoute(int[] route)
//     {
//         if (route == null || route.Length == 0)
//             yield break;

//         Transform firstWaypoint = FindWaypoint(route[0]);

//         if (firstWaypoint == null)
//             yield break;

//         isMoving = true;

//         // STEP 1: Move smoothly from the drone's actual position
//         // to the first waypoint. Do not teleport to Sector_0.
//         Debug.Log("Moving to starting waypoint: " + firstWaypoint.name);

//         yield return StartCoroutine(
//             MoveToPosition(firstWaypoint.position)
//         );

//         // STEP 2: Follow the remaining waypoints from the API.
//         for (int i = 1; i < route.Length; i++)
//         {
//             Transform target = FindWaypoint(route[i]);

//             if (target == null)
//             {
//                 isMoving = false;
//                 yield break;
//             }

//             Debug.Log("Flying to " + target.name);

//             yield return StartCoroutine(
//                 MoveToPosition(target.position)
//             );
//         }

//         Debug.Log(
//             "Reached incident waypoint: Sector_" +
//             route[route.Length - 1]
//         );

//         // STEP 3: Rise vertically above the incident location.
//         Vector3 incidentPosition = incidentDetector.IncidentPosition;

//         float targetY = incidentPosition.y + rooftopClearance;

//         Vector3 aboveWaypoint = new Vector3(
//             transform.position.x,
//             targetY,
//             transform.position.z
//         );

//         Debug.Log("Rising vertically to incident altitude: " + targetY);

//         yield return StartCoroutine(
//             MoveToPosition(aboveWaypoint)
//         );

//         // STEP 4: Move horizontally to the incident location,
//         // keeping the drone at the target altitude.
//         Vector3 aboveIncident = new Vector3(
//             incidentPosition.x,
//             targetY,
//             incidentPosition.z
//         );

//         Debug.Log("Moving above the rooftop incident.");

//         yield return StartCoroutine(
//             MoveToPosition(aboveIncident)
//         );

//         // STEP 5: Descend vertically and settle on the rooftop.
//         Vector3 landingPosition = incidentPosition
//             + Vector3.up * landingOffset;

//         Debug.Log("Descending vertically onto the rooftop.");

//         yield return StartCoroutine(
//             MoveToPosition(landingPosition)
//         );

//         isMoving = false;

//         Debug.Log("Drone has landed on the rooftop!");
//     }

//     IEnumerator MoveToPosition(Vector3 targetPosition)
//     {
//         while (Vector3.Distance(
//             transform.position, targetPosition) > 0.05f)
//         {
//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 targetPosition,
//                 speed * Time.deltaTime
//             );

//             yield return null;
//         }

//         transform.position = targetPosition;
//     }

//     Transform FindWaypoint(int sectorID)
//     {
//         string waypointName = "Sector_" + sectorID;

//         Transform waypoint = waypointsParent.Find(waypointName);

//         if (waypoint == null)
//         {
//             Debug.LogError("Waypoint not found: " + waypointName);
//         }

//         return waypoint;
//     }
// }

// <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>


// using System.Collections;
// using UnityEngine;
// using UnityEngine.Networking;

// [System.Serializable]
// public class RouteResponse
// {
//     public int source;
//     public int destination;
//     public int[] route;
//     public int total_time;
// }

// public class DroneRouteController : MonoBehaviour
// {
//     [Header("References")]
//     public Transform waypointsParent;
//     public RooftopIncidentDetector incidentDetector;

//     [Header("Route Settings")]
//     public int sourceSector = 0;
//     public float speed = 5f;

//     [Header("Rooftop Settings")]
//     public float rooftopClearance = 3f;
//     public float landingOffset = 0.1f;

//     [Header("Realistic Movement")]
//     public float takeoffAcceleration = 3f;
//     public float maxTakeoffSpeed = 8f;
//     public float movementAcceleration = 6f;
//     public float rotationSpeed = 120f;

//     private bool isMoving = false;

//     void Start()
//     {
//         if (waypointsParent == null)
//         {
//             Debug.LogError(
//                 "Please assign CityWayPoints in the Inspector."
//             );
//             return;
//         }

//         if (incidentDetector == null)
//         {
//             Debug.LogError(
//                 "Please assign IncidentManager to Incident Detector."
//             );
//             return;
//         }

//         StartCoroutine(StartMission());
//     }

//     IEnumerator StartMission()
//     {
//         // Wait for the incident detector to find the incident.
//         yield return null;

//         if (!incidentDetector.IncidentFound ||
//             incidentDetector.NearestWaypoint == null)
//         {
//             Debug.LogError(
//                 "No incident or nearest waypoint found."
//             );
//             yield break;
//         }

//         string sectorName =
//             incidentDetector.NearestWaypoint.name;

//         if (!sectorName.StartsWith("Sector_") ||
//             !int.TryParse(
//                 sectorName.Substring("Sector_".Length),
//                 out int destinationSector))
//         {
//             Debug.LogError(
//                 "Invalid waypoint name: " + sectorName
//             );
//             yield break;
//         }

//         Debug.Log("Incident destination: " + sectorName);

//         yield return StartCoroutine(
//             GetRoute(sourceSector, destinationSector)
//         );
//     }

//     IEnumerator GetRoute(int source, int destination)
//     {
//         string url =
//             "http://127.0.0.1:8080/route?source=" +
//             source + "&destination=" + destination;

//         using (UnityWebRequest request =
//             UnityWebRequest.Get(url))
//         {
//             yield return request.SendWebRequest();

//             if (request.result !=
//                 UnityWebRequest.Result.Success)
//             {
//                 Debug.LogError(
//                     "Route request failed: " + request.error
//                 );
//                 yield break;
//             }

//             RouteResponse response =
//                 JsonUtility.FromJson<RouteResponse>(
//                     request.downloadHandler.text
//                 );

//             if (response == null ||
//                 response.route == null ||
//                 response.route.Length == 0)
//             {
//                 Debug.LogError(
//                     "The API returned an empty route."
//                 );
//                 yield break;
//             }

//             if (response.destination != destination ||
//                 response.route[
//                     response.route.Length - 1
//                 ] != destination)
//             {
//                 Debug.LogError(
//                     "The returned route does not end at " +
//                     "the incident waypoint."
//                 );
//                 yield break;
//             }

//             Debug.Log(
//                 "Route received: " +
//                 string.Join(" -> ", response.route)
//             );

//             Debug.Log(
//                 "Total route cost: " + response.total_time
//             );

//             yield return StartCoroutine(
//                 FollowRoute(response.route)
//             );
//         }
//     }

//     IEnumerator FollowRoute(int[] route)
//     {
//         if (route == null || route.Length == 0)
//             yield break;

//         Transform firstWaypoint = FindWaypoint(route[0]);

//         if (firstWaypoint == null)
//             yield break;

//         isMoving = true;

//         // STEP 1: Move from the actual starting position
//         // to the first waypoint without teleporting.
//         Debug.Log(
//             "Moving to starting waypoint: " +
//             firstWaypoint.name
//         );

//         yield return StartCoroutine(
//             MoveToPosition(firstWaypoint.position)
//         );

//         // STEP 2: Follow the route returned by the API.
//         for (int i = 1; i < route.Length; i++)
//         {
//             Transform target = FindWaypoint(route[i]);

//             if (target == null)
//             {
//                 isMoving = false;
//                 yield break;
//             }

//             Debug.Log("Flying to " + target.name);

//             yield return StartCoroutine(
//                 MoveToPosition(target.position)
//             );
//         }

//         Debug.Log(
//             "Reached incident waypoint: Sector_" +
//             route[route.Length - 1]
//         );

//         // STEP 3: Rise vertically to 3 units above
//         // the incident's Y-coordinate.
//         Vector3 incidentPosition =
//             incidentDetector.IncidentPosition;

//         float targetY =
//             incidentPosition.y + rooftopClearance;

//         Vector3 aboveWaypoint = new Vector3(
//             transform.position.x,
//             targetY,
//             transform.position.z
//         );

//         Debug.Log(
//             "Rising vertically to incident altitude: " +
//             targetY
//         );

//         yield return StartCoroutine(
//             RiseWithAcceleration(targetY)
//         );

//         // STEP 4: Move horizontally above the incident,
//         // keeping the drone at the target altitude.
//         Vector3 aboveIncident = new Vector3(
//             incidentPosition.x,
//             targetY,
//             incidentPosition.z
//         );

//         Debug.Log(
//             "Moving above the rooftop incident."
//         );

//         yield return StartCoroutine(
//             MoveToPosition(aboveIncident)
//         );

//         // STEP 5: Descend and settle on the rooftop.
//         Vector3 landingPosition =
//             incidentPosition +
//             Vector3.up * landingOffset;

//         Debug.Log(
//             "Descending vertically onto the rooftop."
//         );

//         yield return StartCoroutine(
//             MoveToPosition(landingPosition)
//         );

//         isMoving = false;

//         Debug.Log(
//             "Drone has landed on the rooftop!"
//         );
//     }

//     IEnumerator RiseWithAcceleration(float targetY)
//     {
//         float currentSpeed = 0f;

//         while (transform.position.y < targetY - 0.05f)
//         {
//             // Gradually accelerate upward.
//             currentSpeed = Mathf.MoveTowards(
//                 currentSpeed,
//                 maxTakeoffSpeed,
//                 takeoffAcceleration * Time.deltaTime
//             );

//             Vector3 nextPosition = transform.position;

//             nextPosition.y = Mathf.MoveTowards(
//                 transform.position.y,
//                 targetY,
//                 currentSpeed * Time.deltaTime
//             );

//             transform.position = nextPosition;

//             yield return null;
//         }

//         // Stop exactly at the target altitude.
//         Vector3 finalPosition = transform.position;
//         finalPosition.y = targetY;
//         transform.position = finalPosition;
//     }

//     IEnumerator MoveToPosition(Vector3 targetPosition)
//     {
//         float currentSpeed = 0f;

//         while (Vector3.Distance(
//             transform.position,
//             targetPosition
//         ) > 0.05f)
//         {
//             float distance = Vector3.Distance(
//                 transform.position,
//                 targetPosition
//             );

//             // Calculate a safe speed for braking.
//             float brakingSpeed = Mathf.Sqrt(
//                 2f * movementAcceleration * distance
//             );

//             float desiredSpeed = Mathf.Min(
//                 speed,
//                 brakingSpeed
//             );

//             // Accelerate or decelerate smoothly.
//             currentSpeed = Mathf.MoveTowards(
//                 currentSpeed,
//                 desiredSpeed,
//                 movementAcceleration * Time.deltaTime
//             );

//             // Rotate toward the horizontal direction of travel.
//             Vector3 direction =
//                 targetPosition - transform.position;

//             Vector3 horizontalDirection = new Vector3(
//                 direction.x,
//                 0f,
//                 direction.z
//             );

//             if (horizontalDirection.sqrMagnitude > 0.001f)
//             {
//                 Quaternion targetRotation =
//                     Quaternion.LookRotation(
//                         horizontalDirection.normalized,
//                         Vector3.up
//                     );

//                 transform.rotation =
//                     Quaternion.RotateTowards(
//                         transform.rotation,
//                         targetRotation,
//                         rotationSpeed * Time.deltaTime
//                     );
//             }

//             // Move toward the target using current speed.
//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 targetPosition,
//                 currentSpeed * Time.deltaTime
//             );

//             yield return null;
//         }

//         transform.position = targetPosition;
//     }

//     Transform FindWaypoint(int sectorID)
//     {
//         string waypointName = "Sector_" + sectorID;

//         Transform waypoint =
//             waypointsParent.Find(waypointName);

//         if (waypoint == null)
//         {
//             Debug.LogError(
//                 "Waypoint not found: " + waypointName
//             );
//         }

//         return waypoint;
//     }
// }




// <<<<<<<<<<<<<<<<<<<<<<<<<<<>><<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>><<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>><<<<<<<<<<<<<<<<<<<<<<<<<<<<



// using System.Collections;
// using UnityEngine;
// using UnityEngine.Networking;

// [System.Serializable]
// public class RouteResponse
// {
//     public int source;
//     public int destination;
//     public int[] route;
//     public int total_time;
// }

// public class DroneRouteController : MonoBehaviour
// {
//     [Header("References")]
//     public Transform waypointsParent;

//     [Header("Battery")]
//     [Range(0f, 100f)]
//     public float battery = 100f;

//     public float chargingRate = 5f;
//     public float batteryPerTravelTimeUnit = 0.5f;
//     public float batteryReserve = 10f;

//     [Header("Movement")]
//     public float speed = 3f;
//     public float movementAcceleration = 2f;
//     public float takeoffAcceleration = 0.8f;
//     public float maxTakeoffSpeed = 2.5f;
//     public float rotationSpeed = 70f;

//     [Header("Rooftop")]
//     public float rooftopClearance = 3f;
//     public float landingOffset = 0.1f;
//     public float timeAtIncident = 2f;

//     public bool IsOnMission { get; private set; }
//     public bool IsAvailable => !IsOnMission;
//     public bool IsCharging { get; private set; }

//     private Transform baseWaypoint;
//     private bool hasBase;
//     private bool isReturning;

//     public void SetBase(Transform basePoint)
//     {
//         baseWaypoint = basePoint;
//         hasBase = baseWaypoint != null;
//     }

//     void Update()
//     {
//         IsCharging = false;

//         if (IsOnMission || !hasBase)
//             return;

//         // Recharge only when the drone is near its base.
//         if (Vector3.Distance(
//             transform.position,
//             baseWaypoint.position) < 1.5f)
//         {
//             IsCharging = battery < 100f;

//             battery = Mathf.Min(
//                 100f,
//                 battery + chargingRate * Time.deltaTime
//             );
//         }
//     }

//     public bool CanCompleteRoundTrip(
//         int outboundCost,
//         int returnCost)
//     {
//         float requiredBattery =
//             (outboundCost + returnCost) *
//             batteryPerTravelTimeUnit;

//         return battery >= requiredBattery + batteryReserve;
//     }

//     public void AssignMission(
//         Vector3 incidentPosition,
//         int destinationSector,
//         int[] outboundRoute,
//         int[] returnRoute,
//         int outboundCost,
//         int returnCost)
//     {
//         if (IsOnMission)
//             return;

//         StartCoroutine(ExecuteMission(
//             incidentPosition,
//             destinationSector,
//             outboundRoute,
//             returnRoute,
//             outboundCost,
//             returnCost
//         ));
//     }

//     IEnumerator ExecuteMission(
//         Vector3 incidentPosition,
//         int destinationSector,
//         int[] outboundRoute,
//         int[] returnRoute,
//         int outboundCost,
//         int returnCost)
//     {
//         IsOnMission = true;
//         isReturning = false;

//         // Charge for the outbound trip using the route cost.
//         battery = Mathf.Max(
//             0f,
//             battery - outboundCost * batteryPerTravelTimeUnit
//         );

//         Debug.Log(
//             name + " dispatched. Battery: " +
//             battery.ToString("F1") + "%"
//         );

//         // Follow the outbound Dijkstra route.
//         yield return StartCoroutine(FollowWaypoints(outboundRoute));

//         // Approach the incident from above.
//         float targetY = incidentPosition.y + rooftopClearance;

//         Vector3 aboveIncident = new Vector3(
//             incidentPosition.x,
//             targetY,
//             incidentPosition.z
//         );

//         yield return StartCoroutine(RiseToAltitude(targetY));

//         yield return StartCoroutine(
//             MoveToPosition(aboveIncident)
//         );

//         // Land vertically on the rooftop.
//         Vector3 landingPosition =
//             incidentPosition + Vector3.up * landingOffset;

//         yield return StartCoroutine(
//             MoveToPosition(landingPosition)
//         );

//         Debug.Log(name + " landed at incident.");

//         // Simulate time spent at the incident.
//         yield return new WaitForSeconds(timeAtIncident);

//         // Deduct the estimated return-trip battery.
//         battery = Mathf.Max(
//             0f,
//             battery - returnCost * batteryPerTravelTimeUnit
//         );

//         isReturning = true;

//         Debug.Log(name + " returning to base.");

//         // Follow the return route from incident to Sector_0.
//         yield return StartCoroutine(FollowWaypoints(returnRoute));

//         // Ensure the drone finishes at the base waypoint.
//         if (hasBase)
//         {
//             yield return StartCoroutine(
//                 MoveToPosition(baseWaypoint.position)
//             );
//         }

//         isReturning = false;
//         IsOnMission = false;

//         Debug.Log(
//             name + " returned to base. Battery: " +
//             battery.ToString("F1") + "%. Charging can begin."
//         );
//     }

//     IEnumerator FollowWaypoints(int[] route)
//     {
//         if (route == null || route.Length == 0)
//         {
//             Debug.LogError(name + ": Empty route.");
//             yield break;
//         }

//         foreach (int sectorID in route)
//         {
//             Transform waypoint = FindWaypoint(sectorID);

//             if (waypoint == null)
//             {
//                 Debug.LogError(
//                     name + ": Missing waypoint Sector_" + sectorID
//                 );

//                 yield break;
//             }

//             yield return StartCoroutine(
//                 MoveToPosition(waypoint.position)
//             );
//         }
//     }

//     IEnumerator RiseToAltitude(float targetY)
//     {
//         float currentSpeed = 0f;

//         while (transform.position.y < targetY - 0.05f)
//         {
//             currentSpeed = Mathf.MoveTowards(
//                 currentSpeed,
//                 maxTakeoffSpeed,
//                 takeoffAcceleration * Time.deltaTime
//             );

//             Vector3 nextPosition = transform.position;

//             nextPosition.y = Mathf.MoveTowards(
//                 transform.position.y,
//                 targetY,
//                 currentSpeed * Time.deltaTime
//             );

//             transform.position = nextPosition;

//             yield return null;
//         }

//         Vector3 finalPosition = transform.position;
//         finalPosition.y = targetY;
//         transform.position = finalPosition;
//     }

//     IEnumerator MoveToPosition(Vector3 targetPosition)
//     {
//         float currentSpeed = 0f;

//         while (Vector3.Distance(
//             transform.position,
//             targetPosition) > 0.05f)
//         {
//             float distance = Vector3.Distance(
//                 transform.position,
//                 targetPosition
//             );

//             float brakingSpeed = Mathf.Sqrt(
//                 2f * movementAcceleration * distance
//             );

//             float desiredSpeed = Mathf.Min(speed, brakingSpeed);

//             currentSpeed = Mathf.MoveTowards(
//                 currentSpeed,
//                 desiredSpeed,
//                 movementAcceleration * Time.deltaTime
//             );

//             Vector3 direction =
//                 targetPosition - transform.position;

//             Vector3 horizontalDirection = new Vector3(
//                 direction.x, 0f, direction.z
//             );

//             if (horizontalDirection.sqrMagnitude > 0.001f)
//             {
//                 Quaternion targetRotation =
//                     Quaternion.LookRotation(
//                         horizontalDirection.normalized,
//                         Vector3.up
//                     );

//                 transform.rotation = Quaternion.RotateTowards(
//                     transform.rotation,
//                     targetRotation,
//                     rotationSpeed * Time.deltaTime
//                 );
//             }

//             transform.position = Vector3.MoveTowards(
//                 transform.position,
//                 targetPosition,
//                 currentSpeed * Time.deltaTime
//             );

//             yield return null;
//         }

//         transform.position = targetPosition;
//     }

//     Transform FindWaypoint(int sectorID)
//     {
//         if (waypointsParent == null)
//             return null;

//         Transform waypoint =
//             waypointsParent.Find("Sector_" + sectorID);

//         if (waypoint == null)
//         {
//             Debug.LogError(
//                 name + ": Waypoint not found: Sector_" + sectorID
//             );
//         }

//         return waypoint;
//     }
// }


// <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class RouteResponse
{
    public int source;
    public int destination;
    public int[] route;
    public int total_time;
}

public class DroneRouteController : MonoBehaviour
{
    [Header("References")]
    public Transform waypointsParent;

    [Header("Battery")]
    [Range(0f, 100f)]
    public float battery = 100f;

    public float chargingRate = 5f;
    public float batteryPerTravelTimeUnit = 0.5f;
    public float batteryReserve = 10f;

    [Header("Movement")]
    public float speed = 3f;
    public float movementAcceleration = 2f;
    public float takeoffAcceleration = 0.8f;
    public float maxTakeoffSpeed = 2.5f;
    public float rotationSpeed = 70f;

    [Header("Rooftop")]
    public float rooftopClearance = 3f;
    public float landingOffset = 0.1f;
    public float timeAtIncident = 2f;

    [Header("Parking and Charging")]
    [Tooltip("Maximum distance from the saved parking position to charge.")]
    public float parkingTolerance = 0.1f;

    [Tooltip("Maximum distance from the saved parking position to count as landed.")]
    public float landingTolerance = 0.15f;

    public bool IsOnMission { get; private set; }
    public bool IsAvailable => !IsOnMission;
    public bool IsCharging { get; private set; }

    // Shared route waypoint used by the fleet.
    private Transform baseWaypoint;
    private bool hasBase;

    // Individual parking location for this drone.
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool homePositionSaved;

    private bool isReturning;

    // Public read-only access to this drone's assigned parking position.
    public Vector3 HomePosition => homePosition;

    public void SetBase(Transform basePoint)
    {
        baseWaypoint = basePoint;
        hasBase = baseWaypoint != null;

        // Save this drone's own position, not Sector_0's position.
        homePosition = transform.position;
        homeRotation = transform.rotation;
        homePositionSaved = true;

        IsCharging = false;

        if (hasBase)
        {
            Debug.Log(
                name + " parking position saved: " + homePosition
            );
        }
        else
        {
            Debug.LogError(name + ": Base waypoint is missing!");
        }
    }

    void Update()
    {
        IsCharging = false;

        // Never charge during a mission or before a parking
        // position has been assigned.
        if (IsOnMission || !homePositionSaved)
            return;

        // Charge ONLY when this drone is at its own parking spot.
        float distanceFromHome = Vector3.Distance(
            transform.position,
            homePosition
        );

        if (distanceFromHome <= parkingTolerance)
        {
            IsCharging = battery < 100f;

            battery = Mathf.Min(
                100f,
                battery + chargingRate * Time.deltaTime
            );
        }
    }

    public bool CanCompleteRoundTrip(
        int outboundCost,
        int returnCost)
    {
        float requiredBattery =
            (outboundCost + returnCost) *
            batteryPerTravelTimeUnit;

        return battery >= requiredBattery + batteryReserve;
    }

    public void AssignMission(
        Vector3 incidentPosition,
        int destinationSector,
        int[] outboundRoute,
        int[] returnRoute,
        int outboundCost,
        int returnCost)
    {
        if (IsOnMission)
            return;

        if (!homePositionSaved || !hasBase)
        {
            Debug.LogError(
                name + ": Assign a valid base using SetBase first."
            );
            return;
        }

        StartCoroutine(ExecuteMission(
            incidentPosition,
            destinationSector,
            outboundRoute,
            returnRoute,
            outboundCost,
            returnCost
        ));
    }

    IEnumerator ExecuteMission(
        Vector3 incidentPosition,
        int destinationSector,
        int[] outboundRoute,
        int[] returnRoute,
        int outboundCost,
        int returnCost)
    {
        IsOnMission = true;
        IsCharging = false;
        isReturning = false;

        // Deduct estimated outbound battery cost.
        battery = Mathf.Max(
            0f,
            battery - outboundCost * batteryPerTravelTimeUnit
        );

        Debug.Log(
            name + " dispatched. Battery: " +
            battery.ToString("F1") + "%"
        );

        // 1. Follow the outbound Dijkstra route.
        yield return StartCoroutine(FollowWaypoints(outboundRoute));

        // 2. Approach the incident from above.
        float targetY = incidentPosition.y + rooftopClearance;

        Vector3 aboveIncident = new Vector3(
            incidentPosition.x,
            targetY,
            incidentPosition.z
        );

        yield return StartCoroutine(RiseToAltitude(targetY));

        yield return StartCoroutine(
            MoveToPosition(aboveIncident)
        );

        // 3. Land on the rooftop.
        Vector3 landingPosition =
            incidentPosition + Vector3.up * landingOffset;

        yield return StartCoroutine(
            MoveToPosition(landingPosition)
        );

        Debug.Log(name + " landed at incident.");

        // 4. Wait at the incident.
        yield return new WaitForSeconds(timeAtIncident);

        // 5. Deduct estimated return-trip battery cost.
        battery = Mathf.Max(
            0f,
            battery - returnCost * batteryPerTravelTimeUnit
        );

        isReturning = true;

        Debug.Log(name + " returning to base.");

        // 6. Follow the return route to Sector_0.
        // Rise first so the drone does not travel horizontally
        // through the rooftop/buildings when leaving the incident.
        Vector3 aboveReturnStart = new Vector3(
            transform.position.x,
            transform.position.y + rooftopClearance,
            transform.position.z
        );

        yield return StartCoroutine(
            MoveToPosition(aboveReturnStart)
        );

        yield return StartCoroutine(
            FollowWaypoints(returnRoute)
        );

        // 7. Fly from Sector_0 to THIS drone's parking location.
        // The five drones can therefore park separately.
        yield return StartCoroutine(
            MoveToPosition(homePosition)
        );

        // 8. Finish at the original orientation.
        transform.rotation = homeRotation;

        isReturning = false;
        IsOnMission = false;

        // Charging begins in Update() only after the drone
        // is within parkingTolerance of its own home position.
        IsCharging = false;

        Debug.Log(
            name + " parked at its own starting location. " +
            "Battery: " + battery.ToString("F1") +
            "%. Charging will begin at this location."
        );
    }

    IEnumerator FollowWaypoints(int[] route)
    {
        if (route == null || route.Length == 0)
        {
            Debug.LogError(name + ": Empty route.");
            yield break;
        }

        foreach (int sectorID in route)
        {
            Transform waypoint = FindWaypoint(sectorID);

            if (waypoint == null)
            {
                Debug.LogError(
                    name + ": Missing waypoint Sector_" + sectorID
                );

                yield break;
            }

            yield return StartCoroutine(
                MoveToPosition(waypoint.position)
            );
        }
    }

    IEnumerator RiseToAltitude(float targetY)
    {
        float currentSpeed = 0f;

        while (transform.position.y < targetY - 0.05f)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                maxTakeoffSpeed,
                takeoffAcceleration * Time.deltaTime
            );

            Vector3 nextPosition = transform.position;

            nextPosition.y = Mathf.MoveTowards(
                transform.position.y,
                targetY,
                currentSpeed * Time.deltaTime
            );

            transform.position = nextPosition;

            yield return null;
        }

        Vector3 finalPosition = transform.position;
        finalPosition.y = targetY;
        transform.position = finalPosition;
    }

    IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        float currentSpeed = 0f;

        while (Vector3.Distance(
            transform.position,
            targetPosition) > 0.05f)
        {
            float distance = Vector3.Distance(
                transform.position,
                targetPosition
            );

            // Slow down as the drone approaches its destination.
            float brakingSpeed = Mathf.Sqrt(
                2f * movementAcceleration * distance
            );

            float desiredSpeed = Mathf.Min(
                speed,
                brakingSpeed
            );

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                desiredSpeed,
                movementAcceleration * Time.deltaTime
            );

            Vector3 direction =
                targetPosition - transform.position;

            Vector3 horizontalDirection = new Vector3(
                direction.x,
                0f,
                direction.z
            );

            // Rotate toward the direction of horizontal travel.
            if (horizontalDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        horizontalDirection.normalized,
                        Vector3.up
                    );

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                currentSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;
    }

    Transform FindWaypoint(int sectorID)
    {
        if (waypointsParent == null)
            return null;

        Transform waypoint =
            waypointsParent.Find("Sector_" + sectorID);

        if (waypoint == null)
        {
            Debug.LogError(
                name + ": Waypoint not found: Sector_" + sectorID
            );
        }

        return waypoint;
    }
}
