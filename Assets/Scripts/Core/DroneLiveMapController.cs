using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Renders a live 2D city sector map and real-time 5-drone fleet tracker
/// on Display 3 (targetDisplay = 2).
/// </summary>
public class DroneLiveMapController : MonoBehaviour
{
    [Header("References")]
    public DroneFleetManager fleetManager;

    [Header("Display Settings")]
    [Tooltip("0-indexed display number (2 = Display 3).")]
    public int targetDisplayIndex = 2;

    [Header("Map Visuals")]
    public string edgesCsvFileName = "city_edges.csv";
    public float mapPadding = 44f;

    // Distinct high-contrast colors for D1..D5
    private static readonly Color[] DroneColors =
    {
        new Color(0.86f, 0.15f, 0.15f, 1f), // D1 - Crimson Red
        new Color(0.15f, 0.39f, 0.92f, 1f), // D2 - Royal Blue
        new Color(0.02f, 0.59f, 0.41f, 1f), // D3 - Emerald Green
        new Color(0.58f, 0.20f, 0.92f, 1f), // D4 - Vivid Purple
        new Color(0.92f, 0.35f, 0.05f, 1f), // D5 - Deep Orange
    };

    private static readonly Color BgColor = new Color(0.97f, 0.98f, 0.99f, 1f);
    private static readonly Color PanelBgColor = Color.white;
    private static readonly Color BorderColor = new Color(0.80f, 0.84f, 0.88f, 1f);
    private static readonly Color TextPrimary = new Color(0.06f, 0.09f, 0.16f, 1f);
    private static readonly Color TextSecondary = new Color(0.39f, 0.45f, 0.55f, 1f);
    private static readonly Color EdgeColor = new Color(0.76f, 0.81f, 0.87f, 1f);
    private static readonly Color BaseEdgeColor = new Color(0.01f, 0.52f, 0.78f, 0.85f);
    private static readonly Color NodeColor = new Color(0.20f, 0.56f, 0.82f, 0.95f);
    private static readonly Color BaseNodeColor = new Color(0.98f, 0.80f, 0.08f, 1f);

    private class SectorNodeData
    {
        public int id;
        public Vector3 worldPos;
        public RectTransform nodeRect;
    }

    private class EdgeVisual
    {
        public int fromId;
        public int toId;
        public RectTransform lineRect;
    }

    private class DroneTrackerUI
    {
        public DroneRouteController drone;
        public int index;
        public Color color;

        // Map markers
        public RectTransform markerRoot;
        public RectTransform pulseRing;
        public Image pulseImage;
        public RectTransform targetRing;
        public RectTransform routeLineRoot;
        public readonly List<RectTransform> activeRouteSegments = new List<RectTransform>();
        public int[] cachedRoute;

        // Telemetry card UI
        public TextMeshProUGUI statusText;
        public Image statusBadgeBg;
        public TextMeshProUGUI positionText;
        public TextMeshProUGUI sectorText;
        public TextMeshProUGUI batteryText;
        public Image batteryBarFill;
    }

    private Sprite circleSprite;
    private Sprite ringSprite;
    private Sprite solidSprite;
    private TMP_FontAsset fontAsset;

    private RectTransform mapViewport;
    private RectTransform edgesLayer;
    private RectTransform activeRoutesLayer;
    private RectTransform nodesLayer;
    private RectTransform dronesLayer;
    private TextMeshProUGUI headerSummaryText;

    private readonly Dictionary<int, SectorNodeData> sectorNodes = new Dictionary<int, SectorNodeData>();
    private readonly List<EdgeVisual> edgeVisuals = new List<EdgeVisual>();
    private readonly List<DroneTrackerUI> droneTrackers = new List<DroneTrackerUI>();

