#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class GameSetupAutoLoad
{
    static GameSetupAutoLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!File.Exists("Assets/Scenes/MainMenu.unity"))
            {
                GameSetupEditor.CreateResourceAssets();
                GameSetupEditor.CreateMainMenuScene();
                GameSetupEditor.SetupBuildSettings();
            }
        };
    }
}

public static class GameSetupEditor
{
    const string MenuRoot = "Game/Setup/";
    static DefaultControls.Resources s_StandardResources;
    static TMP_FontAsset s_UIFont;

    [MenuItem(MenuRoot + "Create All Game Assets And Scenes")]
    public static void CreateAll()
    {
        CreateResourceAssets();
        CreateMainMenuScene();
        SetupBuildSettings();
        AddGameplaySystemsToMainScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("游戏架构资源与场景已创建。请将 MainMenu 设为启动场景后运行。");
    }

    [MenuItem(MenuRoot + "Create Resource Assets Only")]
    public static void CreateResourceAssets()
    {
        EnsureFolder("Assets/Resources/Game");

        CreateItemDatabase();
        CreateRecruitDatabase();
        CreateStatConfig();
        CreateSampleItems();
        CreateSampleShop();
        CreateSampleRecruitTemplate();
    }

    static void CreateItemDatabase()
    {
        string path = "Assets/Resources/Game/ItemDatabase.asset";
        if (AssetDatabase.LoadAssetAtPath<ItemDatabase>(path) != null) return;

        var db = ScriptableObject.CreateInstance<ItemDatabase>();
        AssetDatabase.CreateAsset(db, path);
    }

    static void CreateRecruitDatabase()
    {
        string path = "Assets/Resources/Game/RecruitDatabase.asset";
        if (AssetDatabase.LoadAssetAtPath<RecruitDatabase>(path) != null) return;

        var db = ScriptableObject.CreateInstance<RecruitDatabase>();
        AssetDatabase.CreateAsset(db, path);
    }

    static void CreateStatConfig()
    {
        string path = "Assets/Resources/Game/StatProgressionConfig.asset";
        if (AssetDatabase.LoadAssetAtPath<StatProgressionConfig>(path) != null) return;

        var cfg = ScriptableObject.CreateInstance<StatProgressionConfig>();
        AssetDatabase.CreateAsset(cfg, path);
    }

    static void CreateSampleItems()
    {
        var sword = CreateItem("Assets/Resources/Game/Items/sword_iron.asset", "sword_iron", "铁剑", 2, 1, ItemType.Weapon, EquipSlot.Weapon, 5, 0, 50, 25);
        var potion = CreateItem("Assets/Resources/Game/Items/potion_hp.asset", "potion_hp", "生命药水", 1, 1, ItemType.Consumable, EquipSlot.None, 0, 0, 20, 10);
        var ore = CreateItem("Assets/Resources/Game/Items/ore_copper.asset", "ore_copper", "铜矿", 1, 1, ItemType.Material, EquipSlot.None, 0, 0, 15, 8);

        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/Game/ItemDatabase.asset");
        if (db != null)
        {
            db.items.Clear();
            db.items.Add(sword);
            db.items.Add(potion);
            db.items.Add(ore);
            EditorUtility.SetDirty(db);
        }
    }

