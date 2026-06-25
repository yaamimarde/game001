using System;
using System.Collections.Generic;

public enum CompanionOrder
{
    Follow,
    Hold,
    AttackTarget
}

[Serializable]
public class CompanionSave
{
    public string templateId;
    public string instanceId;
    public StatBlock stats = new StatBlock();
    public List<InventoryItemSave> inventory = new List<InventoryItemSave>();
    public CompanionOrder order = CompanionOrder.Follow;
}
