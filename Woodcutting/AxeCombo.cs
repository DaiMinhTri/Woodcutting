using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Woodcutting;

public static class AxeCombo
{
	private static readonly Dictionary<string, DestructibleType> Originals = new();

	private static ObjectDB? lastDb;
	private static bool lastActive;

	[HarmonyPatch(typeof(ObjectDB), "Awake")]
	public static class ObjectDB_Awake_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(ObjectDB __instance)
		{
			Woodcutting.ApplyAll(__instance);
		}
	}

	internal static void Apply(ObjectDB? db)
	{
		if (!db || db.m_items == null || db.m_items.Count == 0 || !db.GetItemPrefab("Wood"))
		{
			return;
		}

		bool enabled = Woodcutting.enableAxeCombo.Value == Woodcutting.Toggle.On;
		float level = Woodcutting.GetWoodCuttingSkillLevel();
		bool active = enabled && level >= Woodcutting.axeComboSkillLevel.Value;

		if (db == lastDb && active == lastActive)
		{
			return;
		}

		lastDb = db;
		lastActive = active;

		foreach (GameObject itemPrefab in db.m_items)
		{
			if (!itemPrefab)
			{
				continue;
			}

			if (!itemPrefab.name.ToLowerInvariant().Contains("axe"))
			{
				continue;
			}

			ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
			if (!itemDrop || itemDrop.m_itemData == null || itemDrop.m_itemData.m_shared == null || itemDrop.m_itemData.m_shared.m_attack == null)
			{
				continue;
			}

			Attack attack = itemDrop.m_itemData.m_shared.m_attack;
			string key = itemPrefab.name;

			if (!Originals.ContainsKey(key))
			{
				Originals[key] = attack.m_resetChainIfHit;
			}

			attack.m_resetChainIfHit = active ? DestructibleType.None : Originals[key];
		}
	}
}