    static ItemDefinition CreateItem(string path, string id, string name, int gw, int gh,
        ItemType type, EquipSlot slot, int dmg, int def, int buy, int sell)
    {
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
        if (existing != null) return existing;

        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.itemId = id;
        item.displayName = name;
        item.gridWidth = gw;
        item.gridHeight = gh;
        item.itemType = type;
        item.equipSlot = slot;
        item.damageBonus = dmg;
        item.defenseBonus = def;
        item.buyPrice = buy;
        item.sellPrice = sell;
        item.weight = 1f;
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    static void CreateSampleShop()
    {
        string path = "Assets/Resources/Game/Shops/general_store.asset";
        EnsureFolder("Assets/Resources/Game/Shops");
        if (AssetDatabase.LoadAssetAtPath<ShopDefinition>(path) != null) return;

        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/Game/ItemDatabase.asset");
        var shop = ScriptableObject.CreateInstance<ShopDefinition>();
        shop.shopId = "general_store";
        shop.shopName = "杂货铺";
        shop.townPriceModifier = 1f;
        shop.buybackRate = 0.5f;
        shop.buysCategories.Add(ItemType.Material);
        shop.buysCategories.Add(ItemType.Consumable);

        if (db != null && db.items.Count > 0)
        {
            foreach (var item in db.items)
            {
                shop.stock.Add(new ShopStockEntry { item = item, stock = 99, priceMultiplier = 1f });
            }
        }

        AssetDatabase.CreateAsset(shop, path);
    }

    static void CreateSampleRecruitTemplate()
    {
        string path = "Assets/Resources/Game/Recruits/guard_recruit.asset";
        EnsureFolder("Assets/Resources/Game/Recruits");
        if (AssetDatabase.LoadAssetAtPath<RecruitTemplate>(path) != null) return;

        var template = ScriptableObject.CreateInstance<RecruitTemplate>();
        template.templateId = "guard_recruit";
        template.displayName = "流浪卫兵";
        template.hireCost = 200;
        template.dailyWage = 20;
        template.baseStats = StatBlock.CreateDefault();

        var prefabPath = "Assets/Prefabs/CompanionGuard.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            var go = new GameObject("CompanionGuard");
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>();
            go.AddComponent<SpriteRenderer>().color = new Color(0.3f, 0.6f, 1f);
            go.AddComponent<CompanionCharacter>();
            go.AddComponent<FriendlyMoveAI>();
            prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }
        template.prefab = prefab;

        AssetDatabase.CreateAsset(template, path);

        var db = AssetDatabase.LoadAssetAtPath<RecruitDatabase>("Assets/Resources/Game/RecruitDatabase.asset");
        if (db != null)
        {
            db.templates.Clear();
            db.templates.Add(template);
            EditorUtility.SetDirty(db);
        }
    }

