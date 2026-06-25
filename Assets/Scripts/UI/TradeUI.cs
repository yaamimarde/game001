using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TradeUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] Transform shopListRoot;
    [SerializeField] Transform playerListRoot;
    [SerializeField] GameObject rowPrefab;
    [SerializeField] TMP_Text shopTitle;
    [SerializeField] TMP_Text goldText;
    [SerializeField] Button closeButton;

    ShopDefinition currentShop;
    TradeSystem trade;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        GameEventBus.OnGoldChanged += RefreshGold;
        GameEventBus.OnTradeCompleted += Refresh;
    }

    void OnDisable()
    {
        GameEventBus.OnGoldChanged -= RefreshGold;
        GameEventBus.OnTradeCompleted -= Refresh;
    }

    public void Open(ShopDefinition shop)
    {
        if (GameManager.Instance == null || !GameManager.Instance.Session.IsActive) return;

        currentShop = shop;
        trade = GameManager.Instance.Session.Trade;

        if (shopListRoot == null || playerListRoot == null)
            BuildTradePanelUI();

        if (panelRoot != null) panelRoot.SetActive(true);
        else gameObject.SetActive(true);

        if (shopTitle != null) shopTitle.text = shop != null ? shop.shopName : "商店";
        GameManager.Instance.SetPaused(true);
        Refresh();
    }

    void BuildTradePanelUI()
    {
        var root = panelRoot != null ? panelRoot.transform : transform;
        shopTitle = CreateLabel(root, "商店", new Vector2(0, 150));
        goldText = CreateLabel(root, "金币: 0", new Vector2(0, 120));

        var shopGo = new GameObject("ShopList");
        shopGo.transform.SetParent(root, false);
        shopListRoot = shopGo.transform;
        var shopLayout = shopGo.AddComponent<VerticalLayoutGroup>();

        var playerGo = new GameObject("PlayerList");
        playerGo.transform.SetParent(root, false);
        playerListRoot = playerGo.transform;
        playerGo.AddComponent<VerticalLayoutGroup>();

        rowPrefab = CreateRowPrefab();
    }

    static GameObject CreateRowPrefab()
    {
        var row = new GameObject("TradeRowTemplate");
        row.SetActive(false);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(row.transform, false);
        nameGo.AddComponent<TextMeshProUGUI>().fontSize = 16;
        var priceGo = new GameObject("Price");
        priceGo.transform.SetParent(row.transform, false);
        priceGo.AddComponent<TextMeshProUGUI>().fontSize = 16;
        var btnGo = new GameObject("Button");
        btnGo.transform.SetParent(row.transform, false);
        btnGo.AddComponent<Image>().color = Color.gray;
        btnGo.AddComponent<Button>();
        return row;
    }

    static TMP_Text CreateLabel(Transform parent, string text, Vector2 pos)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 30);
        return tmp;
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        GameManager.Instance?.SetPaused(false);
    }

    void Refresh()
    {
        RefreshGold();
        RefreshShopList();
        RefreshPlayerList();
    }

    void RefreshGold()
    {
        if (goldText != null && GameManager.Instance != null)
            goldText.text = $"金币: {GameManager.Instance.Session.Gold}";
    }

    void RefreshShopList()
    {
        if (shopListRoot == null || currentShop == null) return;
        Clear(shopListRoot);

        foreach (var entry in currentShop.stock)
        {
            if (entry.item == null) continue;
            var row = CreateRow(shopListRoot, entry.item.displayName,
                trade.GetBuyPrice(entry.item, currentShop).ToString(), () =>
                {
                    trade.TryBuyFromShop(entry.item, currentShop);
                });
        }
    }

    void RefreshPlayerList()
    {
        if (playerListRoot == null || GameManager.Instance == null) return;
        Clear(playerListRoot);

        foreach (var item in GameManager.Instance.Session.Inventory.Items)
        {
            if (item?.Definition == null) continue;
            var captured = item;
            var row = CreateRow(playerListRoot, item.Definition.displayName,
                trade.GetSellPrice(item.Definition, currentShop).ToString(), () =>
                {
                    trade.TrySellToShop(captured, currentShop);
                });
        }
    }

    GameObject CreateRow(Transform parent, string name, string price, UnityEngine.Events.UnityAction onClick)
    {
        var go = Instantiate(rowPrefab, parent);
        go.SetActive(true);
        var texts = go.GetComponentsInChildren<TMP_Text>();
        if (texts.Length > 0) texts[0].text = name;
        if (texts.Length > 1) texts[1].text = price;
        var btn = go.GetComponentInChildren<Button>();
        if (btn != null) btn.onClick.AddListener(onClick);
        return go;
    }

    static void Clear(Transform root)
    {
        foreach (Transform child in root)
            Destroy(child.gameObject);
    }
}
