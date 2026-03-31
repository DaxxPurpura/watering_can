using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PEAKLib.Core;
using PEAKLib.Items.UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WateringCan;

[BepInDependency(PEAKLib.Core.CorePlugin.Id)]
[BepInDependency(PEAKLib.Items.ItemsPlugin.Id)]
[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static ModDefinition modDefinition = null!;

    internal static ConfigEntry<bool> showWateringSphere = null!;

    private static Dictionary<string, Shader> _peakShaders = null!;
    internal static Dictionary<string, Shader> PeakShaders
    {
        get
        {
            if (_peakShaders == null)
            {
                var shaders = new List<string>
                {
                    "SmokeParticle"
                };
                _peakShaders = new();
                foreach (var sh in shaders)
                {
                    var shader = Shader.Find(sh);
                    if (shader == null)
                    {
                        continue;
                    }
                    _peakShaders[sh] = shader;
                }
            }
            return _peakShaders;
        }
    }

    private void Awake()
    {
        Log = Logger;
        modDefinition = ModDefinition.GetOrCreate(Info.Metadata);
        Log.LogInfo($"Plugin Watering Can is loaded!");

        showWateringSphere = Config.Bind("Debug", "Show Watering Sphere", true, "If true, allows to toggle the visibility of the watering sphere.");

        this.LoadBundleAndContentsWithName("watering_can_bundle.peakbundle", bundle =>
            {
                var wateringCan = bundle.LoadAsset<UnityItemContent>("Watering_Can");
                var wateringCanItem = wateringCan.Item.gameObject.AddComponent<WateringCanItem>();
                wateringCan.Item.gameObject.AddComponent<WateringCanVFX>();

                var vfxLeaves = bundle.LoadAsset<GameObject>("VFX_Leaves");
                NetworkPrefabManager.RegisterNetworkPrefab(modDefinition, vfxLeaves);
                var vfxPalms = bundle.LoadAsset<GameObject>("VFX_Palms");
                NetworkPrefabManager.RegisterNetworkPrefab(modDefinition, vfxPalms);
                var vfxSnow = bundle.LoadAsset<GameObject>("VFX_Snow");
                NetworkPrefabManager.RegisterNetworkPrefab(modDefinition, vfxSnow);
                var snowRenderer = vfxSnow.GetComponent<ParticleSystem>().GetComponent<ParticleSystemRenderer>();
                snowRenderer.material = Resources.FindObjectsOfTypeAll<Material>().ToList().Find(m => m.name == "Snowflake softy");
                snowRenderer.material.shader = PeakShaders["SmokeParticle"];
                var vfxThorns = bundle.LoadAsset<GameObject>("VFX_Thorns");
                NetworkPrefabManager.RegisterNetworkPrefab(modDefinition, vfxThorns);
                var vfxVine = bundle.LoadAsset<GameObject>("VFX_Vine");
                NetworkPrefabManager.RegisterNetworkPrefab(modDefinition, vfxVine);
            }
        );

        //     English /           French /        Italian /         German / Spanish(Spa) / Spanish(Lat) /    Portuguese /      Russian /    Ukrainian / Ch(S) / Ch(T) / Japanese /    Korean /     Polish /      Turkish
		LocalizedText.mainTable["NAME_WATERING CAN"] = new List<string>(13)
		{"WATERING CAN",        "ARROSOIR",  "ANNAFFIATOIO",     "GIEẞKANNE",    "REGADERA",    "REGADERA",      "REGADOR",       "ЛЕЙКА",       "ЛІЙКА", "喷壶", "噴壺", "じょうろ",  "물뿌리개",   "KONEWKA", "SULAMA KABI"};
		LocalizedText.mainTable["POUR WATER"] = new List<string>(13)
		{  "pour water", "verser de l'eau", "versare acqua", "wasser gießen", "verter agua", "verter agua", "despeje água", "налить воду", "налити воду", "倒水", "倒水", "水を注ぎ", "물을 붓다", "wlać wodę",   "su dökmek"};

        new Harmony("com.github.DaxxPurpura.WateringCan").PatchAll();
    }
}
