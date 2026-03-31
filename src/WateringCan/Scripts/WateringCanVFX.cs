using Photon.Pun;
using UnityEngine;

public class WateringCanVFX : MonoBehaviourPun
{
	public Item item;
	public bool isPouring;
	public bool audioStarted;
	public AudioSource audioSource;
	public ParticleSystem particle;
	public GameObject waterLevel;
	public float minWaterLevelPosition = 0.000425f;
	public float maxWaterLevelPosition = 0.0056f;
	public float volume = 0.1f;
	public WateringCanItem wateringCanItem;

	public void Start()
	{
		item = GetComponent<Item>();
		audioSource = transform.Find("Audio_Source").GetComponent<AudioSource>();
		particle = transform.Find("Water_VFX").GetComponent<ParticleSystem>();
		waterLevel = transform.Find("watering_can_B").Find("Water_Level").gameObject;
		wateringCanItem = GetComponent<WateringCanItem>();
	}

	public void Update()
	{
		UpdatePouringWater();

		// Water Level Update
		float waterLevelPosition = Mathf.Lerp(minWaterLevelPosition, maxWaterLevelPosition, wateringCanItem.water / wateringCanItem.totalPourTime);
		waterLevel.transform.localPosition = new Vector3(waterLevel.transform.localPosition.x, waterLevelPosition, waterLevel.transform.localPosition.z);

		if (isPouring && !audioStarted)
		{
			audioSource.Play();
			audioSource.volume = 0f;
			audioStarted = true;
		}
		if (isPouring)
		{
			audioSource.volume = Mathf.Lerp(audioSource.volume, volume, 10f * Time.deltaTime);
		}
		if (!isPouring)
		{
			audioSource.volume = Mathf.Lerp(audioSource.volume, 0f, 10f * Time.deltaTime);
			if (audioSource.volume <= 0.01f)
			{
				audioSource.Stop();
			}
		}
		if (!isPouring && audioStarted)
		{
			audioStarted = false;
		}
	}

	public void UpdatePouringWater()
	{
		if (!photonView.IsMine) return;
		if (!wateringCanItem) return;

		bool flag = wateringCanItem.pouring;
		if (wateringCanItem.water <= 0f)
		{
			flag = false;
		}
		if (flag != isPouring)
		{
			if (flag)
			{
				photonView.RPC("RPC_StartPouringWater", RpcTarget.All);
			}
			else
			{
				photonView.RPC("RPC_EndPouringWater", RpcTarget.All);
			}
			isPouring = flag;
		}
	}

	[PunRPC]
	public void RPC_StartPouringWater()
	{
		isPouring = true;
		if ((bool)particle)
		{
			if (!particle.isPlaying)
			{
				particle.Play();
			}
			ParticleSystem.EmissionModule emission = particle.emission;
			emission.enabled = true;
		}
	}

	[PunRPC]
	public void RPC_EndPouringWater()
	{
		isPouring = false;
		if ((bool)particle)
		{
			ParticleSystem.EmissionModule emission = particle.emission;
			emission.enabled = false;
		}
	}
}
