using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RecruitableNPC : MonoBehaviour{
    [SerializeField] RecruitTemplate template;
    [SerializeField] HireUI hireUI;
    [SerializeField] KeyCode interactKey = KeyCode.E;

    bool playerInRange;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<WarriorPlayer>() != null)
            playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<WarriorPlayer>() != null)
            playerInRange = false;
    }

    void Update()
    {
        if (!playerInRange || template == null) return;
        if (!Input.GetKeyDown(interactKey)) return;

        if (hireUI == null)
            hireUI = FindObjectOfType<HireUI>();

        hireUI?.OpenRecruit(template);
    }
}
