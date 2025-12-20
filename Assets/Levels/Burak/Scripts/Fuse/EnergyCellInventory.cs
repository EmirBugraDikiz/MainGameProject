using UnityEngine;

public static class EnergyCellInventory
{
    public static bool HasCell { get; private set; }
    public static GameObject PickedCellObject { get; private set; }

    public static void Pickup(GameObject cellObj)
    {
        HasCell = true;
        PickedCellObject = cellObj;
    }

    public static GameObject ConsumeCell()
    {
        if (!HasCell) return null;
        HasCell = false;
        var obj = PickedCellObject;
        PickedCellObject = null;
        return obj;
    }
}
