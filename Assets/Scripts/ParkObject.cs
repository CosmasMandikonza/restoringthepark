using UnityEngine;

/// <summary>
/// Component attached to all park objects: trash, bins, repair objects, NPCs.
/// Stores type data, state, and provides helper methods.
/// NO TextMesh references — uses sprite-based indicators instead.
/// </summary>
public class ParkObject : MonoBehaviour
{
    public enum ObjectType { Trash, Bin, Repair, NPC }

    [Header("Type")]
    public ObjectType objectType;
    public string dataType = "";       // "garbage" or "recyclable" for trash/bins
    public string objectName = "";     // Display name

    [Header("Trash")]
    public bool collected = false;
    public Color itemColor = Color.white;
    public Vector2 basePos;            // Original spawn position
    public float bobOffset = 0f;       // Animation offset

    [Header("Repair")]
    public bool repaired = false;
    public Color brokenColor = Color.gray;
    public Color repairedColor = Color.green;
    public int progressValue = 3;
    public float repairAnimTimer = 0f;
    public Vector3 baseScale = Vector3.one;

    [Header("NPC")]
    public string[] dialogues;
    public int currentDialogue = 0;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;

    /// <summary>Repair this object and update visuals using sprite indicators.</summary>
    public void Repair()
    {
        repaired = true;
        repairAnimTimer = 1f;
        if (spriteRenderer != null)
            spriteRenderer.color = repairedColor;

        // Swap X indicator to checkmark indicator
        Transform statusX = transform.Find("StatusX");
        if (statusX != null) statusX.gameObject.SetActive(false);

        Transform statusCheck = transform.Find("StatusCheck");
        if (statusCheck != null) statusCheck.gameObject.SetActive(true);

        // Show glow
        Transform glowT = transform.Find("Glow");
        if (glowT != null) glowT.gameObject.SetActive(true);
    }

    /// <summary>Advance NPC dialogue and return the current line.</summary>
    public string AdvanceDialogue()
    {
        if (dialogues == null || dialogues.Length == 0) return "...";
        string line = dialogues[currentDialogue];
        currentDialogue = (currentDialogue + 1) % dialogues.Length;
        return line;
    }

    /// <summary>Respawn a collected trash item at a new position.</summary>
    public void Respawn(Vector3 newPos)
    {
        collected = false;
        gameObject.SetActive(true);
        transform.position = newPos;
        basePos = newPos;
        if (spriteRenderer != null)
            spriteRenderer.color = itemColor;
    }

    /// <summary>Revive a trash item (for wave respawn).</summary>
    public void Revive(Vector3 newPos)
    {
        collected = false;
        gameObject.SetActive(true);
        transform.position = newPos;
        basePos = newPos;
        if (spriteRenderer != null)
            spriteRenderer.color = itemColor;
    }
}
