using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShopInteractable : MonoBehaviour{
    [SerializeField] ShopDefinition shop;
    [SerializeField] TradeUI tradeUI;
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
        if (!playerInRange || shop == null) return;
        if (!Input.GetKeyDown(interactKey)) return;

        if (tradeUI == null)
            tradeUI = FindObjectOfType<TradeUI>();

        tradeUI?.Open(shop);
    }
}