    [MenuItem(MenuRoot + "Create MainMenu Scene")]
    [MenuItem(MenuRoot + "Rebuild Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        try
        {
            CreateMainMenuSceneInternal();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CreateMainMenuScene 失败: {ex.Message}\n{ex.StackTrace}");
        }
    }

    static void CreateMainMenuSceneInternal()
    {
        const string scenePath = "Assets/Scenes/MainMenu.unity";
        EnsureFolder("Assets/Scenes");
        s_UIFont = ChineseUIFontBuilder.EnsureChineseUIFont();
        if (s_UIFont == null)
        {
            Debug.LogError("无法创建中文字体资产，请先确保 Assets/Fonts/NotoSansSC-Regular.ttf 存在。");
            return;
        }

        if (File.Exists(scenePath))
            AssetDatabase.DeleteAsset(scenePath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var bootstrap = new GameObject("GameBootstrap");
        bootstrap.AddComponent<GameBootstrap>();

        var canvasGo = CreateCanvas("MainMenuCanvas");
        canvasGo.AddComponent<UIFontBootstrap>();
        var mainMenuUI = canvasGo.AddComponent<MainMenuUI>();

        var mainButtons = new GameObject("MainButtons");
        mainButtons.transform.SetParent(canvasGo.transform, false);
        var layout = mainButtons.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        mainButtons.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var layoutRect = mainButtons.GetComponent<RectTransform>();
        layoutRect.anchorMin = new Vector2(0.5f, 0.5f);
        layoutRect.anchorMax = new Vector2(0.5f, 0.5f);
        layoutRect.pivot = new Vector2(0.5f, 0.5f);
        layoutRect.sizeDelta = new Vector2(300, 400);

        var startBtn = CreateButton(mainButtons.transform, "开始游戏");
        var continueBtn = CreateButton(mainButtons.transform, "继续游戏");
        var loadBtn = CreateButton(mainButtons.transform, "读档");
        var settingsBtn = CreateButton(mainButtons.transform, "设置");
        var quitBtn = CreateButton(mainButtons.transform, "退出");

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(canvasGo.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(500, 60);
        titleRect.anchoredPosition = new Vector2(0, 200);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.font = GetUIFont();
        title.text = "游戏标题";
        title.fontSize = 36;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;

        var savePanelGo = new GameObject("SaveSlotPanel");
        savePanelGo.transform.SetParent(canvasGo.transform, false);
        var savePanelBg = savePanelGo.AddComponent<Image>();
        savePanelBg.color = new Color(0, 0, 0, 0.8f);
        var savePanel = savePanelGo.AddComponent<SaveSlotPanel>();
        var savePanelRect = savePanelGo.GetComponent<RectTransform>();
        savePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        savePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        savePanelRect.pivot = new Vector2(0.5f, 0.5f);
        savePanelRect.sizeDelta = new Vector2(500, 480);

        var content = new GameObject("Content");
        content.transform.SetParent(savePanelGo.transform, false);
        StretchRect(content.AddComponent<RectTransform>(), new Vector2(20, 20), new Vector2(-20, -20));
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 10;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childAlignment = TextAnchor.UpperCenter;

        var panelTitle = CreateLayoutText(content.transform, "新游戏 · 选择存档槽位", 22, 32);
        panelTitle.gameObject.name = "PanelTitle";
        panelTitle.alignment = TextAlignmentOptions.Center;
        var panelHint = CreateLayoutText(content.transform, "点击空槽位开始游戏，已有存档将被覆盖", 14, 22);
        panelHint.gameObject.name = "PanelHint";
        panelHint.alignment = TextAlignmentOptions.Center;
        panelHint.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        var slotContainer = new GameObject("Slots");
        slotContainer.transform.SetParent(content.transform, false);
        var slotContainerLayout = slotContainer.AddComponent<LayoutElement>();
        slotContainerLayout.flexibleHeight = 1;
        slotContainerLayout.minHeight = 200;
        var slotLayout = slotContainer.AddComponent<VerticalLayoutGroup>();
        slotLayout.spacing = 8;
        slotLayout.childControlHeight = true;
        slotLayout.childForceExpandHeight = false;

        var slotButtons = new SaveSlotButton[3];
        for (int i = 0; i < 3; i++)
        {
            var slotGo = new GameObject($"Slot{i}");
            slotGo.transform.SetParent(slotContainer.transform, false);
            var slotLayoutElement = slotGo.AddComponent<LayoutElement>();
            slotLayoutElement.preferredHeight = 64;
            var slotInnerLayout = slotGo.AddComponent<VerticalLayoutGroup>();
            slotInnerLayout.spacing = 2;
            slotInnerLayout.childControlHeight = true;
            slotInnerLayout.childForceExpandHeight = false;
            slotButtons[i] = slotGo.AddComponent<SaveSlotButton>();
            var btn = CreateButton(slotGo.transform, $"槽位 {i + 1}");
            var btnLayout = btn.GetComponent<LayoutElement>();
            btnLayout.preferredHeight = 40;
            btnLayout.preferredWidth = -1;
            var titleText = btn.GetComponentInChildren<TMP_Text>();
            var summary = CreateLayoutText(slotGo.transform, "空", 14, 18);
            summary.alignment = TextAlignmentOptions.MidlineLeft;
            SetSerializedField(slotButtons[i], "button", btn);
            SetSerializedField(slotButtons[i], "titleText", titleText);
            SetSerializedField(slotButtons[i], "summaryText", summary);
        }

        var nameRow = new GameObject("NameRow");
        nameRow.transform.SetParent(content.transform, false);
        var nameRowLayout = nameRow.AddComponent<HorizontalLayoutGroup>();
        nameRowLayout.spacing = 8;
        nameRowLayout.childControlWidth = true;
        nameRowLayout.childForceExpandWidth = false;
        nameRowLayout.childControlHeight = true;
        nameRowLayout.childForceExpandHeight = false;
        nameRow.AddComponent<LayoutElement>().preferredHeight = 40;
        var nameLabel = CreateLayoutText(nameRow.transform, "角色名", 16, 40);
        nameLabel.GetComponent<LayoutElement>().preferredWidth = 72;
        nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
        var nameInput = CreateLayoutInputField(nameRow.transform, "战士");

        var footer = new GameObject("Footer");
        footer.transform.SetParent(content.transform, false);
        footer.AddComponent<LayoutElement>().preferredHeight = 44;
        var footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
        footerLayout.childAlignment = TextAnchor.MiddleCenter;
        footerLayout.childControlWidth = true;
        footerLayout.childForceExpandWidth = false;
        var closeSaveBtn = CreateButton(footer.transform, "关闭");

        savePanelGo.SetActive(false);

        SetSerializedField(savePanel, "panelRoot", savePanelGo);
        SetSerializedField(savePanel, "slotButtons", slotButtons);
        SetSerializedField(savePanel, "closeButton", closeSaveBtn);
        SetSerializedField(savePanel, "playerNameInput", nameInput);
        SetSerializedField(savePanel, "panelTitleText", panelTitle);
        SetSerializedField(savePanel, "panelHintText", panelHint);
        SetSerializedField(savePanel, "mainMenuUI", mainMenuUI);

        var settingsPanelGo = CreateSettingsPanel(canvasGo.transform);
        settingsPanelGo.SetActive(false);

        SetSerializedField(mainMenuUI, "startButton", startBtn);
        SetSerializedField(mainMenuUI, "continueButton", continueBtn);
        SetSerializedField(mainMenuUI, "loadButton", loadBtn);
        SetSerializedField(mainMenuUI, "settingsButton", settingsBtn);
        SetSerializedField(mainMenuUI, "quitButton", quitBtn);
        SetSerializedField(mainMenuUI, "saveSlotPanel", savePanel);
        SetSerializedField(mainMenuUI, "settingsPanel", settingsPanelGo.GetComponent<SettingsPanel>());
        SetSerializedField(mainMenuUI, "mainButtons", mainButtons);

        if (!EditorSceneManager.SaveScene(scene, scenePath))
        {
            Debug.LogError($"CreateMainMenuScene 保存失败: {scenePath}");
            return;
        }

        Debug.Log("MainMenu 场景已重建。");
    }

    static GameObject CreateSettingsPanel(Transform parent)
    {
        var go = new GameObject("SettingsPanel");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        var panel = go.AddComponent<SettingsPanel>();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420, 360);

        var content = new GameObject("Content");
        content.transform.SetParent(go.transform, false);
        StretchRect(content.AddComponent<RectTransform>(), new Vector2(24, 24), new Vector2(-24, -24));
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 12;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childAlignment = TextAnchor.UpperCenter;

        var master = CreateSlider(content.transform, "主音量");
        var music = CreateSlider(content.transform, "音乐");
        var sfx = CreateSlider(content.transform, "音效");
        var fullscreen = CreateToggle(content.transform, "全屏");
        var close = CreateButton(content.transform, "关闭");
        var apply = CreateButton(content.transform, "应用");

        SetSerializedField(panel, "panelRoot", go);
        SetSerializedField(panel, "masterSlider", master);
        SetSerializedField(panel, "musicSlider", music);
        SetSerializedField(panel, "sfxSlider", sfx);
        SetSerializedField(panel, "fullscreenToggle", fullscreen);
        SetSerializedField(panel, "closeButton", close);
        SetSerializedField(panel, "applyButton", apply);
        return go;
    }

    [MenuItem(MenuRoot + "Add Gameplay UI To Main Scene")]
    public static void AddGameplaySystemsToMainScene()
    {
        AddGameplayUIToScene("Assets/Main.unity");
        AddGameplayUIToScene("Assets/Scenes/HouseInterior.unity");
    }

    static void AddGameplayUIToScene(string scenePath)
    {
        if (!File.Exists(scenePath)) return;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (Object.FindObjectOfType<GameplayUIController>() != null)
        {
            EditorSceneManager.SaveScene(scene);
            return;
        }

        var canvasGo = Object.FindObjectOfType<Canvas>()?.gameObject;
        if (canvasGo == null)
            canvasGo = CreateCanvas("GameplayCanvas");

        var controller = canvasGo.GetComponent<GameplayUIController>();
        if (controller == null)
            controller = canvasGo.AddComponent<GameplayUIController>();

        var hud = canvasGo.GetComponent<GameplayHUD>() ?? canvasGo.AddComponent<GameplayHUD>();
        var pause = canvasGo.GetComponent<PauseMenuUI>() ?? canvasGo.AddComponent<PauseMenuUI>();
        var inv = canvasGo.GetComponent<InventoryUI>() ?? canvasGo.AddComponent<InventoryUI>();
        var trade = canvasGo.GetComponent<TradeUI>() ?? canvasGo.AddComponent<TradeUI>();
        var hire = canvasGo.GetComponent<HireUI>() ?? canvasGo.AddComponent<HireUI>();

        var settingsGo = GameObject.Find("SettingsPanel");
        SettingsPanel settings = null;
        if (settingsGo == null)
        {
            settingsGo = CreateSettingsPanel(canvasGo.transform);
            settingsGo.SetActive(false);
        }
        settings = settingsGo.GetComponent<SettingsPanel>();

        SetSerializedField(controller, "hud", hud);
        SetSerializedField(controller, "pauseMenu", pause);
        SetSerializedField(controller, "inventoryUI", inv);
        SetSerializedField(controller, "tradeUI", trade);
        SetSerializedField(controller, "hireUI", hire);
        SetSerializedField(controller, "settingsPanel", settings);
        SetSerializedField(pause, "settingsPanel", settings);

        if (Object.FindObjectOfType<SceneAutoSave>() == null)
        {
            var autoSave = new GameObject("SceneAutoSave");
            autoSave.AddComponent<SceneAutoSave>();
        }

        var player = Object.FindObjectOfType<WarriorPlayer>();
        if (player != null && player.GetComponent<PlayerProgressionTracker>() == null)
            player.gameObject.AddComponent<PlayerProgressionTracker>();

        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem(MenuRoot + "Setup Build Settings")]
    public static void SetupBuildSettings()
    {
        var scenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Main.unity",
            "Assets/Scenes/HouseInterior.unity"
        };

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var path in scenes)
        {
            if (File.Exists(path))
                list.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }

    static GameObject CreateCanvas(string name)
    {
        var es = Object.FindObjectOfType<EventSystem>();
        if (es == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject(name);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo;
    }

    static TMP_FontAsset GetUIFont()
    {
        if (s_UIFont == null)
            s_UIFont = ChineseUIFontBuilder.EnsureChineseUIFont();
        return s_UIFont;
    }

    static DefaultControls.Resources GetStandardResources()
    {
        if (s_StandardResources.standard != null)
            return s_StandardResources;

        s_StandardResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        s_StandardResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        s_StandardResources.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        s_StandardResources.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        s_StandardResources.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        s_StandardResources.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        s_StandardResources.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        return s_StandardResources;
    }

    static Button CreateButton(Transform parent, string label)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var btn = go.AddComponent<Button>();
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260, 40);
        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 260;
        layoutElement.preferredHeight = 40;
        CreateText(go.transform, label, 20, Vector2.zero);
        return btn;
    }

