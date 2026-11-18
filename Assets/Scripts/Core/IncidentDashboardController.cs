using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the interactive incident dashboard (Display 2).
/// Collects incident requests locally, manages the visual queue,
/// and commits the batch for priority-ordered drone dispatch.
/// </summary>
public class IncidentDashboardController : MonoBehaviour
{
    [System.Serializable]
    public class IncidentRequest
    {
        public int requestId;
        public int destinationSector;
        public string incidentType;
        public int priority; // 1 = Highest, 6 = Lowest
        public bool isCommitted;
    }

    [Header("Requests Panel Inputs")]
    public TMP_InputField sectorInput;
    public TMP_Dropdown incidentTypeDropdown;
    public Button addRequestButton;
    public TextMeshProUGUI statusFeedbackText; // Optional error/status message label

    [Header("Queue Panel UI")]
    public Transform queueContainer;
    public GameObject queueItemTemplate;
    public Button commitButton;
    public TextMeshProUGUI commitButtonText;

    [Header("Fleet Dispatch Reference")]
    public DroneFleetManager fleetManager;

    [Header("Auto-Dispatch Settings")]
    [Tooltip("How often (in seconds) to check for returned/charged drones when committed requests remain queued.")]
    public float autoDispatchPollInterval = 0.5f;
    public float sequentialDispatchDelay = 1.0f;

    [Header("Queue State")]
    [SerializeField]
    private List<IncidentRequest> pendingRequests = new List<IncidentRequest>();
    private int nextRequestId = 1;
    private bool isDispatching = false;
    private Coroutine autoDispatchCoroutine = null;
    private bool isAutoDispatchRequestInFlight = false;

    // Cache of instantiated queue cards mapped by request ID
    private readonly List<GameObject> activeQueueCards = new List<GameObject>();

    void Awake()
    {
        // Ensure template is inactive at start so it doesn't show as a dummy row
        if (queueItemTemplate != null && queueItemTemplate.activeSelf)
        {
            queueItemTemplate.SetActive(false);
        }
    }

    void Start()
    {
        if (addRequestButton != null)
        {
            addRequestButton.onClick.AddListener(OnAddRequestClicked);
        }

        if (commitButton != null)
        {
            commitButton.onClick.AddListener(OnCommitBatchClicked);
        }

        // Initialize status message if present
        UpdateStatus("");
    }

    /// <summary>
    /// Reads user input, validates sector (0-105), maps priority,
    /// and appends the request to the pending queue.
    /// </summary>
    public void OnAddRequestClicked()
    {
        if (isDispatching)
        {
            UpdateStatus("Please wait, dispatch in progress...", Color.yellow);
            return;
        }

        if (sectorInput == null || incidentTypeDropdown == null)
        {
            Debug.LogError("SectorInput or IncidentTypeDropdown not assigned in IncidentDashboardController.");
            return;
        }

        string rawSector = sectorInput.text.Trim();

        // 1. Validate sector integer input
        if (string.IsNullOrEmpty(rawSector) || !int.TryParse(rawSector, out int sector))
        {
            UpdateStatus("Error: Enter a valid integer sector (0–105).", Color.red);
            return;
        }

        if (sector < 0 || sector > 105)
        {
            UpdateStatus($"Error: Sector {sector} out of range (must be 0–105).", Color.red);
            return;
        }

        // 2. Read selected incident type
        string incidentType = incidentTypeDropdown.options[incidentTypeDropdown.value].text;

        // 3. Resolve priority: Lower integer = Higher priority (1 is highest)
        int priority = GetPriorityForIncident(incidentType);

        // 4. Create request data object
        IncidentRequest newReq = new IncidentRequest
        {
            requestId = nextRequestId++,
            destinationSector = sector,
            incidentType = incidentType,
            priority = priority,
            isCommitted = false
        };

        pendingRequests.Add(newReq);

        // 5. Instantiate visual queue row card
        CreateQueueCard(newReq);

        // 6. Reset sector input for the next entry
        sectorInput.text = "";
        sectorInput.ActivateInputField();

        UpdateStatus($"Added Request #{newReq.requestId} (Sector {newReq.destinationSector}, Pri {newReq.priority})", Color.green);
        Debug.Log($"[Dashboard] Added request #{newReq.requestId} for Sector {newReq.destinationSector} ({newReq.incidentType}) with Priority {newReq.priority}");
    }

    /// <summary>
    /// Instantiates a visual queue card from the template.
    /// </summary>
    private void CreateQueueCard(IncidentRequest req)
    {
        if (queueItemTemplate == null || queueContainer == null)
            return;

        GameObject card = Instantiate(queueItemTemplate, queueContainer);
        card.name = "QueueCard_" + req.requestId;
        card.SetActive(true);

        // Update card text
        TextMeshProUGUI details = card.GetComponentInChildren<TextMeshProUGUI>();
        if (details != null)
        {
            details.text = $"#{req.requestId}          Sector {req.destinationSector}          {req.incidentType}          P{req.priority}";
        }

        activeQueueCards.Add(card);
    }

