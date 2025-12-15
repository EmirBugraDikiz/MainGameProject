using System.Collections.Generic;
using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    // Hangi eþyalarýn toplanmasý gerektiðini belirlemek için bir enum
    public enum ItemType { KasaAnahtari, EskiNot, KirmiziTas, Toplam3Eþya };

    // Toplanan eþyalarý tutan sözlük. Key=ItemType, Value=Toplandý mý?
    private Dictionary<ItemType, bool> collectedItems = new Dictionary<ItemType, bool>();

    [Header("Toplanmasý Gereken Eþyalar")]
    public List<ItemType> requiredItems = new List<ItemType>();

    // Kapýyý kontrol etmek için bir referans (Ýsteðe baðlý, Kapý Scripti kendisi de kontrol edebilir)
    // public RequiredItemsDoor finalDoor;

    void Start()
    {
        // Gerekli tüm eþyalarý baþlangýçta "false" olarak ayarla
        foreach (ItemType item in requiredItems)
        {
            if (!collectedItems.ContainsKey(item))
            {
                collectedItems.Add(item, false);
            }
        }

        // Kontrol için:
        // Debug.Log($"Oyunda toplam {requiredItems.Count} adet eþya aranýyor.");
    }

    // Harici scriptlerin (CollectableItem) çaðýracaðý fonksiyon
    public void CollectItem(ItemType item)
    {
        if (collectedItems.ContainsKey(item) && !collectedItems[item])
        {
            collectedItems[item] = true;
            Debug.Log($"Eþya Toplandý: {item}. Envanterde: {CountCollectedItems()}/{requiredItems.Count}");

            // Kapýnýn açýlýp açýlmayacaðýný kontrol et
            CheckDoorCondition();
        }
        else if (!collectedItems.ContainsKey(item))
        {
            // Gerekli eþyalar listesinde yok ama toplandý. Olmamasý gerekir ama hata kontrolü için.
            Debug.LogWarning($"Eþya toplandý ({item}) ancak RequiredItems listesinde bulunmuyor.");
        }
    }

    // Toplanan eþya sayýsýný döndürür
    public int CountCollectedItems()
    {
        int count = 0;
        foreach (var item in collectedItems.Values)
        {
            if (item)
            {
                count++;
            }
        }
        return count;
    }

    // Tüm gerekli eþyalarýn toplanýp toplanmadýðýný kontrol eden fonksiyon
    public bool AreAllRequiredItemsCollected()
    {
        // Gerekli eþyalarýn sayýsý ile toplanan eþyalarýn sayýsý eþit mi?
        return CountCollectedItems() == requiredItems.Count;
    }

    // Bu fonksiyonu, kapý açma mekanizmasýný ItemCollector scriptinde tutmak isterseniz kullanýn
    private void CheckDoorCondition()
    {
        if (AreAllRequiredItemsCollected())
        {
            Debug.Log("TÜM EÞYALAR TOPLANDI! Kapý açýlacak!");
            // Buradan direkt kapýyý açma komutunu çaðýrabilirsiniz.
            // Örneðin:
            // if (finalDoor != null) { finalDoor.UnlockDoor(); } 
        }
    }
}