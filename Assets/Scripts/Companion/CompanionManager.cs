using System.Collections.Generic;
using UnityEngine;

public class CompanionManager
{
    readonly GameSession session;
    readonly List<CompanionRuntime> active = new List<CompanionRuntime>();
    readonly RecruitDatabase recruitDb;
    float wageTimer;

    public IReadOnlyList<CompanionRuntime> Active => active;

    public CompanionManager(GameSession session)
    {
        this.session = session;
        recruitDb = DefaultGameContent.GetRecruitDatabase();
    }

    public void LoadFromSave(List<CompanionSave> saves)
    {
        ClearRuntime();
        if (saves == null || recruitDb == null) return;

        foreach (var save in saves)
        {
            var template = recruitDb.Get(save.templateId);
            if (template == null) continue;
            SpawnCompanion(template, save);
        }
    }

    public List<CompanionSave> ToSaveList()
    {
        var list = new List<CompanionSave>();
        foreach (var c in active)
        {
            if (c?.Character == null) continue;
            list.Add(new CompanionSave
            {
                templateId = c.TemplateId,
                instanceId = c.InstanceId,
                stats = c.Character.GetStatBlock(),
                order = c.CurrentOrder
            });
        }
        return list;
    }

    public bool TryHire(RecruitTemplate template)
    {
        if (template == null || session == null) return false;
        if (!string.IsNullOrEmpty(template.requiredWorldFlag) &&
            !session.HasWorldFlag(template.requiredWorldFlag))
            return false;
        if (session.Gold < template.hireCost) return false;

        session.Gold -= template.hireCost;
        SpawnCompanion(template, null);
        GameEventBus.RaiseCompanionHired();
        return true;
    }

    void SpawnCompanion(RecruitTemplate template, CompanionSave save)
    {
        var player = Object.FindObjectOfType<WarriorPlayer>();
        Vector3 pos = player != null ? player.transform.position + Vector3.left : Vector3.zero;

        GameObject go;
        if (template.prefab != null)
        {
            go = Object.Instantiate(template.prefab, pos, Quaternion.identity);
        }
        else
        {
            go = CreateRuntimeCompanionPrefab(pos);
        }
        Object.DontDestroyOnLoad(go);

        var character = go.GetComponent<CompanionCharacter>();
        if (character == null)
            character = go.AddComponent<CompanionCharacter>();

        character.Initialize(template, save);
        var runtime = new CompanionRuntime
        {
            GameObject = go,
            Character = character,
            TemplateId = template.templateId,
            InstanceId = save?.instanceId ?? System.Guid.NewGuid().ToString("N"),
            CurrentOrder = save?.order ?? CompanionOrder.Follow
        };

        var moveAi = go.GetComponent<FriendlyMoveAI>();
        if (moveAi != null)
            moveAi.SetOrder(runtime.CurrentOrder);

        active.Add(runtime);
    }

    static GameObject CreateRuntimeCompanionPrefab(Vector3 pos)
    {
        var go = new GameObject("Companion");
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<SpriteRenderer>().color = new Color(0.3f, 0.6f, 1f);
        go.AddComponent<CompanionCharacter>();
        go.AddComponent<FriendlyMoveAI>();
        return go;
    }

    public void SetOrder(CompanionRuntime companion, CompanionOrder order)
    {
        if (companion == null) return;
        companion.CurrentOrder = order;
        var ai = companion.GameObject.GetComponent<FriendlyMoveAI>();
        if (ai != null) ai.SetOrder(order);
        GameEventBus.RaiseCompanionOrderChanged();
    }

    public void RepositionNearPlayer()
    {
        var player = Object.FindObjectOfType<WarriorPlayer>();
        if (player == null) return;

        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].GameObject == null) continue;
            Vector3 offset = Quaternion.Euler(0, 0, i * 45f) * Vector3.left * 1.5f;
            active[i].GameObject.transform.position = player.transform.position + offset;
        }
    }

    public void ClearRuntime()
    {
        foreach (var c in active)
        {
            if (c.GameObject != null)
                Object.Destroy(c.GameObject);
        }
        active.Clear();
    }

    public void TickWages(float deltaTime)
    {
        wageTimer += deltaTime;
        if (wageTimer < 60f) return; // 每分钟结算一次（测试用）
        wageTimer = 0f;

        var db = recruitDb;
        if (db == null) return;

        int totalWage = 0;
        foreach (var c in active)
        {
            var t = db.Get(c.TemplateId);
            if (t != null) totalWage += t.dailyWage;
        }

        if (totalWage > 0 && session.Gold >= totalWage)
            session.Gold -= totalWage;
    }
}

public class CompanionRuntime
{
    public GameObject GameObject;
    public CompanionCharacter Character;
    public string TemplateId;
    public string InstanceId;
    public CompanionOrder CurrentOrder;
}
