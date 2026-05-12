using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Player movement, inventory, and interaction logic.
/// Works with Unity's Input System Package (the new input system).
/// Uses UnityEngine.InputSystem for all input handling.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [System.Serializable]
    public class TrashInfo
    {
        public string type;  // "garbage" or "recyclable"
        public string name;
        public TrashInfo(string t, string n) { type = t; name = n; }
    }

    public float speed = 5f;
    public int maxCarry = 5;
    public List<TrashInfo> inventory = new List<TrashInfo>();

    private Rigidbody2D rb;
    private Vector2 lastDir = Vector2.up;
    private float footstepTimer = 0f;

    // Input System references
    private UnityEngine.InputSystem.Keyboard keyboard;
    private bool inputSystemAvailable = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // Try to get Input System keyboard
        TryInitInputSystem();
    }

    private void TryInitInputSystem()
    {
        try
        {
            keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                inputSystemAvailable = true;
                Debug.Log("PlayerController: Using Input System Package (Keyboard)");
            }
            else
            {
                Debug.LogWarning("PlayerController: Keyboard.current is null. Will retry on first update.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("PlayerController: Input System not available: " + e.Message);
            inputSystemAvailable = false;
        }
    }

    private bool IsKeyPressed(UnityEngine.InputSystem.Key key)
    {
        if (!inputSystemAvailable || keyboard == null)
        {
            // Retry getting keyboard
            try
            {
                keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null) inputSystemAvailable = true;
                else return false;
            }
            catch { return false; }
        }
        return keyboard[key].isPressed;
    }

    private bool WasKeyPressedThisFrame(UnityEngine.InputSystem.Key key)
    {
        if (!inputSystemAvailable || keyboard == null)
        {
            try
            {
                keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null) inputSystemAvailable = true;
                else return false;
            }
            catch { return false; }
        }
        return keyboard[key].wasPressedThisFrame;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // ---- MOVEMENT ----
        float dx = 0f, dy = 0f;
        if (IsKeyPressed(UnityEngine.InputSystem.Key.W) || IsKeyPressed(UnityEngine.InputSystem.Key.UpArrow)) dy = 1f;
        if (IsKeyPressed(UnityEngine.InputSystem.Key.S) || IsKeyPressed(UnityEngine.InputSystem.Key.DownArrow)) dy = -1f;
        if (IsKeyPressed(UnityEngine.InputSystem.Key.A) || IsKeyPressed(UnityEngine.InputSystem.Key.LeftArrow)) dx = -1f;
        if (IsKeyPressed(UnityEngine.InputSystem.Key.D) || IsKeyPressed(UnityEngine.InputSystem.Key.RightArrow)) dx = 1f;

        if (dx != 0 || dy != 0)
        {
            Vector2 dir = new Vector2(dx, dy).normalized;
            lastDir = dir;
            rb.linearVelocity = dir * speed;

            // Footstep sounds
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= 0.23f)
            {
                footstepTimer = 0f;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep();
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            footstepTimer = 0f;
        }

        // Boundary clamping
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, 0.5f, 15.5f);
        pos.y = Mathf.Clamp(pos.y, 0.3f, 9.5f);
        transform.position = pos;

        // ---- INTERACTIONS ----
        CheckTrashPickup();
        CheckBinDeposit();
        CheckRepair();
        CheckNPC();

        // ---- KEY SHORTCUTS ----
        if (WasKeyPressedThisFrame(UnityEngine.InputSystem.Key.P))
        {
            if (GameManager.Instance.currentState == GameManager.GameState.Playing)
                GameManager.Instance.SetState(GameManager.GameState.Paused);
        }

        if (WasKeyPressedThisFrame(UnityEngine.InputSystem.Key.Q) && GameManager.Instance.currentState == GameManager.GameState.Paused)
        {
            GameManager.Instance.SetState(GameManager.GameState.Title);
        }

        if (WasKeyPressedThisFrame(UnityEngine.InputSystem.Key.M))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }
    }

    // ---- TRASH PICKUP (auto on proximity) ----
    void CheckTrashPickup()
    {
        foreach (GameObject trashObj in GameManager.Instance.trashObjects)
        {
            if (trashObj == null || !trashObj.activeSelf) continue;
            float dist = Vector2.Distance(transform.position, trashObj.transform.position);
            if (dist < 0.55f && inventory.Count < maxCarry)
            {
                ParkObject po = trashObj.GetComponent<ParkObject>();
                if (po == null || po.collected) continue;

                po.collected = true;
                inventory.Add(new TrashInfo(po.dataType, po.objectName));
                trashObj.SetActive(false);
                GameManager.Instance.trashCollected++;

                if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
                GameManager.Instance.SpawnParticles(trashObj.transform.position, po.itemColor);
                GameManager.Instance.ShowFloatingText(trashObj.transform.position, "+1 " + po.objectName, Color.white);

                if (inventory.Count >= maxCarry)
                    GameManager.Instance.ShowMessage("Inventory full! Visit a bin to sort items.", 2f);
            }
        }
    }

    // ---- BIN SORTING (auto on proximity with inventory) ----
    void CheckBinDeposit()
    {
        foreach (GameObject binObj in GameManager.Instance.binObjects)
        {
            float dist = Vector2.Distance(transform.position, binObj.transform.position);
            if (dist < 0.8f && inventory.Count > 0)
            {
                ParkObject po = binObj.GetComponent<ParkObject>();
                int gained = 0, lost = 0;

                foreach (var item in inventory)
                {
                    if (item.type == po.dataType) { gained += 3; GameManager.Instance.correctSorts++; }
                    else { lost += 2; GameManager.Instance.wrongSorts++; }
                    GameManager.Instance.itemsSorted++;
                }

                int net = gained - lost;
                GameManager.Instance.AddProgress(net);
                inventory.Clear();

                if (net > 0)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayCorrectSort();
                    GameManager.Instance.SpawnParticles(binObj.transform.position, new Color(0.56f, 0.93f, 0.56f));
                    GameManager.Instance.ShowFloatingText(binObj.transform.position, "+" + gained + " pts!", new Color(0.56f, 0.93f, 0.56f));
                }
                else
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayWrongSort();
                    GameManager.Instance.SpawnParticles(binObj.transform.position, new Color(1f, 0.42f, 0.42f));
                    GameManager.Instance.ShowFloatingText(binObj.transform.position, "Wrong bin! -" + lost, new Color(1f, 0.42f, 0.42f));
                }
            }
        }
    }

    // ---- REPAIR (press E near) ----
    void CheckRepair()
    {
        foreach (GameObject repObj in GameManager.Instance.repairObjects)
        {
            ParkObject po = repObj.GetComponent<ParkObject>();
            if (po == null || po.repaired) continue;

            float dist = Vector2.Distance(transform.position, repObj.transform.position);
            if (dist < 0.7f)
            {
                GameManager.Instance.ShowMessage("Press E to repair " + po.objectName, 0.15f);
                if (WasKeyPressedThisFrame(UnityEngine.InputSystem.Key.E))
                {
                    po.Repair();
                    GameManager.Instance.objectsRepaired++;
                    GameManager.Instance.AddProgress(po.progressValue);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayRepair();
                    GameManager.Instance.SpawnParticles(repObj.transform.position, po.repairedColor);
                    GameManager.Instance.ShowFloatingText(repObj.transform.position, po.objectName + " fixed! +" + po.progressValue, new Color(0.56f, 0.93f, 0.56f));
                }
            }
        }
    }

    // ---- NPC DIALOGUE (press E near) ----
    void CheckNPC()
    {
        foreach (GameObject npcObj in GameManager.Instance.npcObjects)
        {
            float dist = Vector2.Distance(transform.position, npcObj.transform.position);
            if (dist < 0.9f)
            {
                ParkObject po = npcObj.GetComponent<ParkObject>();
                GameManager.Instance.ShowMessage("Press E to talk to " + po.objectName, 0.15f);
                if (WasKeyPressedThisFrame(UnityEngine.InputSystem.Key.E))
                {
                    string line = po.AdvanceDialogue();
                    GameManager.Instance.ShowNPCDialogue(po.objectName, line);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayNPC();
                }
            }
        }
    }
}
