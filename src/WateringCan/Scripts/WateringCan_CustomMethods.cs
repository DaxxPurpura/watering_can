using PEAKLib.Core;
using Photon.Pun;
using UnityEngine;
using WateringCan;

public class WateringCan_CustomMethods : MonoBehaviourPun
{
    public float coolOffCooldown;

    public void Update()
    {
        if (coolOffCooldown > 0)
        {
            coolOffCooldown -= Time.deltaTime;
        }
    }

    [PunRPC]
    public void RPC_InstantiateFruit(string fruitName, Vector3 spawnSpot, string bushName)
    {
        GameObject spawnedFruit = PhotonNetwork.InstantiateItemRoom(fruitName, spawnSpot, Quaternion.identity);
        spawnedFruit.GetComponent<PhotonView>().RPC("SetKinematicRPC", RpcTarget.AllBuffered, true, spawnedFruit.transform.position, spawnedFruit.transform.rotation);
        photonView.RPC("RPC_AddGrowFruitComponent", RpcTarget.All, spawnedFruit.GetComponent<PhotonView>());
        if (bushName.Contains("berrybush ") || bushName.Contains("Jungle_Willow"))
        {
            NetworkPrefabManager.SpawnNetworkPrefab(Plugin.modDefinition.Id + ":VFX_Leaves", spawnedFruit.transform.position, Quaternion.identity);
        }
        else if (bushName.Contains("Jungle_PalmTree"))
        {
            NetworkPrefabManager.SpawnNetworkPrefab(Plugin.modDefinition.Id + ":VFX_Palms", spawnedFruit.transform.position, Quaternion.identity);
        }
        else if (bushName.Contains("Ice_DeadTree"))
        {
            NetworkPrefabManager.SpawnNetworkPrefab(Plugin.modDefinition.Id + ":VFX_Snow", spawnedFruit.transform.position, Quaternion.identity);
        }
        else if (bushName.Contains("Cactus"))
        {
            NetworkPrefabManager.SpawnNetworkPrefab(Plugin.modDefinition.Id + ":VFX_Thorns", spawnedFruit.transform.position, Quaternion.identity);
        }
        Plugin.Log.LogInfo($"{fruitName} instantiated!");
    }

    [PunRPC]
    public void RPC_AddGrowFruitComponent(PhotonView fruitPhotonView)
    {
        GameObject fruit = fruitPhotonView.gameObject;
        if (fruit.GetComponent<WateringCan_GrowFruit>() == null) fruit.AddComponent<WateringCan_GrowFruit>();
    }
}