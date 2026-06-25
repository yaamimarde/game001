using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// 世界场景启动时确保 UI 与示例交互对象存在。
/// </summary>
public class GameWorldBootstrap : MonoBehaviour
{
    [SerializeField] bool spawnDemoInteractables = true;

    void Start()
    {
        EnsureGameplayUI();
        if (spawnDemoInteractables)
            EnsureDemoInteractables();
    }

    void EnsureGameplayUI()
    {
        if (FindObjectOfType<GameplayUIController>() != null) return;

        var canvasGo = new GameObject("GameplayCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        canvasGo.AddComponent<GameplayUIController>();
        canvasGo.AddComponent<GameplayHUD>();
        canvasGo.AddComponent<PauseMenuUI>();
        canvasGo.AddComponent<InventoryUI>();
        canvasGo.AddComponent<TradeUI>();
        canvasGo.AddComponent<HireUI>();
        canvasGo.AddComponent<SettingsPanel>();

        CreateHudTexts(canvasGo.transform);
    }

    void CreateHudTexts(Transform parent)
    {
        var hpGo = new GameObject("HPText");
        hpGo.transform.SetParent(parent, false);
        var hpText = hpGo.AddComponent<TextMeshProUGUI>();
        hpText.fontSize = 18;
        hpText.color = Color.white;
        var hpRect = hpGo.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0, 1);
        hpRect.anchorMax = new Vector2(0, 1);
        hpRect.anchoredPosition = new Vector2(120, -30);
        hpRect.sizeDelta = new Vector2(200, 30);

        var goldGo = new GameObject("GoldText");
        goldGo.transform.SetParent(parent, false);
        var goldText = goldGo.AddComponent<TextMeshProUGUI>();
        goldText.fontSize = 18;
        goldText.color = Color.yellow;
        var goldRect = goldGo.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0, 1);
        goldRect.anchorMax = new Vector2(0, 1);
        goldRect.anchoredPosition = new Vector2(120, -60);
        goldRect.sizeDelta = new Vector2(200, 30);

        var statsGo = new GameObject("StatsText");
        statsGo.transform.SetParent(parent, false);
        var statsText = statsGo.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 14;
        statsText.color = Color.white;
        var statsRect = statsGo.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 1);
        statsRect.anchorMax = new Vector2(0, 1);
        statsRect.anchoredPosition = new Vector2(120, -90);
        statsRect.sizeDelta = new Vector2(300, 30);

        var hud = parent.GetComponent<GameplayHUD>();
        if (hud != null)
        {
            SetField(hud, "hpText", hpText);
            SetField(hud, "goldText", goldText);
            SetField(hud, "statsText", statsText);
        }
    }

    void EnsureDemoInteractables()
    {
        if (FindObjectOfType<ShopInteractable>() != null) return;

        var player = FindObjectOfType<WarriorPlayer>();
        Vector3 basePos = player != null ? player.transform.position : Vector3.zero;

        CreatePickup(basePos + new Vector3(2f, 0, 0), DefaultGameContent.GetItemDatabase().items[1]);
        CreateShop(basePos + new Vector3(-3f, 0, 0), DefaultGameContent.CreateGeneralStore());
        CreateRecruitNpc(basePos + new Vector3(0, 2f, 0), DefaultGameContent.GetRecruitDatabase().templates[0]);
    }

    void CreatePickup(Vector3 pos, ItemDefinition item)
    {
        var go = new GameObject("ItemPickup_" + item.itemId);
        go.transform.position = pos;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1f;
        go.AddComponent<SpriteRenderer>().color = Color.green;
        var pickup = go.AddComponent<ItemPickup>();
        SetField(pickup, "item", item);
    }

    void CreateShop(Vector3 pos, ShopDefinition shop)
    {
        var go = new GameObject("Shop_" + shop.shopId);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 2f);
        go.AddComponent<SpriteRenderer>().color = new Color(1f, 0.8f, 0.2f);
        var interact = go.AddComponent<ShopInteractable>();
        SetField(interact, "shop", shop);
    }

    void CreateRecruitNpc(Vector3 pos, RecruitTemplate template)
    {
        if (template == null) return;
        var go = new GameObject("Recruit_" + template.templateId);
        go.transform.position = pos;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.5f;
        go.AddComponent<SpriteRenderer>().color = new Color(0.3f, 0.6f, 1f);
        var recruit = go.AddComponent<RecruitableNPC>();
        SetField(recruit, "template", template);
    }

    static void SetField(Object target, string field, Object value)
    {
        var fieldInfo = target.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        fieldInfo?.SetValue(target, value);
    }
}
