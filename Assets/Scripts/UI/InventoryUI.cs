using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 网格背包 UI：显示物品、旋转、装备、丢弃。
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] Transform gridRoot;
    [SerializeField] GameObject cellPrefab;
    [SerializeField] GameObject itemViewPrefab;
    [SerializeField] TMP_Text weightText;
    [SerializeField] Button closeButton;
    [SerializeField] Button rotateButton;

    InventoryItemInstance selectedItem;
    GridInventory inventory;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (rotateButton != null) rotateButton.onClick.AddListener(RotateSelected);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (panelRoot != null && panelRoot.activeSelf) Hide();
            else Show();
        }
    }

    void OnEnable() => GameEventBus.OnInventoryChanged += Refresh;
    void OnDisable() => GameEventBus.OnInventoryChanged -= Refresh;

    public void Show()
    {
        if (GameManager.Instance == null || !GameManager.Instance.Session.IsActive) return;
        inventory = GameManager.Instance.Session.Inventory;

        if (gridRoot == null)
        {
            var panel = panelRoot != null ? panelRoot : gameObject;
            var gridGo = new GameObject("GridRoot");
            gridGo.transform.SetParent(panel.transform, false);
            var layout = gridGo.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(42, 42);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = inventory.Width;
            gridRoot = gridGo.transform;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        selectedItem = null;
    }

    void Refresh()
    {
        if (inventory == null && GameManager.Instance != null)
            inventory = GameManager.Instance.Session.Inventory;
        if (inventory == null || gridRoot == null) return;

        foreach (Transform child in gridRoot)
            Destroy(child.gameObject);

        for (int y = 0; y < inventory.Height; y++)
        {
            for (int x = 0; x < inventory.Width; x++)
            {
                var item = inventory.GetAt(x, y);
                bool isOrigin = item != null && item.GridX == x && item.GridY == y;
                if (!isOrigin && item != null) continue;

                CreateCell(x, y, isOrigin ? item : null);
            }
        }

        if (weightText != null)
            weightText.text = $"重量 {inventory.CurrentWeight:F1}/{inventory.MaxWeight:F1}";
    }

    void CreateCell(int x, int y, InventoryItemInstance item)
    {
        GameObject cellGo;
        if (cellPrefab != null)
        {
            cellGo = Instantiate(cellPrefab, gridRoot);
        }
        else
        {
            cellGo = new GameObject($"Cell_{x}_{y}");
            cellGo.transform.SetParent(gridRoot, false);
            var img = cellGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var btn = cellGo.AddComponent<Button>();
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(cellGo.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 10;
            label.alignment = TextAlignmentOptions.Center;
            var rect = cellGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 40);
            var cell = cellGo.AddComponent<InventoryCellUI>();
            int cx = x, cy = y;
            btn.onClick.AddListener(() => OnCellClicked(cx, cy, item));
            if (item != null && item.Definition != null)
                label.text = item.Definition.displayName;
            return;
        }

        var cellUi = cellGo.GetComponent<InventoryCellUI>();
        if (cellUi != null)
            cellUi.Setup(x, y, item, OnCellClicked);
    }

    void OnCellClicked(int x, int y, InventoryItemInstance item)
    {
        if (item == null) return;
        selectedItem = item;

        if (Input.GetMouseButtonDown(1))
            ShowContextMenu(item);
    }

    void RotateSelected()
    {
        if (selectedItem == null || inventory == null) return;
        inventory.Rotate(selectedItem);
        GameManager.Instance?.ApplyPlayerStats();
    }

    void ShowContextMenu(InventoryItemInstance item)
    {
        if (item.Definition.equipSlot != EquipSlot.None)
        {
            GameManager.Instance.Session.Equipment.TryEquip(item, inventory);
            GameManager.Instance.ApplyPlayerStats();
        }
        else if (item.Definition.itemType == ItemType.Consumable)
        {
            inventory.Remove(item);
        }
        Refresh();
    }
}

public class InventoryCellUI : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text label;

    public void Setup(int x, int y, InventoryItemInstance item, System.Action<int, int, InventoryItemInstance> onClick)
    {
        if (button != null)
            button.onClick.AddListener(() => onClick?.Invoke(x, y, item));

        if (item != null && item.Definition != null)
        {
            if (icon != null)
            {
                icon.enabled = item.Definition.icon != null;
                icon.sprite = item.Definition.icon;
            }
            if (label != null)
                label.text = item.Definition.displayName;
        }
        else
        {
            if (icon != null) icon.enabled = false;
            if (label != null) label.text = "";
        }
    }
}
