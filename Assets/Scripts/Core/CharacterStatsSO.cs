using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "Game001/Character Stats")]
public class CharacterStatsSO : ScriptableObject
{
    public string characterName = "Character";
    public int maxHp = 100;
    public int damage = 10;
    public int defense = 5;
    public AttackType attackType = AttackType.Melee;
}
