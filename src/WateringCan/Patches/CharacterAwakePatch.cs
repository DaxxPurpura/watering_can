using HarmonyLib;

namespace WateringCan.Patches;

[HarmonyPatch(typeof(Character), "Awake")]
public class CharacterAwakePatch
{
    static void Postfix(Character __instance)
    {
        if (__instance.gameObject.GetComponent<WateringCan_CustomMethods>() == null)
        {
            Plugin.Log.LogInfo($"Adding CoolOffCooldown to {__instance.characterName}'s character.");
            __instance.gameObject.AddComponent<WateringCan_CustomMethods>();
        }
    }
}