using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour{
    [SerializeField] ItemDefinition item;
    [SerializeField] int stackCount = 1;
    [SerializeField] KeyCode pickupKey = KeyCode.E;

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
        if (!playerInRange || item == null) return;
        if (!Input.GetKeyDown(pickupKey)) return;
        if (GameManager.Instance == null || !GameManager.Instance.Session.IsActive) return;

        if (GameManager.Instance.Session.Inventory.TryAdd(item, stackCount))
            Destroy(gameObject);
    }
}
