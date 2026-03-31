using Photon.Pun;
using System;
using System.Linq;
using UnityEngine;
using WateringCan;


public class WateringCan_SetFlareUnlitRPC : ItemComponent
{
    public void Start()
    {
        Dynamite dynamite = GetComponent<Dynamite>();
        dynamite.lightFuseRadius = 0;
        dynamite.GetData<BoolItemData>(DataEntryKey.FlareActive).Value = false;
        dynamite.sparks.gameObject.SetActive(false);
        dynamite.sparksPhotosensitive.gameObject.SetActive(false);
    }

    public override void OnInstanceDataSet()
    {
    }

    [PunRPC]
    public void SetFlareUnlitRPC()
    {
        Dynamite dynamite = GetComponent<Dynamite>();
        dynamite.GetData<BoolItemData>(DataEntryKey.FlareActive).Value = false;
        dynamite.GetData(DataEntryKey.Fuel, dynamite.SetupDefaultFuel).Value = dynamite.startingFuseTime;
        dynamite.lightFuseRadius = 0;
        dynamite.sparks.gameObject.SetActive(false);
        dynamite.sparksPhotosensitive.gameObject.SetActive(false);
        GameObject[] allDynamiteSmoke = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
        .Where(gameObject => gameObject.name.Contains("VFX_DynamiteSmoke"))
        .ToArray();

        foreach (GameObject dynamiteSmoke in allDynamiteSmoke)
        {
            TrackNetworkedObject smokeTrack = dynamiteSmoke.GetComponent<TrackNetworkedObject>();
            if (smokeTrack != null && smokeTrack.trackedObject != null)
            {
                if (smokeTrack.trackedObject == dynamite.trackable)
                {
                    dynamiteSmoke.GetComponent<ParticleSystem>().Stop();
                    dynamiteSmoke.GetComponent<AudioLoop>().volume = 0;
                    GameObject endSFX = dynamiteSmoke.transform.Find("end").gameObject;
                    endSFX.GetComponent<SFX_PlayOneShot>().afterPlayAction = DestroySmoke(dynamiteSmoke);
                    endSFX.SetActive(true);
                }
            }
        }
        Plugin.Log.LogInfo($"Putting off dynamite!");
    }

    public Action DestroySmoke(GameObject smoke)
    {
        Destroy(smoke);
        return null;
    }
}