    private float minWorldX, maxWorldX, minWorldZ, maxWorldZ;
    private Vector2 lastViewportSize = Vector2.zero;
    private bool isInitialized = false;

    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = targetDisplayIndex;
        }

        // Activate Display 3 in multi-monitor standalone builds
        if (Display.displays.Length > targetDisplayIndex)
        {
            Display.displays[targetDisplayIndex].Activate();
        }

        circleSprite = CreateCircleSprite(64, filled: true, borderThickness: 0f);
        ringSprite = CreateCircleSprite(64, filled: false, borderThickness: 0.22f);
        solidSprite = CreateSolidSprite();
        fontAsset = TMP_Settings.defaultFontAsset;
    }

    void Start()
    {
        if (fleetManager == null)
        {
            fleetManager = FindFirstObjectByType<DroneFleetManager>();
        }

        if (fleetManager == null)
        {
            Debug.LogError("[LiveMap] DroneFleetManager not found! Cannot initialize Display 3 live map.");
            return;
        }

        BuildDashboardLayout();
        CollectSectorNodes();
        LoadEdgesFromCsv();
        BuildStaticMapVisuals();
        BuildDroneTrackers();

        Canvas.ForceUpdateCanvases();
        LayoutStaticMapElements();
        isInitialized = true;
    }

    void LateUpdate()
    {
        if (!isInitialized || fleetManager == null)
            return;

        // Re-layout static map if Display 3 window/aspect changes
        Vector2 currentSize = mapViewport.rect.size;
        if (currentSize.x > 10f && currentSize.y > 10f &&
            (Mathf.Abs(currentSize.x - lastViewportSize.x) > 1f ||
             Mathf.Abs(currentSize.y - lastViewportSize.y) > 1f))
        {
            LayoutStaticMapElements();
        }

        UpdateLiveDronesAndTelemetry();
    }

    private void BuildDashboardLayout()
    {
        RectTransform root = GetComponent<RectTransform>();

        // 1. Full-screen light background
        GameObject bgObj = CreateUIObject("Background", root);
        StretchFill(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = solidSprite;
        bgImg.color = BgColor;

        // 2. Top Header Bar
        GameObject headerObj = CreateUIObject("HeaderBar", root);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 76f);
        Image headerImg = headerObj.AddComponent<Image>();
        headerImg.sprite = solidSprite;
        headerImg.color = PanelBgColor;

        // Subtle header bottom divider
        GameObject headerBorder = CreateUIObject("HeaderDivider", headerRect);
        RectTransform hbRect = headerBorder.GetComponent<RectTransform>();
        hbRect.anchorMin = new Vector2(0f, 0f);
        hbRect.anchorMax = new Vector2(1f, 0f);
        hbRect.pivot = new Vector2(0.5f, 0f);
        hbRect.sizeDelta = new Vector2(0f, 2f);
        Image hbImg = headerBorder.AddComponent<Image>();
        hbImg.sprite = solidSprite;
        hbImg.color = BorderColor;

        // Header Title
        TextMeshProUGUI titleText = CreateTMPText("Title", headerRect, "LIVE DRONE FLEET TRACKER — DISPLAY 3", 26f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.MidlineLeft);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0.55f, 1f);
        titleRect.offsetMin = new Vector2(28f, 0f);
        titleRect.offsetMax = Vector2.zero;

        // Header Live Summary
        headerSummaryText = CreateTMPText("Summary", headerRect, "Initializing fleet...", 18f, FontStyles.Bold, TextSecondary, TextAlignmentOptions.MidlineRight);
        RectTransform sumRect = headerSummaryText.rectTransform;
        sumRect.anchorMin = new Vector2(0.45f, 0f);
        sumRect.anchorMax = new Vector2(1f, 1f);
        sumRect.offsetMin = Vector2.zero;
        sumRect.offsetMax = new Vector2(-28f, 0f);

        // 3. Left Map Panel Container
        GameObject mapPanel = CreateUIObject("MapPanel", root);
        RectTransform mapPanelRect = mapPanel.GetComponent<RectTransform>();
        mapPanelRect.anchorMin = new Vector2(0f, 0f);
        mapPanelRect.anchorMax = new Vector2(0.72f, 1f);
        mapPanelRect.offsetMin = new Vector2(20f, 20f);
        mapPanelRect.offsetMax = new Vector2(-10f, -96f);
        Image mapPanelBg = mapPanel.AddComponent<Image>();
        mapPanelBg.sprite = solidSprite;
        mapPanelBg.color = PanelBgColor;
        AddBorder(mapPanel, BorderColor, 2f);

        // Map Panel Subtitle / Legend Bar at bottom
        GameObject legendBar = CreateUIObject("LegendBar", mapPanelRect);
        RectTransform legendRect = legendBar.GetComponent<RectTransform>();
        legendRect.anchorMin = new Vector2(0f, 0f);
        legendRect.anchorMax = new Vector2(1f, 0f);
        legendRect.pivot = new Vector2(0.5f, 0f);
        legendRect.sizeDelta = new Vector2(0f, 40f);
        Image legendBg = legendBar.AddComponent<Image>();
        legendBg.sprite = solidSprite;
        legendBg.color = new Color(0.95f, 0.97f, 0.99f, 1f);

        TextMeshProUGUI legendText = CreateTMPText(
            "LegendText",
            legendRect,
            "● Gold = Base Sector   ● Blue = City Sector (0–105)   ─── Gray = Flight Corridor   ◎ Colored Ring = Active Mission Target   ● D1–D5 = Live Drone Position",
            14f,
            FontStyles.Normal,
            TextSecondary,
            TextAlignmentOptions.Center
        );
        StretchFill(legendText.rectTransform);

        // Map Viewport (actual drawing area above legend bar)
        GameObject viewportObj = CreateUIObject("MapViewport", mapPanelRect);
        mapViewport = viewportObj.GetComponent<RectTransform>();
        mapViewport.anchorMin = new Vector2(0f, 0f);
        mapViewport.anchorMax = new Vector2(1f, 1f);
        mapViewport.offsetMin = new Vector2(8f, 44f);
        mapViewport.offsetMax = new Vector2(-8f, -8f);
        viewportObj.AddComponent<RectMask2D>();

        // Layered drawing containers (back to front)
        edgesLayer = CreateLayer("EdgesLayer", mapViewport);
        activeRoutesLayer = CreateLayer("ActiveRoutesLayer", mapViewport);
        nodesLayer = CreateLayer("NodesLayer", mapViewport);
        dronesLayer = CreateLayer("DronesLayer", mapViewport);

        // 4. Right Telemetry Panel Container
        GameObject sidePanel = CreateUIObject("TelemetryPanel", root);
        RectTransform sideRect = sidePanel.GetComponent<RectTransform>();
        sideRect.anchorMin = new Vector2(0.72f, 0f);
        sideRect.anchorMax = new Vector2(1f, 1f);
        sideRect.offsetMin = new Vector2(10f, 20f);
        sideRect.offsetMax = new Vector2(-20f, -96f);
        Image sideBg = sidePanel.AddComponent<Image>();
        sideBg.sprite = solidSprite;
        sideBg.color = PanelBgColor;
        AddBorder(sidePanel, BorderColor, 2f);

        // Telemetry Header
        TextMeshProUGUI sideTitle = CreateTMPText("SideTitle", sideRect, "DRONE FLEET TELEMETRY", 19f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.MidlineLeft);
        RectTransform stRect = sideTitle.rectTransform;
        stRect.anchorMin = new Vector2(0f, 1f);
        stRect.anchorMax = new Vector2(1f, 1f);
        stRect.pivot = new Vector2(0.5f, 1f);
        stRect.anchoredPosition = new Vector2(0f, -12f);
        stRect.sizeDelta = new Vector2(-32f, 36f);
    }

    private void CollectSectorNodes()
    {
        sectorNodes.Clear();
        minWorldX = float.MaxValue;
        maxWorldX = float.MinValue;
        minWorldZ = float.MaxValue;
        maxWorldZ = float.MinValue;

        if (fleetManager.waypointsParent != null)
        {
            foreach (Transform child in fleetManager.waypointsParent)
            {
                if (!child.name.StartsWith("Sector_"))
                    continue;

                if (int.TryParse(child.name.Substring("Sector_".Length), out int id))
                {
                    Vector3 pos = child.position;
                    sectorNodes[id] = new SectorNodeData { id = id, worldPos = pos };
                    ExpandBounds(pos);
                }
            }
        }

        // Include drone starting positions in bounds so base pads always stay comfortably inside the map
        if (fleetManager.drones != null)
        {
            foreach (DroneRouteController drone in fleetManager.drones)
            {
                if (drone == null) continue;
                ExpandBounds(drone.transform.position);
            }
        }

        // Guard against degenerate bounds
        if (minWorldX > maxWorldX)
        {
            minWorldX = -100f; maxWorldX = 100f;
            minWorldZ = -100f; maxWorldZ = 100f;
        }
    }

    private void ExpandBounds(Vector3 pos)
    {
        if (pos.x < minWorldX) minWorldX = pos.x;
        if (pos.x > maxWorldX) maxWorldX = pos.x;
        if (pos.z < minWorldZ) minWorldZ = pos.z;
        if (pos.z > maxWorldZ) maxWorldZ = pos.z;
    }

    private void LoadEdgesFromCsv()
    {
        edgeVisuals.Clear();
        string csvPath = Path.Combine(Directory.GetCurrentDirectory(), edgesCsvFileName);
        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"[LiveMap] Could not find {csvPath}; drawing sector nodes without corridor edges.");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 2) continue;

            if (int.TryParse(parts[0].Trim(), out int u) &&
                int.TryParse(parts[1].Trim(), out int v))
            {
                if (sectorNodes.ContainsKey(u) && sectorNodes.ContainsKey(v))
                {
                    edgeVisuals.Add(new EdgeVisual { fromId = u, toId = v });
                }
            }
        }
    }

    private void BuildStaticMapVisuals()
    {
        // Create edge UI images
        foreach (EdgeVisual edge in edgeVisuals)
        {
            bool isBaseEdge = edge.fromId == fleetManager.baseSector || edge.toId == fleetManager.baseSector;
            GameObject lineObj = CreateUIObject($"Edge_{edge.fromId}_{edge.toId}", edgesLayer);
            RectTransform rt = lineObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = lineObj.AddComponent<Image>();
            img.sprite = solidSprite;
            img.color = isBaseEdge ? BaseEdgeColor : EdgeColor;
            img.raycastTarget = false;

            edge.lineRect = rt;
        }

        // Create sector node circles + ID labels
        foreach (var kvp in sectorNodes)
        {
            SectorNodeData node = kvp.Value;
            bool isBase = node.id == fleetManager.baseSector;

            GameObject nodeObj = CreateUIObject($"Node_{node.id}", nodesLayer);
            RectTransform rt = nodeObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float diameter = isBase ? 26f : 18f;
            rt.sizeDelta = new Vector2(diameter, diameter);

            Image img = nodeObj.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color = isBase ? BaseNodeColor : NodeColor;
            img.raycastTarget = false;

            TextMeshProUGUI label = CreateTMPText(
                "Label",
                rt,
                node.id.ToString(),
                isBase ? 11f : (node.id >= 100 ? 7.5f : 8.5f),
                FontStyles.Bold,
                isBase ? TextPrimary : Color.white,
                TextAlignmentOptions.Center
            );
            label.raycastTarget = false;
            StretchFill(label.rectTransform);

            node.nodeRect = rt;
        }
    }

    private void BuildDroneTrackers()
    {
        droneTrackers.Clear();
        if (fleetManager.drones == null)
            return;

        Transform sidePanel = transform.Find("TelemetryPanel");
        int count = fleetManager.drones.Length;

        for (int i = 0; i < count; i++)
        {
            DroneRouteController drone = fleetManager.drones[i];
            if (drone == null) continue;

            Color droneColor = DroneColors[i % DroneColors.Length];
            string shortTag = "D" + (i + 1);

            DroneTrackerUI tracker = new DroneTrackerUI
            {
                drone = drone,
                index = i,
                color = droneColor
            };

            // 1. Active route container for this drone
            tracker.routeLineRoot = CreateLayer($"Route_{shortTag}", activeRoutesLayer);

            // 2. Destination target ring (shown only when on mission)
            GameObject targetObj = CreateUIObject($"TargetRing_{shortTag}", dronesLayer);
            RectTransform targetRt = targetObj.GetComponent<RectTransform>();
            targetRt.anchorMin = new Vector2(0.5f, 0.5f);
            targetRt.anchorMax = new Vector2(0.5f, 0.5f);
            targetRt.sizeDelta = new Vector2(36f, 36f);
            Image targetImg = targetObj.AddComponent<Image>();
            targetImg.sprite = ringSprite;
            targetImg.color = new Color(droneColor.r, droneColor.g, droneColor.b, 0.85f);
            targetImg.raycastTarget = false;
            targetObj.SetActive(false);
            tracker.targetRing = targetRt;

            // 3. Drone live marker root
            GameObject markerObj = CreateUIObject($"DroneMarker_{shortTag}", dronesLayer);
            RectTransform markerRt = markerObj.GetComponent<RectTransform>();
            markerRt.anchorMin = new Vector2(0.5f, 0.5f);
            markerRt.anchorMax = new Vector2(0.5f, 0.5f);
            markerRt.sizeDelta = new Vector2(30f, 30f);

            // Pulsing outer ring
            GameObject pulseObj = CreateUIObject("Pulse", markerRt);
            RectTransform pulseRt = pulseObj.GetComponent<RectTransform>();
            pulseRt.anchorMin = new Vector2(0.5f, 0.5f);
            pulseRt.anchorMax = new Vector2(0.5f, 0.5f);
            pulseRt.sizeDelta = new Vector2(44f, 44f);
            Image pulseImg = pulseObj.AddComponent<Image>();
            pulseImg.sprite = ringSprite;
            pulseImg.color = new Color(droneColor.r, droneColor.g, droneColor.b, 0.45f);
            pulseImg.raycastTarget = false;
            tracker.pulseRing = pulseRt;
            tracker.pulseImage = pulseImg;

            // White outer halo for crisp contrast on white & blue nodes
            GameObject haloObj = CreateUIObject("Halo", markerRt);
            RectTransform haloRt = haloObj.GetComponent<RectTransform>();
            haloRt.anchorMin = new Vector2(0.5f, 0.5f);
            haloRt.anchorMax = new Vector2(0.5f, 0.5f);
            haloRt.sizeDelta = new Vector2(34f, 34f);
            Image haloImg = haloObj.AddComponent<Image>();
            haloImg.sprite = circleSprite;
            haloImg.color = Color.white;
            haloImg.raycastTarget = false;

            // Colored drone circle body
            GameObject bodyObj = CreateUIObject("Body", markerRt);
            RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.sizeDelta = new Vector2(28f, 28f);
            Image bodyImg = bodyObj.AddComponent<Image>();
            bodyImg.sprite = circleSprite;
            bodyImg.color = droneColor;
            bodyImg.raycastTarget = false;

            // D1..D5 text label inside marker
            TextMeshProUGUI tagText = CreateTMPText("Tag", bodyRt, shortTag, 11.5f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            tagText.raycastTarget = false;
            StretchFill(tagText.rectTransform);

            tracker.markerRoot = markerRt;

            // 4. Right-panel Telemetry Card
            if (sidePanel != null)
            {
                BuildTelemetryCard(sidePanel, tracker, i, count, shortTag);
            }

            droneTrackers.Add(tracker);
        }
    }

    private void BuildTelemetryCard(Transform sidePanel, DroneTrackerUI tracker, int index, int totalCount, string shortTag)
    {
        float topPadding = 58f;
        float bottomPadding = 16f;
        float gap = 12f;

        GameObject cardObj = CreateUIObject($"Card_{shortTag}", sidePanel);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();

        float slotHeightNorm = 1f / Mathf.Max(1, totalCount);
        float yMax = 1f - index * slotHeightNorm;
        float yMin = 1f - (index + 1) * slotHeightNorm;

        // Place inside a dedicated cards container so top header never overlaps
        Transform cardsContainer = sidePanel.Find("CardsContainer");
        if (cardsContainer == null)
        {
            GameObject ccObj = CreateUIObject("CardsContainer", sidePanel);
            RectTransform ccRt = ccObj.GetComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0f, 0f);
            ccRt.anchorMax = new Vector2(1f, 1f);
            ccRt.offsetMin = new Vector2(16f, bottomPadding);
            ccRt.offsetMax = new Vector2(-16f, -topPadding);
            cardsContainer = ccRt;
        }

        cardRt.SetParent(cardsContainer, false);
        cardRt.anchorMin = new Vector2(0f, yMin);
        cardRt.anchorMax = new Vector2(1f, yMax);
        cardRt.offsetMin = new Vector2(0f, index == totalCount - 1 ? 0f : gap * 0.5f);
        cardRt.offsetMax = new Vector2(0f, index == 0 ? 0f : -gap * 0.5f);

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.sprite = solidSprite;
        cardBg.color = new Color(0.98f, 0.99f, 1f, 1f);
        AddBorder(cardObj, BorderColor, 1.5f);

        // Left accent stripe in drone's color
        GameObject stripeObj = CreateUIObject("Accent", cardRt);
        RectTransform stripeRt = stripeObj.GetComponent<RectTransform>();
        stripeRt.anchorMin = new Vector2(0f, 0f);
        stripeRt.anchorMax = new Vector2(0f, 1f);
        stripeRt.pivot = new Vector2(0f, 0.5f);
        stripeRt.sizeDelta = new Vector2(7f, 0f);
        Image stripeImg = stripeObj.AddComponent<Image>();
        stripeImg.sprite = solidSprite;
        stripeImg.color = tracker.color;

        // Row 1: Color Badge + Drone Name + Status Pill
        GameObject badgeObj = CreateUIObject("Badge", cardRt);
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 1f);
        badgeRt.anchorMax = new Vector2(0f, 1f);
        badgeRt.pivot = new Vector2(0f, 1f);
        badgeRt.anchoredPosition = new Vector2(18f, -12f);
        badgeRt.sizeDelta = new Vector2(34f, 26f);
        Image badgeImg = badgeObj.AddComponent<Image>();
        badgeImg.sprite = solidSprite;
        badgeImg.color = tracker.color;
        TextMeshProUGUI badgeText = CreateTMPText("Tag", badgeRt, shortTag, 13f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        StretchFill(badgeText.rectTransform);

        TextMeshProUGUI nameText = CreateTMPText("Name", cardRt, tracker.drone.name, 16f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.MidlineLeft);
        RectTransform nameRt = nameText.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(0.55f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(60f, -12f);
        nameRt.sizeDelta = new Vector2(0f, 26f);

        // Status Pill
        GameObject statusBgObj = CreateUIObject("StatusBadge", cardRt);
        RectTransform statusBgRt = statusBgObj.GetComponent<RectTransform>();
        statusBgRt.anchorMin = new Vector2(0.52f, 1f);
        statusBgRt.anchorMax = new Vector2(1f, 1f);
        statusBgRt.pivot = new Vector2(1f, 1f);
        statusBgRt.anchoredPosition = new Vector2(-12f, -12f);
        statusBgRt.sizeDelta = new Vector2(-12f, 26f);
        tracker.statusBadgeBg = statusBgObj.AddComponent<Image>();
        tracker.statusBadgeBg.sprite = solidSprite;
        tracker.statusBadgeBg.color = new Color(0.86f, 0.98f, 0.90f, 1f);

        tracker.statusText = CreateTMPText("StatusText", statusBgRt, "READY", 11.5f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.Center);
        StretchFill(tracker.statusText.rectTransform);

        // Row 2: Live World Position
        tracker.positionText = CreateTMPText("PosText", cardRt, "Pos: (0.0, 0.0, 0.0)", 13f, FontStyles.Normal, TextSecondary, TextAlignmentOptions.MidlineLeft);
        RectTransform posRt = tracker.positionText.rectTransform;
        posRt.anchorMin = new Vector2(0f, 0.5f);
        posRt.anchorMax = new Vector2(1f, 0.5f);
        posRt.anchoredPosition = new Vector2(8f, 10f);
        posRt.sizeDelta = new Vector2(-28f, 22f);

        // Row 3: Nearest Sector / Mission Info
        tracker.sectorText = CreateTMPText("SectorText", cardRt, "Near: Sector_39", 13.5f, FontStyles.Bold, TextPrimary, TextAlignmentOptions.MidlineLeft);
        RectTransform secRt = tracker.sectorText.rectTransform;
        secRt.anchorMin = new Vector2(0f, 0.5f);
        secRt.anchorMax = new Vector2(1f, 0.5f);
        secRt.anchoredPosition = new Vector2(8f, -14f);
        secRt.sizeDelta = new Vector2(-28f, 22f);

        // Row 4: Battery Label + Progress Bar
        tracker.batteryText = CreateTMPText("BatteryText", cardRt, "Battery: 100.0%", 12.5f, FontStyles.Bold, TextSecondary, TextAlignmentOptions.MidlineLeft);
        RectTransform batTextRt = tracker.batteryText.rectTransform;
        batTextRt.anchorMin = new Vector2(0f, 0f);
        batTextRt.anchorMax = new Vector2(0.45f, 0f);
        batTextRt.pivot = new Vector2(0f, 0f);
        batTextRt.anchoredPosition = new Vector2(18f, 12f);
        batTextRt.sizeDelta = new Vector2(0f, 20f);

        GameObject barBgObj = CreateUIObject("BatteryBarBg", cardRt);
        RectTransform barBgRt = barBgObj.GetComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.44f, 0f);
        barBgRt.anchorMax = new Vector2(1f, 0f);
        barBgRt.pivot = new Vector2(0f, 0f);
        barBgRt.anchoredPosition = new Vector2(0f, 15f);
        barBgRt.sizeDelta = new Vector2(-16f, 14f);
        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.sprite = solidSprite;
        barBgImg.color = new Color(0.89f, 0.91f, 0.94f, 1f);

        GameObject barFillObj = CreateUIObject("BatteryBarFill", barBgRt);
        RectTransform barFillRt = barFillObj.GetComponent<RectTransform>();
        barFillRt.anchorMin = new Vector2(0f, 0f);
        barFillRt.anchorMax = new Vector2(1f, 1f);
        barFillRt.offsetMin = Vector2.zero;
        barFillRt.offsetMax = Vector2.zero;
        tracker.batteryBarFill = barFillObj.AddComponent<Image>();
        tracker.batteryBarFill.sprite = solidSprite;
        tracker.batteryBarFill.color = new Color(0.06f, 0.72f, 0.50f, 1f);
    }

    private void LayoutStaticMapElements()
    {
        lastViewportSize = mapViewport.rect.size;
        if (lastViewportSize.x <= 10f || lastViewportSize.y <= 10f)
            return;

        // Position all 106 sector nodes
        foreach (var kvp in sectorNodes)
        {
            SectorNodeData node = kvp.Value;
            if (node.nodeRect != null)
            {
                node.nodeRect.anchoredPosition = WorldToMapPoint(node.worldPos);
            }
        }

        // Position all 140 flight corridor edges
        foreach (EdgeVisual edge in edgeVisuals)
        {
            if (edge.lineRect == null) continue;
            Vector2 p1 = WorldToMapPoint(sectorNodes[edge.fromId].worldPos);
            Vector2 p2 = WorldToMapPoint(sectorNodes[edge.toId].worldPos);
            bool isBaseEdge = edge.fromId == fleetManager.baseSector || edge.toId == fleetManager.baseSector;
            PositionLine(edge.lineRect, p1, p2, isBaseEdge ? 3.2f : 2.0f);
        }

        // Refresh active route overlays to match new viewport size
        foreach (DroneTrackerUI tracker in droneTrackers)
        {
            tracker.cachedRoute = null;
        }
    }

    private void UpdateLiveDronesAndTelemetry()
    {
        int onMissionCount = 0;
        int chargingCount = 0;
        int readyCount = 0;

        float pulsePhase = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);

        foreach (DroneTrackerUI tracker in droneTrackers)
        {
            DroneRouteController drone = tracker.drone;
            if (drone == null) continue;

            // 1. Update live 2D map marker position
            Vector3 worldPos = drone.transform.position;
            Vector2 mapPos = WorldToMapPoint(worldPos);
            tracker.markerRoot.anchoredPosition = mapPos;

            // Pulse ring animation while flying or charging
            if (drone.IsOnMission)
            {
                onMissionCount++;
                tracker.pulseRing.gameObject.SetActive(true);
                float scale = Mathf.Lerp(0.95f, 1.45f, pulsePhase);
                tracker.pulseRing.localScale = new Vector3(scale, scale, 1f);
                tracker.pulseImage.color = new Color(tracker.color.r, tracker.color.g, tracker.color.b, Mathf.Lerp(0.75f, 0.15f, pulsePhase));
            }
            else if (drone.IsCharging)
            {
                chargingCount++;
                tracker.pulseRing.gameObject.SetActive(true);
                float scale = Mathf.Lerp(0.95f, 1.25f, pulsePhase);
                tracker.pulseRing.localScale = new Vector3(scale, scale, 1f);
                tracker.pulseImage.color = new Color(0.96f, 0.62f, 0.04f, Mathf.Lerp(0.70f, 0.20f, pulsePhase));
            }
            else
            {
                readyCount++;
                tracker.pulseRing.gameObject.SetActive(false);
            }

            // 2. Show destination target ring & active route path while on mission
            if (drone.IsOnMission && drone.CurrentDestinationSector >= 0)
            {
                tracker.targetRing.gameObject.SetActive(true);
                tracker.targetRing.anchoredPosition = WorldToMapPoint(drone.CurrentIncidentPosition);
                UpdateActiveRouteOverlay(tracker, drone.CurrentOutboundRoute);
            }
            else
            {
                tracker.targetRing.gameObject.SetActive(false);
                ClearActiveRouteOverlay(tracker);
            }

            // 3. Update telemetry card fields
            if (tracker.positionText != null)
            {
                tracker.positionText.text = $"Pos: ({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1})";
            }

            int nearestSector = FindNearestSectorId(worldPos);
            if (tracker.sectorText != null)
            {
                if (drone.IsOnMission && drone.CurrentDestinationSector >= 0)
                {
                    string phase = drone.IsReturning ? "Returning → Base" : $"Target → Sector_{drone.CurrentDestinationSector}";
                    tracker.sectorText.text = $"Near: Sector_{nearestSector}  |  {phase}";
                }
                else
                {
                    tracker.sectorText.text = $"Near: Sector_{nearestSector}  |  Base Pad";
                }
            }

            if (tracker.statusText != null && tracker.statusBadgeBg != null)
            {
                if (drone.IsOnMission)
                {
                    tracker.statusText.text = drone.IsReturning ? "RETURNING" : $"TO SECTOR {drone.CurrentDestinationSector}";
                    tracker.statusText.color = Color.white;
                    tracker.statusBadgeBg.color = tracker.color;
                }
                else if (drone.IsCharging)
                {
                    tracker.statusText.text = "CHARGING";
                    tracker.statusText.color = new Color(0.47f, 0.21f, 0.06f, 1f);
                    tracker.statusBadgeBg.color = new Color(0.99f, 0.91f, 0.54f, 1f);
                }
                else
                {
                    tracker.statusText.text = "READY";
                    tracker.statusText.color = new Color(0.02f, 0.37f, 0.27f, 1f);
                    tracker.statusBadgeBg.color = new Color(0.82f, 0.98f, 0.90f, 1f);
                }
            }

            if (tracker.batteryText != null && tracker.batteryBarFill != null)
            {
                float pct = Mathf.Clamp(drone.battery, 0f, 100f);
                tracker.batteryText.text = $"Battery: {pct:F1}%";
                RectTransform fillRt = tracker.batteryBarFill.rectTransform;
                fillRt.anchorMax = new Vector2(pct / 100f, 1f);

                if (pct >= 50f)
                    tracker.batteryBarFill.color = new Color(0.06f, 0.72f, 0.50f, 1f); // Green
                else if (pct >= 25f)
                    tracker.batteryBarFill.color = new Color(0.96f, 0.62f, 0.04f, 1f); // Amber
                else
                    tracker.batteryBarFill.color = new Color(0.86f, 0.15f, 0.15f, 1f); // Red
            }
        }

        if (headerSummaryText != null)
        {
            headerSummaryText.text =
                $"On Mission: {onMissionCount}/{droneTrackers.Count}   |   Charging: {chargingCount}   |   Ready: {readyCount}   |   Base: Sector_{fleetManager.baseSector}";
        }
    }

    private void UpdateActiveRouteOverlay(DroneTrackerUI tracker, int[] route)
    {
        if (route == tracker.cachedRoute)
            return;

        ClearActiveRouteOverlay(tracker);
        tracker.cachedRoute = route;

        if (route == null || route.Length < 2)
            return;

        for (int i = 0; i < route.Length - 1; i++)
        {
            if (!sectorNodes.TryGetValue(route[i], out SectorNodeData a) ||
                !sectorNodes.TryGetValue(route[i + 1], out SectorNodeData b))
                continue;

            GameObject segObj = CreateUIObject($"Seg_{i}", tracker.routeLineRoot);
            RectTransform rt = segObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = segObj.AddComponent<Image>();
            img.sprite = solidSprite;
            img.color = new Color(tracker.color.r, tracker.color.g, tracker.color.b, 0.55f);
            img.raycastTarget = false;

            PositionLine(rt, WorldToMapPoint(a.worldPos), WorldToMapPoint(b.worldPos), 4.5f);
            tracker.activeRouteSegments.Add(rt);
        }
    }

    private void ClearActiveRouteOverlay(DroneTrackerUI tracker)
    {
        if (tracker.activeRouteSegments.Count == 0 && tracker.cachedRoute == null)
            return;

        for (int i = 0; i < tracker.activeRouteSegments.Count; i++)
        {
            if (tracker.activeRouteSegments[i] != null)
                Destroy(tracker.activeRouteSegments[i].gameObject);
        }

        tracker.activeRouteSegments.Clear();
        tracker.cachedRoute = null;
    }

    private Vector2 WorldToMapPoint(Vector3 worldPos)
    {
        float availW = Mathf.Max(1f, mapViewport.rect.width - mapPadding * 2f);
        float availH = Mathf.Max(1f, mapViewport.rect.height - mapPadding * 2f);

        float worldW = Mathf.Max(1f, maxWorldX - minWorldX);
        float worldH = Mathf.Max(1f, maxWorldZ - minWorldZ);

        float scale = Mathf.Min(availW / worldW, availH / worldH);

        float centerWorldX = (minWorldX + maxWorldX) * 0.5f;
        float centerWorldZ = (minWorldZ + maxWorldZ) * 0.5f;

        float x = (worldPos.x - centerWorldX) * scale;
        float y = (worldPos.z - centerWorldZ) * scale;
        return new Vector2(x, y);
    }

    private int FindNearestSectorId(Vector3 pos)
    {
        int bestId = fleetManager.baseSector;
        float bestSqrDist = float.MaxValue;

        foreach (var kvp in sectorNodes)
        {
            Vector3 nodePos = kvp.Value.worldPos;
            float dx = pos.x - nodePos.x;
            float dz = pos.z - nodePos.z;
            float sqr = dx * dx + dz * dz;
            if (sqr < bestSqrDist)
            {
                bestSqrDist = sqr;
                bestId = kvp.Key;
            }
        }

        return bestId;
    }

    private static void PositionLine(RectTransform rt, Vector2 p1, Vector2 p2, float thickness)
    {
        Vector2 diff = p2 - p1;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = (p1 + p2) * 0.5f;
        rt.sizeDelta = new Vector2(length, thickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = 5; // UI layer
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static RectTransform CreateLayer(string name, Transform parent)
    {
        GameObject obj = CreateUIObject(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        StretchFill(rt);
        return rt;
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddBorder(GameObject target, Color color, float thickness)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(thickness, -thickness);
    }

    private TextMeshProUGUI CreateTMPText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private static Sprite CreateSolidSprite()
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Color[] pixels = { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateCircleSprite(int size, bool filled, float borderThickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = (size - 1) * 0.5f;
        float outerR = half - 1f;
        float innerR = outerR * (1f - borderThickness);

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                float alpha = Mathf.Clamp01(outerR - dist + 0.5f);
                if (!filled)
                {
                    float innerAlpha = Mathf.Clamp01(dist - innerR + 0.5f);
                    alpha = Mathf.Min(alpha, innerAlpha);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