    /// <summary>
    /// Maps incident type names to priority integer (1 = Highest, 6 = Lowest).
    /// </summary>
    public static int GetPriorityForIncident(string incidentType)
    {
        string norm = incidentType.ToLowerInvariant();

        if (norm.Contains("critical"))
            return 1; // Medical Emergency – Critical
        if (norm.Contains("fire"))
            return 1; // Fire Incident
        if (norm.Contains("major"))
            return 2; // Road Accident – Major
        if (norm.Contains("non-critical"))
            return 3; // Medical Emergency – Non-critical
        if (norm.Contains("minor"))
            return 4; // Road Accident – Minor
        if (norm.Contains("food") || norm.Contains("supply"))
            return 5; // Food Supply
        
        return 6; // Other / Lowest
    }

    /// <summary>
    /// Triggers the 5-second buffer and batch dispatch sequence.
    /// </summary>
    public void OnCommitBatchClicked()
    {
        if (isDispatching)
            return;

        if (pendingRequests.Count == 0)
        {
            UpdateStatus("Queue is empty. Add requests before committing.", Color.yellow);
            return;
        }

        bool hasUncommitted = pendingRequests.Exists(r => !r.isCommitted);
        if (!hasUncommitted)
        {
            StartAutoDispatchIfNeeded();
            UpdateStatus("Queued requests are already committed — auto-dispatching as drones return.", Color.yellow);
            return;
        }

        StartCoroutine(CommitAndDispatchRoutine());
    }

    private IEnumerator CommitAndDispatchRoutine()
    {
        isDispatching = true;
        if (commitButton != null) commitButton.interactable = false;
        if (addRequestButton != null) addRequestButton.interactable = false;

        // Pause background auto-dispatcher cleanly before modifying/sorting the committed batch
        while (isAutoDispatchRequestInFlight)
        {
            yield return null;
        }

        if (autoDispatchCoroutine != null)
        {
            StopCoroutine(autoDispatchCoroutine);
            autoDispatchCoroutine = null;
        }

        // Mark all currently queued requests as committed
        foreach (IncidentRequest req in pendingRequests)
        {
            req.isCommitted = true;
        }

        // Sort pending requests by Priority ASCENDING (1 is highest, then 2, etc.)
        // Secondary sort: earlier requestId first
        SortPendingRequestsByPriority();
        SyncQueueCardOrder();

        // 5-second countdown buffer to allow switching Game View to Display 1
        for (int secondsLeft = 5; secondsLeft > 0; secondsLeft--)
        {
            if (commitButtonText != null)
            {
                commitButtonText.text = $"DISPATCHING IN {secondsLeft}s (SWITCH TO DISPLAY 1)...";
            }
            UpdateStatus($"Priority sorted. Drones launching in {secondsLeft}s — Switch to Display 1!", Color.yellow);
            yield return new WaitForSeconds(1.0f);
        }

        if (commitButtonText != null)
        {
            commitButtonText.text = "DISPATCHING DRONES...";
        }
        UpdateStatus($"Dispatching {pendingRequests.Count} request(s) by priority...", Color.green);

        // Dispatch committed requests in priority order to the fleet manager
        if (fleetManager != null)
        {
            List<IncidentRequest> dispatchList = new List<IncidentRequest>(pendingRequests);

            foreach (IncidentRequest req in dispatchList)
            {
                DroneFleetManager.DispatchOutcome outcome = DroneFleetManager.DispatchOutcome.Failed;
                yield return StartCoroutine(fleetManager.DispatchIncidentSector(
                    req.destinationSector,
                    req.requestId,
                    req.priority,
                    result => outcome = result,
                    verboseLogging: true
                ));

                if (outcome == DroneFleetManager.DispatchOutcome.Dispatched)
                {
                    RemoveRequestFromQueue(req);

                    // Short delay between dispatches so drones take off sequentially
                    yield return new WaitForSeconds(sequentialDispatchDelay);
                }
                else if (outcome == DroneFleetManager.DispatchOutcome.Failed)
                {
                    req.isCommitted = false;
                }
            }
        }
        else
        {
            Debug.LogWarning("[Dashboard] FleetManager reference not assigned. Batch prioritized locally.");
        }

        if (commitButtonText != null)
        {
            commitButtonText.text = "COMMIT & DISPATCH QUEUE";
        }

        if (commitButton != null) commitButton.interactable = true;
        if (addRequestButton != null) addRequestButton.interactable = true;
        isDispatching = false;

        int remainingCommitted = GetCommittedRequestCount();
        if (remainingCommitted > 0)
        {
            UpdateStatus($"{remainingCommitted} committed request(s) queued — will auto-dispatch as drones return.", Color.yellow);
            StartAutoDispatchIfNeeded();
        }
        else
        {
            UpdateStatus("Dispatch sequence complete.", Color.white);
        }
    }

