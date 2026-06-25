using UnityEngine;

[CreateAssetMenu(fileName = "RecruitTemplate", menuName = "Game/Recruit Template")]
public class RecruitTemplate : ScriptableObject
{
    public string templateId;
    public string displayName;
    public GameObject prefab;
    public int hireCost = 200;
    public int dailyWage = 20;
    public string requiredWorldFlag;
    public StatBlock baseStats = StatBlock.CreateDefault();
}
