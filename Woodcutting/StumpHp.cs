using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Woodcutting;

public static class StumpHp
{
	private static readonly string[] StumpPrefabs =
	{
		"Beech_Stub",
		"BirchStub",
		"FirTree_Stub",
		"OakStub",
		"Pinetree_01_Stub",
		"SwampTree1_Stub",
		"AshlandsTreeStump1",
		"AshlandsTreeStump2",
		"AshlandsTreeStump3"
	};

	private static readonly string[] LogPrefabs =
	{
		"beech_log",
		"Birch_log",
		"FirTree_Big_log",
		"FirTree_log",
		"FirTree_Snow_log",
		"Oak_log",
		"PineTree_log",
		"PineTree_Snow_log",
		"PineTree_Snow_log_XL",
		"SwampTree1_log",
		"yggashoot_log",
		"AshlandsTreeLog1",
		"AshlandsTreeLog2"
	};

	private static readonly Dictionary<string, float> StumpVanilla = new();
	private static readonly Dictionary<string, float> LogVanilla = new();
	private static readonly HashSet<string> Warned = new();

	private static List<TreeLog>? logPrefabs;

	private static string lastStateKey = "";

	[HarmonyPatch(typeof(ZNetScene), "Awake")]
	public static class ZNetScene_Awake_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(ZNetScene __instance)
		{
			if (__instance)
			{
				Woodcutting.ApplyAll();
			}
		}
	}

	[HarmonyPatch(typeof(Skills), "RaiseSkill")]
	public static class Skills_RaiseSkill_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(Skills __instance, Skills.SkillType skillType, float factor)
		{
			if (skillType == Skills.SkillType.WoodCutting)
			{
				Woodcutting.ApplyAll();
			}
		}
	}

	[HarmonyPatch(typeof(Player), "OnSpawned")]
	public static class Player_OnSpawned_Patch
	{
		[HarmonyPostfix]
		public static void Postfix()
		{
			Woodcutting.ApplyAll();
		}
	}

	internal static void Apply()
	{
		ZNetScene scene = ZNetScene.instance;
		if (!scene)
		{
			return;
		}

		float level = Woodcutting.GetWoodCuttingSkillLevel();

		bool stumpsScaled = level >= Woodcutting.stumpSkillLevel.Value;
		float stumpTarget = stumpsScaled ? Woodcutting.stumpHp.Value : -1f;

		foreach (string prefabName in StumpPrefabs)
		{
			GameObject prefab = scene.GetPrefab(prefabName);
			if (!prefab)
			{
				WarnOnce(prefabName);
				continue;
			}

			if (!prefab.TryGetComponent<Destructible>(out Destructible destructible))
			{
				WarnOnce(prefabName);
				continue;
			}

			if (!StumpVanilla.ContainsKey(prefabName))
			{
				StumpVanilla[prefabName] = destructible.m_health;
			}

			destructible.m_health = stumpTarget >= 0f ? stumpTarget : StumpVanilla[prefabName];
		}

		bool logsEnabled = Woodcutting.enableLogHp.Value == Woodcutting.Toggle.On;
		int fixedLogHp = Woodcutting.logHp.Value;

		foreach (TreeLog treeLog in GetLogPrefabs(scene))
		{
			string prefabName = CleanName(treeLog.gameObject.name);
			if (string.IsNullOrEmpty(prefabName))
			{
				continue;
			}

			if (!LogVanilla.ContainsKey(prefabName))
			{
				LogVanilla[prefabName] = treeLog.m_health;
			}

			treeLog.m_health = ComputeLogTarget(logsEnabled, fixedLogHp, level, LogVanilla[prefabName]);
		}

		string stateKey = $"{stumpTarget}|{logsEnabled}|{fixedLogHp}|{(int)level}";
		if (stateKey != lastStateKey)
		{
			lastStateKey = stateKey;
			RefreshLiveInstances(stumpTarget, logsEnabled, fixedLogHp, level);
		}
	}

	private static List<TreeLog> GetLogPrefabs(ZNetScene scene)
	{
		if (logPrefabs == null)
		{
			List<TreeLog> discovered = new();
			foreach (GameObject prefab in scene.m_prefabs)
			{
				if (prefab && prefab.TryGetComponent<TreeLog>(out TreeLog treeLog))
				{
					discovered.Add(treeLog);
				}
			}

			if (discovered.Count > 0)
			{
				logPrefabs = discovered;
			}
			else
			{
				Woodcutting.LogWarning("TreeLog prefab auto-discovery found no prefabs; using the built-in log list.");
				logPrefabs = new List<TreeLog>();
				foreach (string prefabName in LogPrefabs)
				{
					GameObject prefab = scene.GetPrefab(prefabName);
					if (prefab && prefab.TryGetComponent<TreeLog>(out TreeLog treeLog))
					{
						logPrefabs.Add(treeLog);
					}
					else
					{
						WarnOnce(prefabName);
					}
				}
			}
		}

		return logPrefabs;
	}

	private static float ComputeLogTarget(bool enabled, int fixedLogHp, float level, float vanilla)
	{
		if (!enabled)
		{
			return vanilla;
		}

		if (fixedLogHp > 0)
		{
			return fixedLogHp;
		}

		return vanilla * Mathf.Pow(2f, -3f * level / 100f);
	}

	private static void RefreshLiveInstances(float stumpTarget, bool logsEnabled, int fixedLogHp, float level)
	{
		foreach (Destructible destructible in Object.FindObjectsByType<Destructible>(FindObjectsSortMode.None))
		{
			string prefabName = CleanName(destructible.gameObject.name);
			if (!StumpVanilla.TryGetValue(prefabName, out float vanilla))
			{
				continue;
			}

			float target = stumpTarget >= 0f ? stumpTarget : vanilla;
			ApplyInstanceHealth(destructible.GetComponent<ZNetView>(), value => destructible.m_health = value, target);
		}

		foreach (TreeLog treeLog in Object.FindObjectsByType<TreeLog>(FindObjectsSortMode.None))
		{
			string prefabName = CleanName(treeLog.gameObject.name);
			if (!LogVanilla.TryGetValue(prefabName, out float vanilla))
			{
				continue;
			}

			float target = ComputeLogTarget(logsEnabled, fixedLogHp, level, vanilla);
			ApplyInstanceHealth(treeLog.GetComponent<ZNetView>(), value => treeLog.m_health = value, target);
		}
	}

	private static void ApplyInstanceHealth(ZNetView nview, System.Action<float> setHealth, float target)
	{
		setHealth(target);
		if (nview && nview.IsOwner())
		{
			ZDO? zdo = nview.GetZDO();
			if (zdo != null)
			{
				zdo.Set("health", target);
			}
		}
	}

	internal static bool IsStump(string cleanName)
	{
		foreach (string prefabName in StumpPrefabs)
		{
			if (prefabName == cleanName)
			{
				return true;
			}
		}

		return false;
	}

	internal static string CleanName(string objectName)
	{
		const string cloneSuffix = "(Clone)";
		return objectName.EndsWith(cloneSuffix)
			? objectName.Substring(0, objectName.Length - cloneSuffix.Length).TrimEnd()
			: objectName;
	}

	private static void WarnOnce(string prefabName)
	{
		if (Warned.Add(prefabName))
		{
			Woodcutting.LogWarning($"Prefab not found or missing component: {prefabName} (further warnings for this name suppressed)");
		}
	}
}