    static TMP_Text CreateText(Transform parent, string text, int size, Vector2 anchoredPos)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = GetUIFont();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = anchoredPos;
        return tmp;
    }

    static TMP_Text CreateLayoutText(Transform parent, string text, int size, float preferredHeight)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        tmp.font = GetUIFont();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    static TMP_InputField CreateLayoutInputField(Transform parent, string defaultText)
    {
        var root = new GameObject("TMP InputField");
        root.transform.SetParent(parent, false);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        var rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.flexibleWidth = 1;
        rootLayout.preferredHeight = 36;

        var input = root.AddComponent<TMP_InputField>();

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(root.transform, false);
        var textAreaRect = textArea.AddComponent<RectTransform>();
        StretchRect(textAreaRect, new Vector2(10, 6), new Vector2(-10, -6));
        textArea.AddComponent<RectMask2D>();

        var placeholder = CreateText(textArea.transform, "输入角色名", 18, Vector2.zero);
        placeholder.name = "Placeholder";
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(1f, 1f, 1f, 0.5f);

        var text = CreateText(textArea.transform, defaultText, 18, Vector2.zero);
        text.name = "Text";
        text.alignment = TextAlignmentOptions.MidlineLeft;

        input.textViewport = textAreaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = defaultText;
        return input;
    }

    static TMP_InputField CreateInputField(Transform parent, string defaultText, Vector2 anchoredPos, Vector2 size)
    {
        var root = new GameObject("TMP InputField");
        root.transform.SetParent(parent, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = size;
        rootRect.anchoredPosition = anchoredPos;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var input = root.AddComponent<TMP_InputField>();

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(root.transform, false);
        var textAreaRect = textArea.AddComponent<RectTransform>();
        StretchRect(textAreaRect, new Vector2(10, 6), new Vector2(-10, -6));
        textArea.AddComponent<RectMask2D>();

        var placeholder = CreateText(textArea.transform, "输入角色名", 18, Vector2.zero);
        placeholder.name = "Placeholder";
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(1f, 1f, 1f, 0.5f);

        var text = CreateText(textArea.transform, defaultText, 18, Vector2.zero);
        text.name = "Text";
        text.alignment = TextAlignmentOptions.MidlineLeft;

        input.textViewport = textAreaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = defaultText;
        return input;
    }

    static Slider CreateSlider(Transform parent, string label)
    {
        var row = new GameObject(label);
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(360, 48);
        var rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 48;

        var labelText = CreateText(row.transform, label, 16, Vector2.zero);
        StretchRect(labelText.rectTransform, new Vector2(0, 24), new Vector2(-180, 0));
        labelText.alignment = TextAlignmentOptions.MidlineLeft;

        var sliderGo = DefaultControls.CreateSlider(GetStandardResources());
        sliderGo.name = "Slider";
        sliderGo.transform.SetParent(row.transform, false);
        var sliderRect = sliderGo.GetComponent<RectTransform>();
        StretchRect(sliderRect, new Vector2(120, 8), new Vector2(0, -8));

        return sliderGo.GetComponent<Slider>();
    }

    static Toggle CreateToggle(Transform parent, string label)
    {
        var row = new GameObject(label);
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(360, 36);
        var rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 36;

        var toggleGo = DefaultControls.CreateToggle(GetStandardResources());
        toggleGo.name = "Toggle";
        toggleGo.transform.SetParent(row.transform, false);
        var toggleRect = toggleGo.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0, 0.5f);
        toggleRect.anchorMax = new Vector2(0, 0.5f);
        toggleRect.pivot = new Vector2(0, 0.5f);
        toggleRect.anchoredPosition = new Vector2(0, 0);
        toggleRect.sizeDelta = new Vector2(28, 28);

        var labelText = CreateText(row.transform, label, 16, Vector2.zero);
        StretchRect(labelText.rectTransform, new Vector2(40, 0), new Vector2(0, 0));
        labelText.alignment = TextAlignmentOptions.MidlineLeft;

        return toggleGo.GetComponent<Toggle>();
    }

    static void StretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.anchoredPosition = Vector2.zero;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetSerializedField(Object target, string fieldName, Object[] value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null && prop.isArray)
        {
            prop.arraySize = value.Length;
            for (int i = 0; i < value.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = value[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
