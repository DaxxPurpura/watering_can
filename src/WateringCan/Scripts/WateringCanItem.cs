using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WateringCan;
using Zorro.Core;

public class WateringCanItem : ItemComponent
{
	public float initialPourCost = 0.25f;
	public float totalPourTime = 10f;
	public bool pouring;

	public float interactionRadius = 1f;
	public LayerMask interactionLayers = LayerMask.GetMask("Map", "Terrain", "Character", "Default");
	public Transform overlapSphereDefault = null!;
	public Transform overlapSphereUp = null!;
	public Vector3 overlapSpherePosition;
	public bool isOverlapSphereVisible = false;

	public float selfWaterValue = 0.8f;
	public float seaLevel = 0.425f;
	public float rootsWaterLevel = 215f;
	public StormVisual rainVisual = null!;
	public float rainRefillAmount = 0.05f;
	public GameObject[] heatZones = null!;
	public float waterPoolsRadius = 4.5f;
	public float waterPoolsDepth = -0.9f;
	public float waterPoolsHeight = 0.9f;
	//public int baseWeight = 2;
	//public int maxWaterWeight = 5;
	//public int currentCarryWeight = 0;

	// Watering Bush
	public Dictionary<GameObject, int> bushGrowthProgress = new Dictionary<GameObject, int>();
	public int fruitGrowTime = 27;

	// Watering Magic Bean
	public float vineGrowLength = 30f;
	public float vineGrowTick; // 0.125f
	public bool canVineGrow = true;

	// Cooling Off Player
	public float coolOffPlayerAmount = 0.025f;
	public float coolOffPlayerCooldown = 0.25f;

	[SerializeField]
	public float water;
	public float generalTick; // 0.1f

	public override void Awake()
	{
		base.Awake();
		Item obj = item;
		obj.OnPrimaryStarted = (Action)Delegate.Combine(obj.OnPrimaryStarted, new Action(StartPour));
		Item obj2 = item;
		obj2.OnPrimaryCancelled = (Action)Delegate.Combine(obj2.OnPrimaryCancelled, new Action(CancelPour));
		overlapSphereDefault = transform.Find("Overlap_Sphere_Default");
		overlapSphereUp = transform.Find("Overlap_Sphere_Up");
		transform.Find("Sphere").gameObject.SetActive(isOverlapSphereVisible);
		GameObject rain = GameObject.FindGameObjectWithTag("Rain");
		if (rain != null) rainVisual = rain.GetComponent<StormVisual>();
		heatZones = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
        .Where(gameObject => gameObject.name == "heat zone" && gameObject.activeInHierarchy == true)
        .ToArray();
	}

	public void OnDestroy()
	{
		Item obj = item;
		obj.OnPrimaryHeld = (Action)Delegate.Remove(obj.OnPrimaryHeld, new Action(StartPour));
		Item obj2 = item;
		obj2.OnPrimaryCancelled = (Action)Delegate.Remove(obj2.OnPrimaryCancelled, new Action(CancelPour));
	}

	public override void OnInstanceDataSet()
	{
		if (HasData(DataEntryKey.Fuel))
		{
			water = GetData<FloatItemData>(DataEntryKey.Fuel).Value;
			item.SetUseRemainingPercentage(water / totalPourTime);
		}
		else if (photonView.IsMine)
		{
			water = totalPourTime;
			item.SetUseRemainingPercentage(1f);
		}
	}

	public void Update()
	{
		generalTick -= Time.deltaTime;

		UpdatePour();

		if (generalTick <= 0f)
		{
			if (water != totalPourTime)
			{
				RefillWater();
			}
			generalTick = 0.1f;
		}

		if (Plugin.showWateringSphere.Value == true && Input.GetKeyDown(KeyCode.O))
		{
			isOverlapSphereVisible = !isOverlapSphereVisible;
			transform.Find("Sphere").gameObject.SetActive(isOverlapSphereVisible);
			Plugin.Log.LogInfo($"Watering sphere toggled");
		}

		overlapSpherePosition = overlapSphereDefault.position;
		if (overlapSphereDefault.position.y - transform.position.y > selfWaterValue) overlapSpherePosition = overlapSphereUp.position;
		if (isOverlapSphereVisible == true) transform.Find("Sphere").position = overlapSpherePosition;

		// Weight stuff that doesn't work so...
		//if (GetComponent<Item>().carryWeight != currentCarryWeight)
		//{
		//	Plugin.Log.LogInfo(GetComponent<Item>().carryWeight);
		//	currentCarryWeight = Mathf.RoundToInt(maxWaterWeight * (water / totalPourTime) + baseWeight);
		//	GetComponent<Item>().carryWeight = currentCarryWeight;
		//	Plugin.Log.LogInfo(GetComponent<Item>().carryWeight);
		//	Plugin.Log.LogInfo(currentCarryWeight);
		//	if (GetComponent<Item>().trueHolderCharacter != null) GetComponent<Item>().trueHolderCharacter.refs.afflictions.UpdateWeight();
		//}
	}

