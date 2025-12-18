using System.Collections.Generic;
using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    public enum ItemType
    {
        KasaAnahtari,
        EskiNot,
        KirmiziTas,
        UluKapsul
    }

    [Header("Required Items")]
    public List<ItemType> requiredItems = new List<ItemType>();

    private readonly Dictionary<ItemType, bool> collectedItems = new Dictionary<ItemType, bool>();

    private void Start()
    {
        foreach (ItemType item in requiredItems)
        {
            if (!collectedItems.ContainsKey(item))
                collectedItems.Add(item, false);
        }
    }

    public void CollectItem(ItemType item)
    {
        if (!collectedItems.ContainsKey(item))
        {
            Debug.LogWarning($"Collected item ({item}) but it is not in requiredItems list.");
            return;
        }

        if (collectedItems[item]) return;

        collectedItems[item] = true;
        Debug.Log($"Item collected: {item}. Progress: {CountCollectedItems()}/{requiredItems.Count}");

        if (AreAllRequiredItemsCollected())
            Debug.Log("ALL REQUIRED ITEMS COLLECTED! Door can be unlocked.");
    }

    public int CountCollectedItems()
    {
        int count = 0;
        foreach (bool collected in collectedItems.Values)
            if (collected) count++;
        return count;
    }

    public bool AreAllRequiredItemsCollected()
    {
        return CountCollectedItems() == requiredItems.Count;
    }
}
