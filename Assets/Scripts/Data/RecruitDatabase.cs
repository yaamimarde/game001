using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecruitDatabase", menuName = "Game/Recruit Database")]
public class RecruitDatabase : ScriptableObject
{
    public List<RecruitTemplate> templates = new List<RecruitTemplate>();

    Dictionary<string, RecruitTemplate> lookup;

    public RecruitTemplate Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        BuildLookup();
        lookup.TryGetValue(id, out RecruitTemplate t);
        return t;
    }

    void BuildLookup()
    {
        if (lookup != null) return;
        lookup = new Dictionary<string, RecruitTemplate>();
        foreach (var t in templates)
        {
            if (t != null && !string.IsNullOrEmpty(t.templateId))
                lookup[t.templateId] = t;
        }
    }

    void OnEnable() => lookup = null;
}
