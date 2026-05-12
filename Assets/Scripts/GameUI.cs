using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Creates and manages all UI elements at runtime.
/// Matches the HTML5 Canvas version's UI exactly.
/// </summary>
public class GameUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject titleScreen, instructionsScreen, creditsScreen;
    private GameObject hudPanel, pausePanel, winPanel, npcDialoguePanel;
    private Text progressText, inventoryText, messageText, timerText, waveText;
    private Text trashCountText, repairCountText, controlsText;
    private Image progressBarFill;
    private Text npcNameText, npcDialogueTextUI;
    private Text winStatsText;
    private Font uiFont;

    void Start()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("UICanvas");
        canvasObj.transform.SetParent(null);
        DontDestroyOnLoad(canvasObj);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 600);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem exists (compatible with Input System Package)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            try
            {
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null) es.AddComponent(inputModuleType);
                else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            catch { es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>(); }
        }

        // Load font
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (uiFont == null) { Debug.LogError("No built-in font found!"); return; }

        BuildTitleScreen();
        BuildInstructionsScreen();
        BuildCreditsScreen();
        BuildHUD();
        BuildPausePanel();
        BuildWinPanel();
        BuildNPCDialogue();
        ShowTitleScreen();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        switch (GameManager.Instance.currentState)
        {
            case GameManager.GameState.Title: ShowTitleScreen(); break;
            case GameManager.GameState.Instructions: ShowInstructionsScreen(); break;
            case GameManager.GameState.Credits: ShowCreditsScreen(); break;
            case GameManager.GameState.Playing: HideAllScreens(); UpdateHUD(); break;
            case GameManager.GameState.Paused: ShowPausePanel(); break;
            case GameManager.GameState.Win: ShowWinPanel(); break;
        }

        if (npcDialoguePanel != null)
        {
            npcDialoguePanel.SetActive(GameManager.Instance.npcDialogueTimer > 0);
            if (GameManager.Instance.npcDialogueTimer > 0)
            {
                npcNameText.text = GameManager.Instance.npcDialogueName;
                npcDialogueTextUI.text = GameManager.Instance.npcDialogueText;
            }
        }
    }

    // ============ VISIBILITY ============

    void HideAllScreens()
    {
        SetActive(titleScreen, false);
        SetActive(instructionsScreen, false);
        SetActive(creditsScreen, false);
        SetActive(hudPanel, true);
        SetActive(pausePanel, false);
        SetActive(winPanel, false);
    }

    void ShowTitleScreen() { SetActive(titleScreen, true); SetActive(instructionsScreen, false); SetActive(creditsScreen, false); SetActive(hudPanel, false); SetActive(pausePanel, false); SetActive(winPanel, false); }
    void ShowInstructionsScreen() { SetActive(titleScreen, false); SetActive(instructionsScreen, true); SetActive(creditsScreen, false); SetActive(hudPanel, false); SetActive(pausePanel, false); SetActive(winPanel, false); }
    void ShowCreditsScreen() { SetActive(titleScreen, false); SetActive(instructionsScreen, false); SetActive(creditsScreen, true); SetActive(hudPanel, false); SetActive(pausePanel, false); SetActive(winPanel, false); }
    void ShowPausePanel() { SetActive(hudPanel, true); SetActive(pausePanel, true); SetActive(winPanel, false); }

    void ShowWinPanel()
    {
        SetActive(hudPanel, true); SetActive(pausePanel, false); SetActive(winPanel, true);
        if (winStatsText != null)
        {
            int mins = GameManager.Instance.gameTimeSeconds / 60;
            int secs = GameManager.Instance.gameTimeSeconds % 60;
            winStatsText.text = string.Format("Time: {0:00}:{1:00}\nTrash Collected: {2}\nItems Sorted: {3}\nCorrect Sorts: {4}\nWrong Sorts: {5}\nObjects Repaired: {6}\nWaves: {7}",
                mins, secs, GameManager.Instance.trashCollected, GameManager.Instance.itemsSorted,
                GameManager.Instance.correctSorts, GameManager.Instance.wrongSorts,
                GameManager.Instance.objectsRepaired, GameManager.Instance.wave);
        }
    }

    void SetActive(GameObject obj, bool active) { if (obj != null) obj.SetActive(active); }

    // ============ HUD UPDATE ============

    void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        if (progressBarFill != null)
            progressBarFill.fillAmount = GameManager.Instance.progress / GameManager.MaxProgress;

        if (progressText != null)
            progressText.text = "Restoration: " + Mathf.FloorToInt(GameManager.Instance.progress / GameManager.MaxProgress * 100f) + "%";

        if (inventoryText != null)
        {
            int count = GameManager.Instance.GetInventoryCount();
            string items = "";
            var inv = GameManager.Instance.GetInventory();
            foreach (var item in inv)
                items += (item.type == "garbage" ? "[G]" : "[R]");
            inventoryText.text = "Carrying: " + count + "/5 " + items;
        }

        if (messageText != null)
            messageText.text = GameManager.Instance.messageTimer > 0 ? GameManager.Instance.currentMessage : "";

        if (timerText != null)
        {
            int mins = GameManager.Instance.gameTimeSeconds / 60;
            int secs = GameManager.Instance.gameTimeSeconds % 60;
            timerText.text = string.Format("Time: {0:00}:{1:00}", mins, secs);
        }

        if (waveText != null)
            waveText.text = "Wave: " + GameManager.Instance.wave;

        if (trashCountText != null)
        {
            int remaining = 0;
            foreach (GameObject t in GameManager.Instance.trashObjects)
                if (t != null && t.activeSelf) remaining++;
            trashCountText.text = "Trash: " + remaining;
        }

        if (repairCountText != null)
        {
            int remaining = 0;
            foreach (GameObject r in GameManager.Instance.repairObjects)
            {
                ParkObject po = r.GetComponent<ParkObject>();
                if (po != null && !po.repaired) remaining++;
            }
            repairCountText.text = "Repairs: " + remaining;
        }
    }

    // ============ UI HELPERS ============

    GameObject MakePanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = bgColor;
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return panel;
    }

    Text MakeText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text txt = obj.AddComponent<Text>();
        txt.font = uiFont; txt.text = content; txt.fontSize = fontSize; txt.color = color;
        txt.alignment = anchor; txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.05f); rt.anchorMax = new Vector2(0.95f, 0.95f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return txt;
    }

    Button MakeButton(string name, Transform parent, string label, int fontSize, Color btnColor, UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = btnColor;
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = uiFont; txt.text = label; txt.fontSize = fontSize; txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        RectTransform txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250f, 50f);
        return btn;
    }

    // ============ BUILD SCREENS ============

    void BuildTitleScreen()
    {
        titleScreen = MakePanel("TitleScreen", canvas.transform, new Color(0.02f, 0.06f, 0.02f, 0.97f));

        Text title = MakeText("Title", titleScreen.transform, "RESTORE THE PARK", 42, new Color(0.3f, 0.85f, 0.3f));
        SetAnchors(title, 0.1f, 0.68f, 0.9f, 0.88f);

        Text sub = MakeText("Subtitle", titleScreen.transform, "A Community Park Cleanup Game", 18, new Color(0.5f, 0.75f, 0.5f));
        SetAnchors(sub, 0.1f, 0.58f, 0.9f, 0.68f);

        Text desc = MakeText("Desc", titleScreen.transform, "Clean trash, sort recyclables, repair objects,\nand watch the park come back to life!", 14, new Color(0.6f, 0.6f, 0.6f));
        SetAnchors(desc, 0.1f, 0.50f, 0.9f, 0.58f);

        Button playBtn = MakeButton("PlayBtn", titleScreen.transform, "PLAY", 22, new Color(0.18f, 0.55f, 0.22f), () => GameManager.Instance.StartGame());
        SetAnchors(playBtn.gameObject, 0.3f, 0.35f, 0.7f, 0.46f);

        Button howBtn = MakeButton("HowBtn", titleScreen.transform, "HOW TO PLAY", 22, new Color(0.18f, 0.38f, 0.55f), () => GameManager.Instance.SetState(GameManager.GameState.Instructions));
        SetAnchors(howBtn.gameObject, 0.3f, 0.22f, 0.7f, 0.33f);

        Button credBtn = MakeButton("CredBtn", titleScreen.transform, "CREDITS", 22, new Color(0.55f, 0.38f, 0.18f), () => GameManager.Instance.SetState(GameManager.GameState.Credits));
        SetAnchors(credBtn.gameObject, 0.3f, 0.09f, 0.7f, 0.20f);
    }

    void BuildInstructionsScreen()
    {
        instructionsScreen = MakePanel("InstructionsScreen", canvas.transform, new Color(0.02f, 0.06f, 0.02f, 0.97f));

        Text title = MakeText("Title", instructionsScreen.transform, "HOW TO PLAY", 34, new Color(0.3f, 0.85f, 0.3f));
        SetAnchors(title, 0.1f, 0.85f, 0.9f, 0.95f);

        string text = "WASD / Arrow Keys — Move your character\n\n" +
            "Walk over trash — Auto-pickup (carry up to 5 items)\n\n" +
            "RED bin — Sort GARBAGE items (walk near with items)\n" +
            "BLUE bin — Sort RECYCLABLE items (walk near with items)\n\n" +
            "E key — Repair broken objects / Talk to NPCs\n" +
            "P key — Pause the game\n" +
            "M key — Toggle sound on/off\n\n" +
            "Correct sort = +3 points | Wrong sort = -2 points\n\n" +
            "Goal: Reach 100% park restoration!\n" +
            "The park transforms from gray to green as you progress.";

        Text instText = MakeText("InstText", instructionsScreen.transform, text, 15, Color.white, TextAnchor.UpperLeft);
        SetAnchors(instText, 0.06f, 0.12f, 0.94f, 0.82f);

        Button backBtn = MakeButton("BackBtn", instructionsScreen.transform, "BACK", 20, new Color(0.55f, 0.28f, 0.18f), () => GameManager.Instance.SetState(GameManager.GameState.Title));
        SetAnchors(backBtn.gameObject, 0.35f, 0.02f, 0.65f, 0.10f);
    }

    void BuildCreditsScreen()
    {
        creditsScreen = MakePanel("CreditsScreen", canvas.transform, new Color(0.02f, 0.06f, 0.02f, 0.97f));

        Text title = MakeText("Title", creditsScreen.transform, "CREDITS", 34, new Color(0.3f, 0.85f, 0.3f));
        SetAnchors(title, 0.1f, 0.85f, 0.9f, 0.95f);

        string text = "RESTORE THE PARK\n\nDeveloper: Cosmas Mandikonza\n\nEngine: Unity 6 (2D)\n\n" +
            "Programming Language: C#\n\nAll visuals generated programmatically\n" +
            "using Unity SpriteRenderer.\n\nAll audio generated programmatically\n" +
            "using Unity AudioSource.\n\nNo external assets or libraries used.";

        Text credText = MakeText("CredText", creditsScreen.transform, text, 15, Color.white);
        SetAnchors(credText, 0.1f, 0.12f, 0.9f, 0.82f);

        Button backBtn = MakeButton("BackBtn", creditsScreen.transform, "BACK", 20, new Color(0.55f, 0.28f, 0.18f), () => GameManager.Instance.SetState(GameManager.GameState.Title));
        SetAnchors(backBtn.gameObject, 0.35f, 0.02f, 0.65f, 0.10f);
    }

    void BuildHUD()
    {
        hudPanel = new GameObject("HUD");
        hudPanel.transform.SetParent(canvas.transform, false);
        RectTransform hudRt = hudPanel.AddComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero; hudRt.offsetMax = Vector2.zero;

        // === TOP: Progress bar ===
        GameObject progBg = new GameObject("ProgBg");
        progBg.transform.SetParent(hudPanel.transform, false);
        Image progBgImg = progBg.AddComponent<Image>();
        progBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        SetAnchors(progBg, 0.1f, 0.94f, 0.9f, 0.98f);

        GameObject progFill = new GameObject("ProgFill");
        progFill.transform.SetParent(hudPanel.transform, false);
        progressBarFill = progFill.AddComponent<Image>();
        progressBarFill.color = new Color(0.2f, 0.7f, 0.2f);
        SetAnchors(progFill, 0.1f, 0.94f, 0.9f, 0.98f);

        GameObject progTxt = new GameObject("ProgText");
        progTxt.transform.SetParent(hudPanel.transform, false);
        progressText = progTxt.AddComponent<Text>();
        progressText.font = uiFont; progressText.text = "Restoration: 0%"; progressText.fontSize = 14; progressText.color = Color.white;
        progressText.alignment = TextAnchor.MiddleCenter;
        SetAnchors(progTxt, 0.1f, 0.94f, 0.9f, 0.98f);

        // === BOTTOM: Dark info bar (matching HTML5 version) ===
        GameObject bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(hudPanel.transform, false);
        Image barImg = bottomBar.AddComponent<Image>();
        barImg.color = new Color(0.05f, 0.1f, 0.25f, 0.9f);
        SetAnchors(bottomBar, 0f, 0f, 1f, 0.07f);

        // Inventory (bottom left)
        GameObject invTxt = new GameObject("InvText");
        invTxt.transform.SetParent(hudPanel.transform, false);
        inventoryText = invTxt.AddComponent<Text>();
        inventoryText.font = uiFont; inventoryText.text = "Carrying: 0/5"; inventoryText.fontSize = 12;
        inventoryText.color = new Color(0.7f, 0.9f, 0.7f); inventoryText.alignment = TextAnchor.MiddleLeft;
        SetAnchors(invTxt, 0.01f, 0.01f, 0.35f, 0.06f);

        // Timer + Wave (bottom center)
        GameObject timeTxt = new GameObject("TimerText");
        timeTxt.transform.SetParent(hudPanel.transform, false);
        timerText = timeTxt.AddComponent<Text>();
        timerText.font = uiFont; timerText.text = "Time: 00:00"; timerText.fontSize = 12;
        timerText.color = Color.white; timerText.alignment = TextAnchor.MiddleCenter;
        SetAnchors(timeTxt, 0.35f, 0.035f, 0.65f, 0.07f);

        GameObject wavTxt = new GameObject("WaveText");
        wavTxt.transform.SetParent(hudPanel.transform, false);
        waveText = wavTxt.AddComponent<Text>();
        waveText.font = uiFont; waveText.text = "Wave: 1"; waveText.fontSize = 12;
        waveText.color = new Color(0.9f, 0.9f, 0.3f); waveText.alignment = TextAnchor.MiddleCenter;
        SetAnchors(wavTxt, 0.35f, 0.005f, 0.65f, 0.04f);

        // Trash + Repairs count (bottom right)
        GameObject trashTxt = new GameObject("TrashText");
        trashTxt.transform.SetParent(hudPanel.transform, false);
        trashCountText = trashTxt.AddComponent<Text>();
        trashCountText.font = uiFont; trashCountText.text = "Trash: 39"; trashCountText.fontSize = 12;
        trashCountText.color = new Color(0.8f, 0.8f, 0.6f); trashCountText.alignment = TextAnchor.MiddleRight;
        SetAnchors(trashTxt, 0.65f, 0.035f, 0.99f, 0.07f);

        GameObject repTxt = new GameObject("RepairsText");
        repTxt.transform.SetParent(hudPanel.transform, false);
        repairCountText = repTxt.AddComponent<Text>();
        repairCountText.font = uiFont; repairCountText.text = "Repairs: 10"; repairCountText.fontSize = 12;
        repairCountText.color = new Color(0.8f, 0.8f, 0.6f); repairCountText.alignment = TextAnchor.MiddleRight;
        SetAnchors(repTxt, 0.65f, 0.005f, 0.99f, 0.04f);

        // Message text (above bottom bar)
        GameObject msgTxt = new GameObject("MessageText");
        msgTxt.transform.SetParent(hudPanel.transform, false);
        messageText = msgTxt.AddComponent<Text>();
        messageText.font = uiFont; messageText.text = ""; messageText.fontSize = 16;
        messageText.color = new Color(1f, 0.85f, 0f); messageText.alignment = TextAnchor.MiddleCenter;
        SetAnchors(msgTxt, 0.1f, 0.08f, 0.9f, 0.14f);

        // Controls hint
        GameObject hintTxt = new GameObject("HintText");
        hintTxt.transform.SetParent(hudPanel.transform, false);
        controlsText = hintTxt.AddComponent<Text>();
        controlsText.font = uiFont;
        controlsText.text = "WASD: Move | Walk over trash to pick up | Walk to bin to sort | E: Talk/Repair | P: Pause | M: Toggle sound";
        controlsText.fontSize = 10; controlsText.color = new Color(0.4f, 0.6f, 0.4f);
        controlsText.alignment = TextAnchor.MiddleCenter;
        SetAnchors(hintTxt, 0.05f, 0.0f, 0.95f, 0.025f);
    }

    void BuildPausePanel()
    {
        pausePanel = MakePanel("PausePanel", canvas.transform, new Color(0f, 0f, 0f, 0.75f));

        Text pauseText = MakeText("PauseText", pausePanel.transform, "PAUSED", 44, Color.white);
        SetAnchors(pauseText, 0.2f, 0.6f, 0.8f, 0.8f);

        Button resumeBtn = MakeButton("ResumeBtn", pausePanel.transform, "RESUME (P)", 20, new Color(0.18f, 0.55f, 0.22f), () => GameManager.Instance.SetState(GameManager.GameState.Playing));
        SetAnchors(resumeBtn.gameObject, 0.3f, 0.38f, 0.7f, 0.5f);

        Button menuBtn = MakeButton("MenuBtn", pausePanel.transform, "MENU (Q)", 20, new Color(0.55f, 0.28f, 0.18f), () => GameManager.Instance.SetState(GameManager.GameState.Title));
        SetAnchors(menuBtn.gameObject, 0.3f, 0.22f, 0.7f, 0.34f);

        pausePanel.SetActive(false);
    }

    void BuildWinPanel()
    {
        winPanel = MakePanel("WinPanel", canvas.transform, new Color(0f, 0.15f, 0f, 0.92f));

        Text winTitle = MakeText("WinTitle", winPanel.transform, "PARK RESTORED!", 40, new Color(0.3f, 1f, 0.3f));
        SetAnchors(winTitle, 0.1f, 0.8f, 0.9f, 0.95f);

        GameObject statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(winPanel.transform, false);
        winStatsText = statsObj.AddComponent<Text>();
        winStatsText.font = uiFont; winStatsText.text = ""; winStatsText.fontSize = 15;
        winStatsText.color = Color.white; winStatsText.alignment = TextAnchor.MiddleCenter;
        SetAnchors(winStatsText, 0.1f, 0.3f, 0.9f, 0.78f);

        Button playAgainBtn = MakeButton("PlayAgainBtn", winPanel.transform, "PLAY AGAIN", 20, new Color(0.18f, 0.55f, 0.22f), () => GameManager.Instance.StartGame());
        SetAnchors(playAgainBtn.gameObject, 0.3f, 0.12f, 0.7f, 0.26f);

        winPanel.SetActive(false);
    }

    void BuildNPCDialogue()
    {
        npcDialoguePanel = MakePanel("NPCDialogue", canvas.transform, new Color(0f, 0f, 0f, 0.85f));

        GameObject nameObj = new GameObject("NPCName");
        nameObj.transform.SetParent(npcDialoguePanel.transform, false);
        npcNameText = nameObj.AddComponent<Text>();
        npcNameText.font = uiFont; npcNameText.text = ""; npcNameText.fontSize = 16;
        npcNameText.color = new Color(1f, 0.85f, 0f); npcNameText.alignment = TextAnchor.MiddleCenter;
        SetAnchors(npcNameText, 0.05f, 0.72f, 0.95f, 0.95f);

        GameObject dlgObj = new GameObject("NPCDlgText");
        dlgObj.transform.SetParent(npcDialoguePanel.transform, false);
        npcDialogueTextUI = dlgObj.AddComponent<Text>();
        npcDialogueTextUI.font = uiFont; npcDialogueTextUI.text = ""; npcDialogueTextUI.fontSize = 14;
        npcDialogueTextUI.color = Color.white; npcDialogueTextUI.alignment = TextAnchor.MiddleCenter;
        SetAnchors(npcDialogueTextUI, 0.05f, 0.1f, 0.95f, 0.7f);

        RectTransform panelRt = npcDialoguePanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.15f, 0.08f); panelRt.anchorMax = new Vector2(0.85f, 0.25f);
        panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;

        npcDialoguePanel.SetActive(false);
    }

    // ============ HELPER ============

    void SetAnchors(GameObject obj, float minX, float minY, float maxX, float maxY)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null) { rt.anchorMin = new Vector2(minX, minY); rt.anchorMax = new Vector2(maxX, maxY); rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    }

    void SetAnchors(Text txt, float minX, float minY, float maxX, float maxY)
    {
        if (txt != null) SetAnchors(txt.gameObject, minX, minY, maxX, maxY);
    }
}
