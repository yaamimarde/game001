using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HireUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text infoText;
    [SerializeField] Button hireButton;
    [SerializeField] Button dismissButton;
    [SerializeField] Button followButton;
    [SerializeField] Button holdButton;
    [SerializeField] Button closeButton;

    RecruitTemplate currentTemplate;
    CompanionRuntime selectedCompanion;

    void Awake()
    {
        if (hireButton != null) hireButton.onClick.AddListener(DoHire);
        if (dismissButton != null) dismissButton.onClick.AddListener(DismissSelected);
        if (followButton != null) followButton.onClick.AddListener(() => SetOrder(CompanionOrder.Follow));
        if (holdButton != null) holdButton.onClick.AddListener(() => SetOrder(CompanionOrder.Hold));
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void OpenRecruit(RecruitTemplate template)
    {
        currentTemplate = template;
        selectedCompanion = null;
        EnsurePanel();
        if (panelRoot != null) panelRoot.SetActive(true);
        if (titleText != null) titleText.text = template.displayName;
        if (infoText != null)
            infoText.text = $"雇佣费: {template.hireCost}\n日薪: {template.dailyWage}";
        if (hireButton != null) hireButton.gameObject.SetActive(true);
        if (dismissButton != null) dismissButton.gameObject.SetActive(false);
    }

    void EnsurePanel()
    {
        if (panelRoot != null) return;
        panelRoot = new GameObject("HirePanel");
        panelRoot.transform.SetParent(transform, false);
        var rect = panelRoot.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 300);
        panelRoot.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);

        titleText = CreateLabel(panelRoot.transform, "招募", new Vector2(0, 100));
        infoText = CreateLabel(panelRoot.transform, "", new Vector2(0, 40));
        hireButton = CreateButton(panelRoot.transform, "雇佣", new Vector2(0, -40));
        dismissButton = CreateButton(panelRoot.transform, "解散", new Vector2(0, -80));
        followButton = CreateButton(panelRoot.transform, "跟随", new Vector2(-80, -120));
        holdButton = CreateButton(panelRoot.transform, "停留", new Vector2(80, -120));
        closeButton = CreateButton(panelRoot.transform, "关闭", new Vector2(0, -160));

        hireButton.onClick.AddListener(DoHire);
        dismissButton.onClick.AddListener(DismissSelected);
        followButton.onClick.AddListener(() => SetOrder(CompanionOrder.Follow));
        holdButton.onClick.AddListener(() => SetOrder(CompanionOrder.Hold));
        closeButton.onClick.AddListener(Close);
        panelRoot.SetActive(false);
    }

    static TMP_Text CreateLabel(Transform parent, string text, Vector2 pos)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        go.GetComponent<RectTransform>().anchoredPosition = pos;
        return tmp;
    }

    static Button CreateButton(Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = Color.gray;
        var btn = go.AddComponent<Button>();
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(120, 30);
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    public void OpenSquad()
    {
        currentTemplate = null;
        EnsurePanel();
        if (panelRoot != null) panelRoot.SetActive(true);
        if (titleText != null) titleText.text = "队伍";
        if (GameManager.Instance != null)
        {
            var companions = GameManager.Instance.Session.Companions.Active;
            selectedCompanion = companions.Count > 0 ? companions[0] : null;
            if (infoText != null)
                infoText.text = $"同伴数: {companions.Count}";
        }
        else
        {
            selectedCompanion = null;
        }

        if (hireButton != null) hireButton.gameObject.SetActive(false);
        if (dismissButton != null) dismissButton.gameObject.SetActive(true);
    }

    void DoHire()
    {
        if (currentTemplate == null || GameManager.Instance == null) return;
        if (GameManager.Instance.Session.Companions.TryHire(currentTemplate))
            Close();
    }

    void DismissSelected()
    {
        if (GameManager.Instance == null)
            return;

        if (selectedCompanion == null && GameManager.Instance.Session.Companions.Active.Count == 0)
            return;

        GameManager.Instance.Session.Companions.ClearRuntime();
        selectedCompanion = null;
        GameEventBus.RaiseCompanionDismissed();
        Close();
    }

    void SetOrder(CompanionOrder order)
    {
        if (GameManager.Instance == null) return;

        if (selectedCompanion != null)
        {
            GameManager.Instance.Session.Companions.SetOrder(selectedCompanion, order);
            return;
        }

        foreach (var c in GameManager.Instance.Session.Companions.Active)
            GameManager.Instance.Session.Companions.SetOrder(c, order);
    }

    void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
