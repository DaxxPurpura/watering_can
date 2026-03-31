using HarmonyLib;

namespace WateringCan.Patches;

[HarmonyPatch(typeof(ItemCooking), "FinishCookingRPC")]
public class ItemCookingFinishCookingRPCPatch
{
    static void Postfix(ItemCooking __instance)
    {
        var wateringCanItem = __instance.GetComponent<WateringCanItem>();
        if (wateringCanItem == null) return;
        
        wateringCanItem.VaporizeWater();
    }
}