using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main game controller. Spawns all objects at runtime.
/// NO world-space TextMesh objects — all labels use sprites or Canvas UI.
/// Attach to an empty GameObject called "GameManager".
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Title, Instructions, Credits, Playing, Paused, Win }

    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState currentState = GameState.Title;
    public float progress = 0f;
    public const float MaxProgress = 150f;
    public int wave = 1;
    public int trashCollected = 0;
    public int itemsSorted = 0;
    public int correctSorts = 0;
    public int wrongSorts = 0;
    public int objectsRepaired = 0;
    public int gameTimeSeconds = 0;
    public string currentMessage = "";
    public float messageTimer = 0f;
    public string npcDialogueText = "";
    public string npcDialogueName = "";
    public float npcDialogueTimer = 0f;

    [HideInInspector] public List<GameObject> trashObjects = new List<GameObject>();
    [HideInInspector] public List<GameObject> binObjects = new List<GameObject>();
    [HideInInspector] public List<GameObject> repairObjects = new List<GameObject>();
    [HideInInspector] public List<GameObject> npcObjects = new List<GameObject>();

    private GameObject playerObj;
    private GameObject grayOverlay;
    private float gameTimeAccum = 0f;
    private bool waveSpawned = false;
    private List<GameObject> particles = new List<GameObject>();

    // Sprite cache
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    // ============ GAME DATA ============
    // World: 16 x 10 units, Camera at (8, 5, -10), ortho size 5

    struct ZoneDef { public string name; public float x, y, w, h; public Color color; }
    private ZoneDef[] zones = {
        new ZoneDef { name="Playground",   x=0.5f,  y=6f,   w=4f,   h=3.5f, color=new Color(0.48f,0.72f,0.48f) },
        new ZoneDef { name="Garden",       x=11.5f, y=6f,   w=4f,   h=3.5f, color=new Color(0.53f,0.78f,0.53f) },
        new ZoneDef { name="Central Path", x=5.5f,  y=4f,   w=5f,   h=2f,   color=new Color(0.78f,0.72f,0.53f) },
        new ZoneDef { name="Picnic Area",  x=0.5f,  y=0.5f, w=4.5f, h=3f,   color=new Color(0.56f,0.78f,0.56f) },
        new ZoneDef { name="Pond Area",    x=11.5f, y=0.5f, w=4f,   h=3f,   color=new Color(0.50f,0.72f,0.78f) },
        new ZoneDef { name="Entrance",     x=5.5f,  y=0.3f, w=5f,   h=1.5f, color=new Color(0.75f,0.72f,0.56f) },
    };

    struct TrashTemplate { public string type; public string name; public Color color; public string shape; }
    private TrashTemplate[] trashTemplates = {
        new TrashTemplate { type="garbage",    name="Food Waste",     color=new Color(0.54f,0.41f,0.08f), shape="circle" },
        new TrashTemplate { type="garbage",    name="Crumpled Paper", color=new Color(0.69f,0.69f,0.69f), shape="square" },
        new TrashTemplate { type="garbage",    name="Broken Cup",     color=new Color(0.63f,0.22f,0.17f), shape="triangle" },
        new TrashTemplate { type="garbage",    name="Old Shoe",       color=new Color(0.42f,0.26f,0.15f), shape="diamond" },
        new TrashTemplate { type="garbage",    name="Dirty Rag",      color=new Color(0.53f,0.53f,0.44f), shape="square" },
        new TrashTemplate { type="recyclable", name="Plastic Bottle", color=new Color(0.29f,0.56f,0.85f), shape="circle" },
        new TrashTemplate { type="recyclable", name="Soda Can",       color=new Color(0.75f,0.75f,0.82f), shape="square" },
        new TrashTemplate { type="recyclable", name="Cardboard",      color=new Color(0.78f,0.66f,0.47f), shape="square" },
        new TrashTemplate { type="recyclable", name="Glass Jar",      color=new Color(0.53f,0.78f,0.91f), shape="diamond" },
        new TrashTemplate { type="recyclable", name="Newspaper",      color=new Color(0.85f,0.82f,0.69f), shape="square" },
    };

    private Vector2[] trashPositions = {
        new Vector2(1.2f,8.5f), new Vector2(3.1f,7.8f), new Vector2(2.3f,7.0f), new Vector2(3.8f,7.3f), new Vector2(1.6f,6.8f),
        new Vector2(12.2f,8.3f), new Vector2(13.9f,7.5f), new Vector2(12.9f,6.8f), new Vector2(11.7f,7.3f), new Vector2(14.3f,8.7f),
        new Vector2(6.3f,5.2f), new Vector2(8.2f,5.1f), new Vector2(9.8f,5.0f), new Vector2(7.2f,4.5f),
        new Vector2(1.6f,2.5f), new Vector2(3.5f,1.8f), new Vector2(2.5f,1.2f), new Vector2(4.2f,2.8f), new Vector2(1.3f,1.5f),
        new Vector2(12.2f,2.2f), new Vector2(13.9f,1.5f), new Vector2(12.9f,0.9f), new Vector2(11.7f,1.8f), new Vector2(14.3f,0.7f),
        new Vector2(6.5f,1.0f), new Vector2(8.5f,0.8f), new Vector2(7.5f,0.6f), new Vector2(6.0f,0.7f), new Vector2(9.0f,1.0f),
        new Vector2(3.9f,8.5f), new Vector2(14.1f,8.0f), new Vector2(9.0f,4.8f),
        new Vector2(5.0f,3.5f), new Vector2(3.1f,1.0f), new Vector2(13.5f,0.8f),
        new Vector2(7.5f,0.5f), new Vector2(10.5f,2.5f), new Vector2(2.2f,4.5f),
        new Vector2(14.5f,4.5f), new Vector2(5.5f,8.0f),
    };

    struct RepairDef { public string name; public Vector2 pos; public float w, h; public Color broken, repaired; public int prog; public string type; }
    private RepairDef[] repairDefs = {
        new RepairDef { name="Park Bench",     type="bench",     pos=new Vector2(2.8f,2.5f), w=1.1f,h=0.5f, broken=new Color(0.36f,0.25f,0.13f), repaired=new Color(0.72f,0.53f,0.04f), prog=5 },
        new RepairDef { name="Flower Bed",     type="flowerbed", pos=new Vector2(13f,7.5f),  w=0.9f,h=0.9f, broken=new Color(0.38f,0.38f,0.25f), repaired=new Color(1f,0.41f,0.71f),   prog=4 },
        new RepairDef { name="Wooden Fence",   type="fence",     pos=new Vector2(2.2f,7f),   w=1.6f,h=0.4f, broken=new Color(0.24f,0.17f,0.06f), repaired=new Color(0.78f,0.63f,0.31f), prog=4 },
        new RepairDef { name="Fountain",       type="fountain",  pos=new Vector2(8f,5f),     w=0.9f,h=0.9f, broken=new Color(0.44f,0.44f,0.44f), repaired=new Color(0.38f,0.69f,0.88f), prog=6 },
        new RepairDef { name="Swing Set",      type="swing",     pos=new Vector2(3.5f,7.5f), w=0.9f,h=0.9f, broken=new Color(0.31f,0.31f,0.31f), repaired=new Color(1f,0.84f,0f),      prog=4 },
        new RepairDef { name="Lamp Post",      type="lamppost",  pos=new Vector2(8f,7.5f),   w=0.3f,h=0.9f, broken=new Color(0.25f,0.25f,0.25f), repaired=new Color(1f,0.89f,0.71f),   prog=3 },
        new RepairDef { name="Stone Bridge",   type="bridge",    pos=new Vector2(13f,2.5f),  w=1.3f,h=0.5f, broken=new Color(0.31f,0.31f,0.31f), repaired=new Color(0.63f,0.63f,0.63f), prog=4 },
        new RepairDef { name="Bird Bath",      type="birdbath",  pos=new Vector2(13.5f,7f),  w=0.6f,h=0.7f, broken=new Color(0.38f,0.38f,0.38f), repaired=new Color(0.69f,0.82f,0.88f), prog=3 },
        new RepairDef { name="Garden Planter", type="planter",   pos=new Vector2(12f,2f),    w=0.8f,h=0.6f, broken=new Color(0.31f,0.25f,0.19f), repaired=new Color(0.82f,0.41f,0.12f), prog=3 },
        new RepairDef { name="Path Light",     type="pathlight", pos=new Vector2(5.5f,5f),   w=0.3f,h=0.7f, broken=new Color(0.22f,0.22f,0.22f), repaired=new Color(1f,0.84f,0f),      prog=2 },
    };

    struct NPCDef { public Vector2 pos; public string name; public string[] dialogues; public Color bodyColor, hatColor; }
    private NPCDef[] npcDefs = {
        new NPCDef { pos=new Vector2(6.5f,1.2f), name="Park Ranger", bodyColor=new Color(0.36f,0.25f,0.13f), hatColor=new Color(0.55f,0.27f,0.07f),
            dialogues=new string[]{ "Welcome! This park needs your help. Pick up trash and sort it into the right bins!",
                "RED bins are for garbage. BLUE bins are for recyclables. Sorting correctly earns more points!",
                "Press E near broken objects to repair them. Each repair helps restore the park!",
                "The park will come back to life as you clean and repair. Reach 100% to fully restore it!",
                "You can carry up to 5 items at a time. Visit the bins often to sort what you've collected!" } },
        new NPCDef { pos=new Vector2(2.5f,4.5f), name="Kids", bodyColor=new Color(1f,0.6f,0.4f), hatColor=new Color(1f,0.39f,0.28f),
            dialogues=new string[]{ "Our playground is so messy! Can you help clean it up?",
                "The swing set is broken... I wish someone could fix it!",
                "Thank you for cleaning! The playground is looking better already!" } },
        new NPCDef { pos=new Vector2(13.5f,4.5f), name="Gardener", bodyColor=new Color(0.2f,0.5f,0.2f), hatColor=new Color(0.13f,0.55f,0.13f),
            dialogues=new string[]{ "The garden used to be so beautiful... now it's full of weeds and trash.",
                "If you could repair the flower bed, the butterflies would come back!",
                "You're doing great! The garden is starting to bloom again!" } }
    };

    private Vector2[] binPositions = { new Vector2(6f,1.2f), new Vector2(10f,1.2f) };
    private string[] binTypes = { "garbage", "recyclable" };

    // ============ LIFECYCLE ============

    void Awake() { Instance = this; }

    void Start()
    {
        gameObject.AddComponent<AudioManager>();
        BuildSpriteCache();

        SpawnBackground();
        SpawnZones();
        SpawnPaths();
        SpawnBorder();
        SpawnGrayOverlay();
        SpawnDecorativeTrees();
        SpawnRepairObjects();
        SpawnBins();
        SpawnNPCs();
        SpawnTrash();
        SpawnPlayer();

        GameObject uiObj = new GameObject("GameUI");
        uiObj.AddComponent<GameUI>();

        SetState(GameState.Title);
    }

    void Update()
    {
        if (currentState != GameState.Playing) return;

        gameTimeAccum += Time.deltaTime;
        if (gameTimeAccum >= 1f) { gameTimeAccum -= 1f; gameTimeSeconds++; }

        if (messageTimer > 0) messageTimer -= Time.deltaTime;

        if (npcDialogueTimer > 0) { npcDialogueTimer -= Time.deltaTime; if (npcDialogueTimer <= 0) npcDialogueText = ""; }

        // Update gray overlay (fades out as progress increases)
        if (grayOverlay != null)
        {
            float targetAlpha = 0.65f * (1f - progress / MaxProgress);
            SpriteRenderer sr = grayOverlay.GetComponent<SpriteRenderer>();
            if (sr != null) { Color c = sr.color; c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 2f); sr.color = c; }
        }

        // Update zone colors based on progress
        float gp = progress / MaxProgress;
        for (int i = 0; i < zones.Length; i++)
        {
            Transform zoneT = transform.Find("Zone_" + zones[i].name);
            if (zoneT != null)
            {
                SpriteRenderer zsr = zoneT.GetComponent<SpriteRenderer>();
                if (zsr != null)
                {
                    Color baseColor = zones[i].color;
                    zsr.color = Color.Lerp(baseColor * 0.35f, baseColor, gp);
                }
            }
        }

        // Update tree visibility
        float treeAlpha = Mathf.Min(1f, gp * 1.5f);
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Tree_"))
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) { Color c = sr.color; c.a = treeAlpha; sr.color = c; }
            }
            if (child.name.StartsWith("Trunk_"))
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) { Color c = sr.color; c.a = treeAlpha; sr.color = c; }
            }
        }

        // Path fade-in
        float pathAlpha = Mathf.Min(0.6f, gp * 0.8f);
        Transform pathV = transform.Find("PathV");
        if (pathV != null) { SpriteRenderer sr = pathV.GetComponent<SpriteRenderer>(); if (sr != null) { Color c = sr.color; c.a = pathAlpha; sr.color = c; } }
        Transform pathH = transform.Find("PathH");
        if (pathH != null) { SpriteRenderer sr = pathH.GetComponent<SpriteRenderer>(); if (sr != null) { Color c = sr.color; c.a = pathAlpha; sr.color = c; } }

        // Repair animation decay
        foreach (GameObject repObj in repairObjects)
        {
            ParkObject po = repObj.GetComponent<ParkObject>();
            if (po != null && po.repairAnimTimer > 0)
            {
                po.repairAnimTimer -= Time.deltaTime * 2f;
                if (po.repairAnimTimer < 0) po.repairAnimTimer = 0;
                float scale = 1f + po.repairAnimTimer * 0.15f;
                repObj.transform.localScale = new Vector3(po.baseScale.x * scale, po.baseScale.y * scale, 1f);
            }
        }

        // Trash bobbing animation
        float time = Time.time;
        for (int i = 0; i < trashObjects.Count; i++)
        {
            if (trashObjects[i] != null && trashObjects[i].activeSelf)
            {
                ParkObject po = trashObjects[i].GetComponent<ParkObject>();
                if (po != null)
                {
                    float bob = Mathf.Sin(time * 2.5f + po.bobOffset) * 0.06f;
                    trashObjects[i].transform.position = new Vector3(po.basePos.x, po.basePos.y + bob, 0f);
                }
            }
        }

        // Cleanup particles
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            if (particles[i] == null) { particles.RemoveAt(i); continue; }
            ParticleObj po = particles[i].GetComponent<ParticleObj>();
            if (po != null && po.IsDead()) { Destroy(particles[i]); particles.RemoveAt(i); }
        }

        CheckWaveSpawn();

        if (progress >= MaxProgress) { SetState(GameState.Win); AudioManager.Instance.PlayWin(); AudioManager.Instance.StopBGMusic(); }
    }

    // ============ STATE MANAGEMENT ============

    public void SetState(GameState state)
    {
        currentState = state;
        if (playerObj != null)
        {
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = (state == GameState.Playing || state == GameState.Paused);
        }
        if (state == GameState.Playing && !AudioManager.Instance.IsMuted()) AudioManager.Instance.StartBGMusic();
        if (state == GameState.Paused) AudioManager.Instance.StopBGMusic();
    }

    public void StartGame()
    {
        progress = 0f; wave = 1; trashCollected = 0; itemsSorted = 0; correctSorts = 0;
        wrongSorts = 0; objectsRepaired = 0; gameTimeSeconds = 0; gameTimeAccum = 0f;
        messageTimer = 0f; npcDialogueText = ""; npcDialogueTimer = 0f;

        for (int i = 0; i < trashObjects.Count; i++)
        {
            ParkObject po = trashObjects[i].GetComponent<ParkObject>();
            po.collected = false; trashObjects[i].SetActive(true);
            trashObjects[i].transform.position = trashPositions[i];
            po.basePos = trashPositions[i];
            po.GetComponent<SpriteRenderer>().color = po.itemColor;
        }
        foreach (GameObject repObj in repairObjects)
        {
            ParkObject po = repObj.GetComponent<ParkObject>();
            po.repaired = false; po.repairAnimTimer = 0;
            repObj.GetComponent<SpriteRenderer>().color = po.brokenColor;
            repObj.transform.localScale = po.baseScale;
            // Reset status indicator: show X, hide checkmark
            Transform statusT = repObj.transform.Find("StatusX");
            if (statusT != null) statusT.gameObject.SetActive(true);
            Transform checkT = repObj.transform.Find("StatusCheck");
            if (checkT != null) checkT.gameObject.SetActive(false);
            // Reset glow
            Transform glowT = repObj.transform.Find("Glow");
            if (glowT != null) glowT.gameObject.SetActive(false);
        }
        foreach (GameObject npcObj in npcObjects)
        {
            ParkObject po = npcObj.GetComponent<ParkObject>();
            po.currentDialogue = 0;
        }
        if (playerObj != null)
        {
            playerObj.transform.position = new Vector3(8f, 2f, 0f);
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc != null) pc.inventory.Clear();
        }
        if (grayOverlay != null)
        {
            SpriteRenderer sr = grayOverlay.GetComponent<SpriteRenderer>();
            Color c = sr.color; c.a = 0.65f; sr.color = c;
        }
        currentMessage = "Welcome! Pick up trash, sort it in bins, and press E to repair objects!";
        messageTimer = 4f;
        SetState(GameState.Playing);
    }

    public void AddProgress(int amount)
    {
        float prevProgress = progress;
        progress = Mathf.Clamp(progress + amount, 0f, MaxProgress);
        int prevMilestone = Mathf.FloorToInt(prevProgress / 25f);
        int currMilestone = Mathf.FloorToInt(progress / 25f);
        if (currMilestone > prevMilestone && progress < MaxProgress)
        {
            AudioManager.Instance.PlayMilestone();
            currentMessage = "Park is " + (currMilestone * 25) + "% restored! Keep going!";
            messageTimer = 2.5f;
        }
    }

    void CheckWaveSpawn()
    {
        int uncollected = 0;
        foreach (GameObject t in trashObjects) if (t != null && t.activeSelf) uncollected++;
        if (uncollected == 0 && !waveSpawned && wave < 5)
        {
            wave++; waveSpawned = true;
            foreach (GameObject t in trashObjects)
            {
                ParkObject po = t.GetComponent<ParkObject>();
                if (!t.activeSelf && Random.value < 0.75f)
                {
                    Vector3 newPos = po.basePos;
                    newPos.x += (Random.value - 0.5f) * 1f; newPos.y += (Random.value - 0.5f) * 1f;
                    newPos.x = Mathf.Clamp(newPos.x, 1f, 15f); newPos.y = Mathf.Clamp(newPos.y, 0.5f, 9.5f);
                    po.Revive(newPos);
                }
            }
            currentMessage = "Wave " + wave + "! More trash has appeared!";
            messageTimer = 3f; AudioManager.Instance.PlayMilestone();
            Invoke("ResetWaveSpawned", 2f);
        }
    }
    void ResetWaveSpawned() { waveSpawned = false; }

    // ============ SPRITE GENERATION ============

    void BuildSpriteCache()
    {
        spriteCache["white"] = MakePixelSprite();
        spriteCache["circle"] = MakeShapeSprite(ShapeType.Circle);
        spriteCache["square"] = MakeShapeSprite(ShapeType.Square);
        spriteCache["triangle"] = MakeShapeSprite(ShapeType.Triangle);
        spriteCache["diamond"] = MakeShapeSprite(ShapeType.Diamond);
        spriteCache["star"] = MakeShapeSprite(ShapeType.Star);
        spriteCache["player"] = MakePlayerSprite();
        spriteCache["xMark"] = MakeXSprite();
        spriteCache["checkMark"] = MakeCheckSprite();
    }

    Sprite GetSprite(string key) { return spriteCache.ContainsKey(key) ? spriteCache[key] : spriteCache["white"]; }

    enum ShapeType { Circle, Square, Triangle, Diamond, Star }

    Sprite MakePixelSprite()
    {
        Texture2D tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
    }

    Sprite MakeShapeSprite(ShapeType shape)
    {
        int size = 64; Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - size / 2f) / (size / 2f); float ny = (y - size / 2f) / (size / 2f);
                bool inside = false;
                switch (shape)
                {
                    case ShapeType.Circle: inside = nx * nx + ny * ny <= 0.85f; break;
                    case ShapeType.Square: inside = Mathf.Abs(nx) <= 0.8f && Mathf.Abs(ny) <= 0.8f; break;
                    case ShapeType.Triangle: inside = ny >= -0.7f && ny <= 0.8f && Mathf.Abs(nx) <= (0.8f - ny) * 0.7f + 0.05f; break;
                    case ShapeType.Diamond: inside = Mathf.Abs(nx) + Mathf.Abs(ny) <= 0.8f; break;
                    case ShapeType.Star:
                        float dist = Mathf.Sqrt(nx * nx + ny * ny);
                        float angle = Mathf.Atan2(ny, nx);
                        float starR = 0.5f + 0.35f * Mathf.Cos(angle * 5f);
                        inside = dist <= starR;
                        break;
                }
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(pixels); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    Sprite MakePlayerSprite()
    {
        int w = 32, h = 48; Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color green = new Color(0.3f, 0.69f, 0.31f); Color darkGreen = new Color(0.18f, 0.49f, 0.2f);
        Color skin = new Color(1f, 0.8f, 0.5f); Color brown = new Color(0.36f, 0.25f, 0.13f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                if (y >= 42 && y < 48 && x >= 6 && x < 26) c = darkGreen;
                if (y >= 39 && y < 43 && x >= 3 && x < 29) c = darkGreen;
                if (y >= 31 && y < 41 && x >= 9 && x < 23) c = skin;
                if (y >= 35 && y < 38 && (x == 12 || x == 19)) c = new Color(0.2f, 0.2f, 0.2f);
                if (y >= 32 && y < 34 && x >= 13 && x < 19) c = new Color(0.55f, 0.41f, 0.08f);
                if (y >= 14 && y < 33 && x >= 7 && x < 25) c = green;
                if (y >= 14 && y < 17 && x >= 7 && x < 25) c = brown;
                if (y >= 4 && y < 16) { if (x >= 9 && x < 15) c = darkGreen; if (x >= 17 && x < 23) c = darkGreen; }
                if (y >= 2 && y < 6) { if (x >= 8 && x < 16) c = brown; if (x >= 16 && x < 24) c = brown; }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 32f);
    }

    /// <summary>Red X mark for broken repair objects</summary>
    Sprite MakeXSprite()
    {
        int size = 32; Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onDiag = Mathf.Abs(x - y) < 3 || Mathf.Abs(x - (size - 1 - y)) < 3;
                bool inCircle = (x - size/2f) * (x - size/2f) + (y - size/2f) * (y - size/2f) < (size/2f) * (size/2f);
                tex.SetPixel(x, y, (onDiag && inCircle) ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    /// <summary>Green checkmark for repaired objects</summary>
    Sprite MakeCheckSprite()
    {
        int size = 32; Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Check mark: short stroke going down-left, then long stroke going up-right
                bool onStroke = false;
                // Short left stroke (bottom of check)
                float t1 = (float)y / size;
                float expectedX1 = size * 0.3f + t1 * size * 0.15f;
                if (y < size * 0.5f && Mathf.Abs(x - expectedX1) < 2.5f) onStroke = true;
                // Long right stroke (top of check)
                float t2 = (float)(y - size * 0.35f) / (size * 0.65f);
                float expectedX2 = size * 0.35f + t2 * size * 0.45f;
                if (y >= size * 0.35f && Mathf.Abs(x - expectedX2) < 2.5f) onStroke = true;

                bool inCircle = (x - size/2f) * (x - size/2f) + (y - size/2f) * (y - size/2f) < (size/2f) * (size/2f);
                tex.SetPixel(x, y, (onStroke && inCircle) ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    // ============ SPAWNING ============

    void SpawnBackground()
    {
        GameObject bg = new GameObject("Background");
        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite("white");
        sr.color = new Color(0.30f, 0.38f, 0.28f); // Dark muted green
        sr.sortingOrder = -20;
        bg.transform.position = new Vector3(8f, 5f, 0f);
        bg.transform.localScale = new Vector3(16f, 10f, 1f);
        bg.transform.SetParent(this.transform);
    }

    void SpawnZones()
    {
        foreach (ZoneDef z in zones)
        {
            // Zone background
            GameObject zone = new GameObject("Zone_" + z.name);
            SpriteRenderer sr = zone.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite("white");
            sr.color = z.color * 0.35f; // Starts dimmed
            sr.sortingOrder = -18;
            zone.transform.position = new Vector3(z.x + z.w / 2f, z.y + z.h / 2f, 0f);
            zone.transform.localScale = new Vector3(z.w, z.h, 1f);
            zone.transform.SetParent(this.transform);

            // Zone border (slightly larger, subtle)
            GameObject border = new GameObject("ZoneBorder_" + z.name);
            SpriteRenderer bsr = border.AddComponent<SpriteRenderer>();
            bsr.sprite = GetSprite("white");
            bsr.color = new Color(z.color.r * 0.6f, z.color.g * 0.6f, z.color.b * 0.6f, 0.25f);
            bsr.sortingOrder = -17;
            border.transform.position = new Vector3(z.x + z.w / 2f, z.y + z.h / 2f, 0f);
            border.transform.localScale = new Vector3(z.w + 0.06f, z.h + 0.06f, 1f);
            border.transform.SetParent(this.transform);

            // NO TEXT LABELS — zones are identified by color and position
        }
    }

    void SpawnDecorativeTrees()
    {
        Vector2[] treePositions = {
            new Vector2(0.5f,9.2f), new Vector2(15.5f,9.2f), new Vector2(0.5f,0.5f),
            new Vector2(15.5f,0.5f), new Vector2(5f,9.2f), new Vector2(11f,9.2f),
            new Vector2(5f,0.5f), new Vector2(11f,0.5f),
        };
        for (int i = 0; i < treePositions.Length; i++)
        {
            // Tree trunk
            GameObject trunk = new GameObject("Trunk_" + i);
            trunk.transform.position = new Vector3(treePositions[i].x, treePositions[i].y - 0.1f, 0f);
            SpriteRenderer tsr = trunk.AddComponent<SpriteRenderer>();
            tsr.sprite = GetSprite("white");
            tsr.color = new Color(0.31f, 0.2f, 0.08f, 0f);
            tsr.sortingOrder = -15;
            trunk.transform.localScale = new Vector3(0.15f, 0.35f, 1f);
            trunk.transform.SetParent(this.transform);

            // Tree canopy
            GameObject tree = new GameObject("Tree_" + i);
            tree.transform.position = new Vector3(treePositions[i].x, treePositions[i].y + 0.3f, 0f);
            SpriteRenderer sr = tree.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite("circle");
            sr.color = new Color(0.13f, 0.39f, 0.13f, 0f);
            sr.sortingOrder = -14;
            tree.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            tree.transform.SetParent(this.transform);
        }
    }

    void SpawnPaths()
    {
        // Vertical path
        GameObject pathV = new GameObject("PathV");
        SpriteRenderer srV = pathV.AddComponent<SpriteRenderer>();
        srV.sprite = GetSprite("white");
        srV.color = new Color(0.78f, 0.7f, 0.55f, 0f);
        srV.sortingOrder = -19;
        pathV.transform.position = new Vector3(8f, 5f, 0f);
        pathV.transform.localScale = new Vector3(0.8f, 9f, 1f);
        pathV.transform.SetParent(this.transform);

        // Horizontal path
        GameObject pathH = new GameObject("PathH");
        SpriteRenderer srH = pathH.AddComponent<SpriteRenderer>();
        srH.sprite = GetSprite("white");
        srH.color = new Color(0.78f, 0.7f, 0.55f, 0f);
        srH.sortingOrder = -19;
        pathH.transform.position = new Vector3(8f, 5f, 0f);
        pathH.transform.localScale = new Vector3(15f, 0.6f, 1f);
        pathH.transform.SetParent(this.transform);
    }

    void SpawnGrayOverlay()
    {
        // Gray overlay sits between background elements and game objects
        grayOverlay = new GameObject("GrayOverlay");
        SpriteRenderer sr = grayOverlay.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite("white");
        sr.color = new Color(0.35f, 0.35f, 0.35f, 0.65f);
        sr.sortingOrder = 0; // Between bg (-20 to -14) and game objects (1+)
        grayOverlay.transform.position = new Vector3(8f, 5f, 0f);
        grayOverlay.transform.localScale = new Vector3(16f, 10f, 1f);
        grayOverlay.transform.SetParent(this.transform);
    }

    void SpawnBorder()
    {
        GameObject border = new GameObject("Border");
        SpriteRenderer sr = border.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite("white");
        sr.color = new Color(0.36f, 0.25f, 0.13f);
        sr.sortingOrder = -12;
        border.transform.position = new Vector3(8f, 5f, 0f);
        border.transform.localScale = new Vector3(16.2f, 10.2f, 1f);
        border.transform.SetParent(this.transform);

        GameObject inner = new GameObject("InnerBorder");
        SpriteRenderer isr = inner.AddComponent<SpriteRenderer>();
        isr.sprite = GetSprite("white");
        isr.color = new Color(0.54f, 0.41f, 0.08f);
        isr.sortingOrder = -11;
        inner.transform.position = new Vector3(8f, 5f, 0f);
        inner.transform.localScale = new Vector3(16.1f, 10.1f, 1f);
        inner.transform.SetParent(this.transform);
    }

    void SpawnPlayer()
    {
        playerObj = new GameObject("Player");
        playerObj.transform.position = new Vector3(8f, 2f, 0f);

        Rigidbody2D rb = playerObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SpriteRenderer sr = playerObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite("player");
        sr.sortingOrder = 10;

        BoxCollider2D col = playerObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.8f);

        playerObj.AddComponent<PlayerController>();
    }

    void SpawnBins()
    {
        for (int i = 0; i < binPositions.Length; i++)
        {
            Color binColor = i == 0 ? new Color(0.75f, 0.18f, 0.18f) : new Color(0.18f, 0.35f, 0.75f);
            Color lidColor = i == 0 ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.25f, 0.50f, 0.85f);

            GameObject bin = new GameObject("Bin_" + binTypes[i]);
            bin.transform.position = new Vector3(binPositions[i].x, binPositions[i].y, 0f);

            // Bin body
            SpriteRenderer sr = bin.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite("square");
            sr.color = binColor;
            sr.sortingOrder = 5;
            bin.transform.localScale = new Vector3(0.9f, 0.65f, 1f);

            // Bin lid (slightly wider, on top)
            GameObject lid = new GameObject("Lid");
            lid.transform.SetParent(bin.transform);
            lid.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            SpriteRenderer lsr = lid.AddComponent<SpriteRenderer>();
            lsr.sprite = GetSprite("square");
            lsr.color = lidColor;
            lsr.sortingOrder = 6;
            lid.transform.localScale = new Vector3(1.1f, 0.12f, 1f);

            // Small colored dot indicator (red dot = garbage, blue dot = recycle)
            // Garbage: red circle, Recyclable: blue circle with "R" shape
            GameObject indicator = new GameObject("Indicator");
            indicator.transform.SetParent(bin.transform);
            indicator.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            SpriteRenderer isr = indicator.AddComponent<SpriteRenderer>();
            isr.sprite = GetSprite("circle");
            isr.color = Color.white;
            isr.sortingOrder = 7;
            indicator.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

            // Glow behind bin to make it stand out
            GameObject glow = new GameObject("BinGlow");
            glow.transform.SetParent(bin.transform);
            glow.transform.localPosition = Vector3.zero;
            SpriteRenderer gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = GetSprite("circle");
            gsr.color = new Color(binColor.r, binColor.g, binColor.b, 0.15f);
            gsr.sortingOrder = 4;
            glow.transform.localScale = new Vector3(2f, 2f, 1f);

            ParkObject po = bin.AddComponent<ParkObject>();
            po.objectType = ParkObject.ObjectType.Bin;
            po.dataType = binTypes[i];
            po.objectName = binTypes[i] == "garbage" ? "TRASH" : "RECYCLE";
            po.spriteRenderer = sr;

            binObjects.Add(bin);
        }
    }

    void SpawnRepairObjects()
    {
        foreach (RepairDef rd in repairDefs)
        {
            GameObject rep = new GameObject("Repair_" + rd.name);
            rep.transform.position = new Vector3(rd.pos.x, rd.pos.y, 0f);

            // Main body
            SpriteRenderer sr = rep.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite("square");
            sr.color = rd.broken;
            sr.sortingOrder = 3;
            rep.transform.localScale = new Vector3(rd.w, rd.h, 1f);

            // Glow (hidden initially, shown when repaired)
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(rep.transform);
            glow.transform.localPosition = Vector3.zero;
            SpriteRenderer gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = GetSprite("circle");
            gsr.color = new Color(0.56f, 0.93f, 0.56f, 0.2f);
            gsr.sortingOrder = 2;
            glow.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            glow.SetActive(false);

            // Status X mark (shown when broken)
            GameObject statusX = new GameObject("StatusX");
            statusX.transform.SetParent(rep.transform);
            statusX.transform.localPosition = new Vector3(0f, rd.h / 2f + 0.15f, 0f);
            SpriteRenderer xsr = statusX.AddComponent<SpriteRenderer>();
            xsr.sprite = GetSprite("xMark");
            xsr.color = new Color(1f, 0.3f, 0.3f);
            xsr.sortingOrder = 4;
            statusX.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            // Status check mark (hidden, shown when repaired)
            GameObject statusCheck = new GameObject("StatusCheck");
            statusCheck.transform.SetParent(rep.transform);
            statusCheck.transform.localPosition = new Vector3(0f, rd.h / 2f + 0.15f, 0f);
            SpriteRenderer csr = statusCheck.AddComponent<SpriteRenderer>();
            csr.sprite = GetSprite("checkMark");
            csr.color = new Color(0.3f, 0.9f, 0.3f);
            csr.sortingOrder = 4;
            statusCheck.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            statusCheck.SetActive(false);

            ParkObject po = rep.AddComponent<ParkObject>();
            po.objectType = ParkObject.ObjectType.Repair;
            po.objectName = rd.name;
            po.brokenColor = rd.broken;
            po.repairedColor = rd.repaired;
            po.progressValue = rd.prog;
            po.spriteRenderer = sr;
            po.baseScale = rep.transform.localScale;

            repairObjects.Add(rep);
        }
    }

    void SpawnNPCs()
    {
        foreach (NPCDef nd in npcDefs)
        {
            GameObject npc = new GameObject("NPC_" + nd.name);
            npc.transform.position = new Vector3(nd.pos.x, nd.pos.y, 0f);

            // NPC body
            SpriteRenderer sr = npc.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite("npc");
            sr.color = nd.bodyColor;
            sr.sortingOrder = 6;
            npc.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            // Hat/cap on top (small colored circle)
            GameObject hat = new GameObject("Hat");
            hat.transform.SetParent(npc.transform);
            hat.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            SpriteRenderer hsr = hat.AddComponent<SpriteRenderer>();
            hsr.sprite = GetSprite("circle");
            hsr.color = nd.hatColor;
            hsr.sortingOrder = 7;
            hat.transform.localScale = new Vector3(0.35f, 0.2f, 1f);

            // Exclamation mark indicator (shows NPC is talkable)
            GameObject excl = new GameObject("Exclaim");
            excl.transform.SetParent(npc.transform);
            excl.transform.localPosition = new Vector3(0.15f, 0.8f, 0f);
            SpriteRenderer esr = excl.AddComponent<SpriteRenderer>();
            esr.sprite = GetSprite("diamond");
            esr.color = new Color(1f, 0.85f, 0f);
            esr.sortingOrder = 8;
            excl.transform.localScale = new Vector3(0.15f, 0.15f, 1f);

            ParkObject po = npc.AddComponent<ParkObject>();
            po.objectType = ParkObject.ObjectType.NPC;
            po.objectName = nd.name;
            po.dialogues = nd.dialogues;
            po.spriteRenderer = sr;

            npcObjects.Add(npc);
        }
    }

    void SpawnTrash()
    {
        for (int i = 0; i < trashPositions.Length; i++)
        {
            TrashTemplate tmpl = trashTemplates[i % trashTemplates.Length];
            GameObject trash = new GameObject("Trash_" + i);
            trash.transform.position = new Vector3(trashPositions[i].x, trashPositions[i].y, 0f);

            // Shape based on template
            SpriteRenderer sr = trash.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite(tmpl.shape);
            sr.color = tmpl.color;
            sr.sortingOrder = 4;
            trash.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

            // Colored ring behind trash to indicate type
            // Orange-brown ring = garbage, Blue ring = recyclable
            GameObject ring = new GameObject("TypeRing");
            ring.transform.SetParent(trash.transform);
            ring.transform.localPosition = Vector3.zero;
            SpriteRenderer rsr = ring.AddComponent<SpriteRenderer>();
            rsr.sprite = GetSprite("circle");
            rsr.color = tmpl.type == "garbage"
                ? new Color(0.82f, 0.45f, 0.2f, 0.35f)
                : new Color(0.2f, 0.55f, 0.85f, 0.35f);
            rsr.sortingOrder = 3;
            ring.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

            ParkObject po = trash.AddComponent<ParkObject>();
            po.objectType = ParkObject.ObjectType.Trash;
            po.dataType = tmpl.type;
            po.objectName = tmpl.name;
            po.itemColor = tmpl.color;
            po.spriteRenderer = sr;
            po.bobOffset = Random.value * Mathf.PI * 2f;
            po.basePos = trashPositions[i];

            trashObjects.Add(trash);
        }
    }

    // ============ VISUAL EFFECTS ============

    public void SpawnParticles(Vector3 pos, Color color)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject p = new GameObject("Particle");
            p.transform.position = pos;
            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = GetSprite("circle");
            sr.color = color;
            sr.sortingOrder = 20;
            p.transform.localScale = Vector3.one * 0.1f;
            ParticleObj po = p.AddComponent<ParticleObj>();
            po.vx = (Random.value - 0.5f) * 3f;
            po.vy = (Random.value - 0.5f) * 3f + 1f;
            po.life = 0.6f + Random.value * 0.3f;
            particles.Add(p);
        }
    }

    // Shows a floating text using HUD message system instead of world-space TextMesh
    public void ShowFloatingText(Vector3 pos, string text, Color color)
    {
        // Use the HUD message system instead of world-space text
        if (currentMessage == "" || messageTimer <= 0)
        {
            currentMessage = text;
            messageTimer = 1.5f;
        }
    }

    public void ShowMessage(string msg, float duration) { currentMessage = msg; messageTimer = duration; }

    public void ShowNPCDialogue(string name, string text) { npcDialogueName = name; npcDialogueText = text; npcDialogueTimer = 4f; }

    public GameObject GetPlayer() => playerObj;

    public int GetInventoryCount()
    {
        if (playerObj == null) return 0;
        PlayerController pc = playerObj.GetComponent<PlayerController>();
        return pc != null ? pc.inventory.Count : 0;
    }

    public List<PlayerController.TrashInfo> GetInventory()
    {
        if (playerObj == null) return new List<PlayerController.TrashInfo>();
        PlayerController pc = playerObj.GetComponent<PlayerController>();
        return pc != null ? pc.inventory : new List<PlayerController.TrashInfo>();
    }
}

// ============ HELPER COMPONENTS ============

public class ParticleObj : MonoBehaviour
{
    public float vx, vy, life;
    void Update()
    {
        transform.position += new Vector3(vx, vy, 0f) * Time.deltaTime;
        vy -= 3f * Time.deltaTime;
        life -= Time.deltaTime;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) { Color c = sr.color; c.a = Mathf.Max(0, life); sr.color = c; }
        transform.localScale = Vector3.one * Mathf.Max(0.02f, life * 0.1f);
    }
    public bool IsDead() => life <= 0;
}