    private void StartAutoDispatchIfNeeded()
    {
        if (autoDispatchCoroutine == null && GetCommittedRequestCount() > 0)
        {
            autoDispatchCoroutine = StartCoroutine(AutoDispatchCommittedQueueRoutine());
        }
    }

    /// <summary>
    /// Continuously monitors committed requests that remained queued due to busy or charging drones,
    /// and automatically dispatches them in priority order once a drone returns and has sufficient battery.
    /// </summary>
    private IEnumerator AutoDispatchCommittedQueueRoutine()
    {
        while (GetCommittedRequestCount() > 0)
        {
            if (isDispatching || fleetManager == null || !fleetManager.HasAvailableParkedDrone())
            {
                yield return new WaitForSeconds(autoDispatchPollInterval);
                continue;
            }

            // Gather committed requests in strict priority order
            List<IncidentRequest> committedList = pendingRequests.FindAll(r => r.isCommitted);
            committedList.Sort((a, b) =>
            {
                int pComp = a.priority.CompareTo(b.priority);
                if (pComp != 0) return pComp;
                return a.requestId.CompareTo(b.requestId);
            });

            bool dispatchedAnyThisPass = false;

            foreach (IncidentRequest req in committedList)
            {
                if (isDispatching || !fleetManager.HasAvailableParkedDrone())
                    break;

                DroneFleetManager.DispatchOutcome outcome = DroneFleetManager.DispatchOutcome.Failed;
                isAutoDispatchRequestInFlight = true;

                yield return StartCoroutine(fleetManager.DispatchIncidentSector(
                    req.destinationSector,
                    req.requestId,
                    req.priority,
                    result => outcome = result,
                    verboseLogging: false
                ));

                isAutoDispatchRequestInFlight = false;

                if (outcome == DroneFleetManager.DispatchOutcome.Dispatched)
                {
                    dispatchedAnyThisPass = true;
                    RemoveRequestFromQueue(req);

                    int remaining = GetCommittedRequestCount();
                    Debug.Log($"[Dashboard] Auto-dispatched queued Request #{req.requestId} (Sector {req.destinationSector}, Priority {req.priority}). Remaining committed: {remaining}");

                    if (remaining > 0)
                    {
                        UpdateStatus($"Auto-dispatched Request #{req.requestId}. {remaining} committed request(s) still queued.", Color.green);
                        yield return new WaitForSeconds(sequentialDispatchDelay);
                    }
                    else
                    {
                        UpdateStatus("All committed requests dispatched.", Color.white);
                    }
                }
                else if (outcome == DroneFleetManager.DispatchOutcome.WaitingForDrone)
                {
                    // Preserve strict priority order while the parked drone finishes charging
                    break;
                }
                else
                {
                    // Uncommit failed request so it does not stall remaining queued requests
                    req.isCommitted = false;
                }
            }

            if (!dispatchedAnyThisPass)
            {
                yield return new WaitForSeconds(autoDispatchPollInterval);
            }
        }

        autoDispatchCoroutine = null;
    }

    private void SortPendingRequestsByPriority()
    {
        pendingRequests.Sort((a, b) =>
        {
            int pComp = a.priority.CompareTo(b.priority);
            if (pComp != 0) return pComp;
            return a.requestId.CompareTo(b.requestId);
        });
    }

    private void SyncQueueCardOrder()
    {
        for (int i = 0; i < pendingRequests.Count; i++)
        {
            int reqId = pendingRequests[i].requestId;
            GameObject matchingCard = activeQueueCards.Find(c => c != null && c.name == "QueueCard_" + reqId);
            if (matchingCard != null)
            {
                matchingCard.transform.SetSiblingIndex(i);
            }
        }
    }

    private void RemoveRequestFromQueue(IncidentRequest req)
    {
        GameObject cardToRemove = activeQueueCards.Find(c => c != null && c.name == "QueueCard_" + req.requestId);
        if (cardToRemove != null)
        {
            activeQueueCards.Remove(cardToRemove);
            Destroy(cardToRemove);
        }
        pendingRequests.Remove(req);
    }

    private int GetCommittedRequestCount()
    {
        int count = 0;
        for (int i = 0; i < pendingRequests.Count; i++)
        {
            if (pendingRequests[i].isCommitted)
                count++;
        }
        return count;
    }

    private void UpdateStatus(string message, Color? color = null)
    {
        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = message;
            if (color.HasValue) statusFeedbackText.color = color.Value;
        }
    }
}