	public void UpdatePour()
	{
		if (!pouring || !photonView.IsMine)
		{
			return;
		}
		water -= Time.deltaTime;

		if (water <= 0f)
		{
			water = 0f;
			CancelPour();
		}
		else
		{
			if (generalTick <= 0f)
			{
				Collider[] colliders = Physics.OverlapSphere(overlapSpherePosition, interactionRadius, interactionLayers);

				foreach (Collider collider in colliders)
				{
					if (collider.transform == transform || collider.transform.IsChildOf(transform)) continue;
					WaterInteraction(collider.gameObject);
				}
			}
		}
		GetData<FloatItemData>(DataEntryKey.Fuel).Value = water;
		item.SetUseRemainingPercentage(water / totalPourTime);
	}

	public void WaterInteraction(GameObject target)
	{
		// Plugin.Log.LogInfo($"Watering: {target.name} in Layer {target.gameObject.layer} ({LayerMask.LayerToName(target.gameObject.layer)})");
		if (target.name.Contains("berrybush ") && target.gameObject.layer == 21) // Map Layer
		{
			GrowFruitOnBush(target);
		}
		else if ((target.name.Contains("Mesh") || target.name.Contains("Ice_DeadTree")
		|| target.name.Contains("Cactus")) && target.gameObject.layer == 21
		&& target.transform.parent.GetComponent<BerryBush>()) // Map Layer
		{
			GrowFruitOnBush(target.transform.parent.gameObject);
		}
		// Magic Bean vine stuff that doesn't work so...
		//else if (target.name == "Stalk" && target.gameObject.layer == 20) // Terrain Layer
		//{
		//	GrowMagicBeanCoroutine(target);
		//}
		else if (target.name == "RigCollider" && target.gameObject.layer == 10) // Character Layer
		{
			GameObject character = target.transform.root.gameObject;
			if (character.GetComponent<CharacterAfflictions>().GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hot) <= 0f) return;
			if (character.GetComponent<WateringCan_CustomMethods>().coolOffCooldown > 0f) return;
			photonView.RPC("RPC_CoolOffPlayer", RpcTarget.All, character.GetPhotonView());
		}
		else if (target.transform.root.name.Contains("Dynamite")
		&& target.transform.root.gameObject.GetComponent<Dynamite>().GetData<BoolItemData>(DataEntryKey.FlareActive).Value == true) // Default Layer
		{
			photonView.RPC("RPC_AnRPCToAddAnRPCLol", RpcTarget.All, target.transform.root.gameObject.GetPhotonView());
		}
		else if (target.transform.parent.GetComponent<Campfire>()
		&& target.transform.parent.GetComponent<Campfire>().state == Campfire.FireState.Lit)
		{
			target.transform.parent.GetComponent<PhotonView>().RPC("Extinguish_Rpc", RpcTarget.AllBuffered);
		}
	}

	public void GrowFruitOnBush(GameObject bush)
	{
		if (!bushGrowthProgress.ContainsKey(bush))
		{
			bushGrowthProgress[bush] = 0;
			Transform? areFreeSpots = FindFreeSpawnSpot(bush.GetComponent<BerryBush>().GetSpawnSpots());
			if (areFreeSpots == null)
			{
				bushGrowthProgress.Remove(bush);
				Plugin.Log.LogInfo($"No free spawn spots");
				return;
			}
		}

		Plugin.Log.LogInfo($"Growing fruit in {bush.name} ({bushGrowthProgress[bush] * 100 / fruitGrowTime}%)");

		if (bushGrowthProgress[bush] < fruitGrowTime) bushGrowthProgress[bush] += 1;

		if (bushGrowthProgress[bush] >= fruitGrowTime)
		{
			bushGrowthProgress.Remove(bush);
			Transform? freeSpawnSpot = FindFreeSpawnSpot(bush.GetComponent<BerryBush>().GetSpawnSpots());

			if (freeSpawnSpot == null)
			{
				Plugin.Log.LogInfo($"No free spawn spots");
				return;
			}

			var chosenFruit = LootData.GetRandomItem(bush.GetComponent<BerryBush>().GetSpawnPool());
			Character.localCharacter.photonView.RPC("RPC_InstantiateFruit", RpcTarget.MasterClient, chosenFruit.name, freeSpawnSpot.position, bush.name);
		}
	}

	public Transform? FindFreeSpawnSpot(List<Transform> spawnSpots)
	{
		Transform[] shuffledSpots = spawnSpots.OrderBy(x => new System.Random().Next()).ToArray();

		foreach (Transform spot in shuffledSpots)
		{
			if (IsSpawnSpotFree(spot))
			{
				return spot;
			}
		}
		return null;
	}

	public bool IsSpawnSpotFree(Transform spot)
	{
			Collider[] colliders = Physics.OverlapSphere(spot.position, 0.1f);

			foreach (Collider collider in colliders)
			{
				if (collider.transform.IsChildOf(spot.root)) continue;
				if (collider.isTrigger) continue;

				return false;
			}
		return true;
	}

	public void GrowMagicBean(GameObject stalk)
	{
		float vineMaxLength = stalk.transform.parent.GetComponentInParent<MagicBeanVine>().maxLength;

		if (vineMaxLength < vineGrowLength)
		{
			vineGrowTick -= Time.deltaTime;
			if (vineGrowTick <= 0f)
			{
				stalk.transform.parent.GetComponentInParent<MagicBeanVine>().maxLength += 1f;
				Plugin.Log.LogInfo($"Vine growing to {vineMaxLength}");
				//RemoveOtherVine(stalk);
				vineGrowTick = 0.125f;
			}
		}
	}

	public void RemoveOtherVine(GameObject stalk)
	{
		Collider[] colliders = Physics.OverlapSphere(stalk.transform.position, 0.1f);

		foreach (Collider collider in colliders)
		{
			GameObject magicBeanVine = collider.transform.parent.parent.gameObject;
			if (magicBeanVine.name == "MagicBeanVine(Clone)" && collider.transform.position == stalk.transform.position && magicBeanVine != stalk.transform.parent.parent.gameObject)
			{
				Plugin.Log.LogInfo($"Removing duplicated {magicBeanVine.name}");
				Destroy(magicBeanVine);
			}
		}
	}

	public void RefillWater()
	{
		if (transform.position.y <= seaLevel
		|| (transform.position.y <= rootsWaterLevel && Singleton<MapHandler>.Instance.GetCurrentBiome() == Biome.BiomeType.Roots)
		|| IsWaterPoolNear() == true)
		{
			water = totalPourTime;
			GetData<FloatItemData>(DataEntryKey.Fuel).Value = water;
			item.SetUseRemainingPercentage(1f);
			Plugin.Log.LogInfo($"Refilling Watering Can!");
		}
		else if (IsRaining() == true && water < totalPourTime)
		{
			water += rainRefillAmount;
			GetData<FloatItemData>(DataEntryKey.Fuel).Value = water;
			item.SetUseRemainingPercentage(water / totalPourTime);
			Plugin.Log.LogInfo($"Slowly refilling Watering Can! ({Math.Round(water * 10)}%)");
		}
	}

	public bool IsWaterPoolNear()
	{
		if (Singleton<MapHandler>.Instance == null || Singleton<MapHandler>.Instance.GetCurrentSegment() != Segment.Alpine) return false;

		foreach (GameObject heatZone in heatZones)
		{
			if (Vector3.Distance(transform.position, heatZone.transform.position) <= waterPoolsRadius)
			{
				float wateringCanYDistance = transform.position.y - heatZone.transform.position.y;
				if (wateringCanYDistance >= waterPoolsDepth && wateringCanYDistance <= waterPoolsHeight) return true;
			}
		}

		return false;
	}

	public bool IsRaining()
	{
		if (rainVisual == null) return false;
		if (rainVisual.observedPlayerInWindZone == true) return true;
		return false;
	}


	void OnTriggerEnter(Collider collider)
	{
    	if (water != totalPourTime && collider.transform.parent.parent.name.Contains("Waterfall_Spline"))
		{
			water = totalPourTime;
			GetData<FloatItemData>(DataEntryKey.Fuel).Value = water;
			item.SetUseRemainingPercentage(1f);
			Plugin.Log.LogInfo($"Refilling Watering Can!");
		}
	}

	public void StartPour()
	{
		//Plugin.Log.LogInfo($"Started pouring water");
		if (water >= initialPourCost)
		{
			water -= initialPourCost;
			pouring = true;
			item.SetUseRemainingPercentage(water / totalPourTime);
		}
		else
		{
			water = 0f;
			pouring = true;
			item.SetUseRemainingPercentage(water / totalPourTime);
		}
	}

	public void CancelPour()
	{
		//Plugin.Log.LogInfo($"Cancelled pouring water");
		pouring = false;
	}

	[PunRPC]
	public void RPC_CoolOffPlayer(PhotonView characterView)
	{
		Photon.Realtime.Player player = characterView.Owner;
		if (player == null) return;
		if (player.IsLocal) Character.localCharacter.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Hot, coolOffPlayerAmount);
		characterView.transform.root.gameObject.GetComponent<WateringCan_CustomMethods>().coolOffCooldown = coolOffPlayerCooldown;
		Plugin.Log.LogInfo($"Cooling off {player.NickName}");
    }

	[PunRPC]
	public void RPC_AnRPCToAddAnRPCLol(PhotonView dynamiteView)
	{
		GameObject dynamite = dynamiteView.transform.root.gameObject;
		if (dynamite.GetComponent<WateringCan_SetFlareUnlitRPC>() == null) dynamite.AddComponent<WateringCan_SetFlareUnlitRPC>();
		dynamiteView.RPC("SetFlareUnlitRPC", RpcTarget.All);
	}

	[PunRPC]
	public void RPC_PutOffCampfire(PhotonView campfireView)
	{
		GameObject campfire = campfireView.gameObject;
		campfire.GetComponent<Campfire>().beenBurningFor = campfire.GetComponent<Campfire>().burnsFor - 0.1f;
	}

	public void VaporizeWater()
	{
		water = 0f;
		GetData<FloatItemData>(DataEntryKey.Fuel).Value = 0f;
		item.SetUseRemainingPercentage(0f);
		Plugin.Log.LogInfo($"Vaporizing water!");
	}
}
