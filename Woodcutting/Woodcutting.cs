using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

namespace Woodcutting;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInIncompatibility("org.bepinex.plugins.valheim_plus")]
[BepInIncompatibility("DMT.StumpsAreOneHp")]
[BepInIncompatibility("coemt.StumpsAreOneHp")]
[BepInIncompatibility("blacks7ar.AxeComboWhenChoppingTree")]
public class Woodcutting : BaseUnityPlugin
{
	private const string ModName = "dmtWoodcutting";
	private const string ModVersion = "1.1.0";
	private const string ModGUID = "dmtWoodcutting";

	private static readonly ConfigSync configSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion };

	internal static ManualLogSource? ModLogger;

	private static ConfigEntry<Toggle> serverConfigLocked = null!;
	private static ConfigEntry<float> treeYieldFactor = null!;
	private static ConfigEntry<float> damageFromTreesFactor = null!;
	private static ConfigEntry<float> forestMovementSpeedFactor = null!;

	internal static ConfigEntry<int> stumpSkillLevel = null!;
	internal static ConfigEntry<int> stumpHp = null!;
	internal static ConfigEntry<Toggle> enableLogHp = null!;
	internal static ConfigEntry<int> logHp = null!;
	internal static ConfigEntry<Toggle> showHpOnAim = null!;
	internal static ConfigEntry<int> hpDisplaySkillLevel = null!;
	internal static ConfigEntry<Toggle> enableAxeCombo = null!;
	internal static ConfigEntry<int> axeComboSkillLevel = null!;
	internal static ConfigEntry<Toggle> enableFellerStamina = null!;
	internal static ConfigEntry<int> fellerStaminaStartLevel = null!;
	internal static ConfigEntry<float> fellerStaminaMax = null!;
	internal static ConfigEntry<Toggle> enableFellingBlow = null!;
	internal static ConfigEntry<int> fellingBlowStartLevel = null!;
	internal static ConfigEntry<float> fellingBlowMaxChance = null!;
	internal static ConfigEntry<Toggle> enableAxeConservation = null!;
	internal static ConfigEntry<int> axeConservationSkillLevel = null!;
	internal static ConfigEntry<float> axeConservationChance = null!;
	internal static ConfigEntry<float> axeConservationMaxChance = null!;
	internal static ConfigEntry<Toggle> enableXpBoost = null!;
	internal static ConfigEntry<int> xpBoostSkillLevel = null!;
	internal static ConfigEntry<float> xpBoostMultiplier = null!;
	internal static ConfigEntry<Toggle> enableLumberjack = null!;
	internal static ConfigEntry<int> lumberjackSkillLevel = null!;

	private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
	{
		ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

		SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
		syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

		return configEntry;
	}

	private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

	internal enum Toggle
	{
		On = 1,
		Off = 0,
	}

	public void Awake()
	{
		ModLogger = Logger;

		serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
		configSync.AddLockingConfigEntry(serverConfigLocked);
		treeYieldFactor = config("1 - General", "Tree item yield modifier at level 100", 2f, new ConfigDescription("Item yield from trees will be modified by this value at skill level 100.", new AcceptableValueRange<float>(1f, 5f)));
		damageFromTreesFactor = config("1 - General", "Tree fall damage modifier at level 100", 0.3f, new ConfigDescription("Damage from falling trees will be modified by this value at skill level 100.", new AcceptableValueRange<float>(0.00f, 1f)));
		forestMovementSpeedFactor = config("1 - General", "Movement speed modifier in forests at level 100", 1.1f, new ConfigDescription("Movement speed in forests will be modified by this value at skill level 100.", new AcceptableValueRange<float>(1f, 2f)));

		stumpSkillLevel = config("2 - Stumps and Logs", "Stump Skill Level", 20, new ConfigDescription("Woodcutting skill level at which stumps get 'Stump HP'. 0 = always. Below this level stumps keep their vanilla HP.", new AcceptableValueRange<int>(0, 100)));
		stumpHp = config("2 - Stumps and Logs", "Stump HP", 1, new ConfigDescription("HP for tree stumps at or above 'Stump Skill Level'. 1 = one hit destroys a stump.", new AcceptableValueRange<int>(0, 1000)));
		enableLogHp = config("2 - Stumps and Logs", "Enable Log HP", Toggle.On, "Enable HP reduction for felled tree logs.");
		logHp = config("2 - Stumps and Logs", "Log HP", 0, new ConfigDescription("0 = progressive skill curve: log HP scales as 2^(-3 x level/100), so logs are halved around skill 33, quartered around 67 and down to 1/8 at 100. Above 0 = fixed HP for logs at every level.", new AcceptableValueRange<int>(0, 1000)));
		showHpOnAim = config("3 - HP Display", "Show HP On Aim", Toggle.On, "Show the current and maximum HP of a tree, stump or log when aiming at it.");
		hpDisplaySkillLevel = config("3 - HP Display", "HP Display Skill Level", 10, new ConfigDescription("Woodcutting skill level at which HP is shown when aiming. 0 = always.", new AcceptableValueRange<int>(0, 100)));
		enableAxeCombo = config("4 - Axe Combo", "Enable Axe Combo", Toggle.On, "Makes the axe attack chain uninterruptible when hitting trees (the chain is not reset on hits).");
		axeComboSkillLevel = config("4 - Axe Combo", "Axe Combo Skill Level", 50, new ConfigDescription("Woodcutting skill level at which the axe combo activates. 0 = active from level 0 while enabled.", new AcceptableValueRange<int>(0, 100)));
		enableFellerStamina = config("5 - Felling Stamina", "Enable Felling Stamina", Toggle.On, "Gain stamina each time you fell a standing tree.");
		fellerStaminaStartLevel = config("5 - Felling Stamina", "Stamina Start Level", 40, new ConfigDescription("Woodcutting level at which felling a standing tree starts granting stamina; the amount grows linearly up to 'Max Stamina At Level 100'.", new AcceptableValueRange<int>(0, 100)));
		fellerStaminaMax = config("5 - Felling Stamina", "Max Stamina At Level 100", 15f, new ConfigDescription("Stamina gained per felled standing tree at level 100 (0 at 'Stamina Start Level', linear in between).", new AcceptableValueRange<float>(0f, 100f)));
		enableFellingBlow = config("6 - Felling Blow", "Enable Felling Blow", Toggle.On, "Chance that a standing tree falls in a single hit.");
		fellingBlowStartLevel = config("6 - Felling Blow", "Felling Blow Start Level", 60, new ConfigDescription("Woodcutting level at which the one-hit fell chance starts growing; it reaches 'Max Chance At Level 100' at level 100.", new AcceptableValueRange<int>(0, 100)));
		fellingBlowMaxChance = config("6 - Felling Blow", "Max Chance At Level 100", 30f, new ConfigDescription("Percent chance at level 100 to fell a standing tree with a single hit (0 at 'Felling Blow Start Level', linear in between).", new AcceptableValueRange<float>(0f, 100f)));
		enableAxeConservation = config("7 - Axe Conservation", "Enable Axe Conservation", Toggle.On, "Chance that hitting a tree or a log does not cost axe durability.");
		axeConservationSkillLevel = config("7 - Axe Conservation", "Axe Conservation Skill Level", 70, new ConfigDescription("Woodcutting skill level at which axe conservation activates.", new AcceptableValueRange<int>(0, 100)));
		axeConservationChance = config("7 - Axe Conservation", "Chance", 30f, new ConfigDescription("Percent chance that a tree or log hit costs no durability at 'Axe Conservation Skill Level'. Grows linearly to 'Max Chance At Level 100' at level 100.", new AcceptableValueRange<float>(0f, 100f)));
		axeConservationMaxChance = config("7 - Axe Conservation", "Max Chance At Level 100", 50f, new ConfigDescription("Percent chance that a tree or log hit costs no durability at skill level 100 (linear from 'Chance' at 'Axe Conservation Skill Level').", new AcceptableValueRange<float>(0f, 100f)));
		enableXpBoost = config("8 - XP Boost", "Enable XP Boost", Toggle.On, "Boost Woodcutting XP gain at high skill levels.");
		xpBoostSkillLevel = config("8 - XP Boost", "XP Boost Skill Level", 80, new ConfigDescription("Woodcutting skill level at which the XP multiplier starts applying.", new AcceptableValueRange<int>(0, 100)));
		xpBoostMultiplier = config("8 - XP Boost", "XP Multiplier", 1.5f, new ConfigDescription("Multiplier applied to Woodcutting XP at or above 'XP Boost Skill Level'.", new AcceptableValueRange<float>(1f, 5f)));
		enableLumberjack = config("9 - Lumberjack", "Enable Lumberjack", Toggle.On, "Trees felled with melee hits fall in the direction you are facing.");
		lumberjackSkillLevel = config("9 - Lumberjack", "Lumberjack Skill Level", 30, new ConfigDescription("Woodcutting skill level at which felled trees fall in the direction you face. 0 = always while enabled.", new AcceptableValueRange<int>(0, 100)));

		stumpSkillLevel.SettingChanged += (_, _) => ApplyAll();
		stumpHp.SettingChanged += (_, _) => ApplyAll();
		enableLogHp.SettingChanged += (_, _) => ApplyAll();
		logHp.SettingChanged += (_, _) => ApplyAll();
		enableAxeCombo.SettingChanged += (_, _) => ApplyAll();
		axeComboSkillLevel.SettingChanged += (_, _) => ApplyAll();

		gameObject.AddComponent<HpDisplay>();

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		foreach (System.Type type in assembly.GetTypes())
		{
			try
			{
				harmony.CreateClassProcessor(type).Patch();
			}
			catch (System.Exception ex)
			{
				Logger.LogError($"Patch failed for {type.FullName}: {ex}");
			}
		}
	}

	internal static void ApplyAll(ObjectDB? db = null)
	{
		StumpHp.Apply();
		AxeCombo.Apply(db ?? ObjectDB.instance);
	}

	internal static float GetWoodCuttingSkillLevel()
	{
		Player player = Player.m_localPlayer;
		if (!player)
		{
			return 0f;
		}

		Skills skills = player.GetSkills();
		if (!skills)
		{
			return 0f;
		}

		return skills.GetSkillLevel(Skills.SkillType.WoodCutting);
	}

	internal static void LogWarning(string message) => ModLogger?.LogWarning(message);

	[HarmonyPatch(typeof(ImpactEffect), nameof(ImpactEffect.OnCollisionEnter))]
	public class SetTreeFlag
	{
		public static bool hitByTree = false;

		private static void Prefix(ImpactEffect __instance)
		{
			if (__instance.GetComponent<TreeLog>())
			{
				hitByTree = true;
			}
		}

		private static void Finalizer() => hitByTree = false;
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	public class ReduceDamageTakenFromTrees
	{
		private static void Prefix(Character __instance, HitData hit)
		{
			if (SetTreeFlag.hitByTree && __instance is Player player)
			{
				hit.ApplyModifier(1 - player.GetComponent<ZNetView>().GetZDO().GetFloat("Woodcutting Skill Factor") * (1 - damageFromTreesFactor.Value));
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Update))]
	public class PlayerUpdate
	{
		private static void Postfix(Player __instance)
		{
			if (__instance == Player.m_localPlayer)
			{
				__instance.GetComponent<ZNetView>().GetZDO().Set("Woodcutting Skill Factor", __instance.GetSkillFactor(Skills.SkillType.WoodCutting));
				Milestones.CheckFelledTree(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), typeof(int))]
	private class IncreaseItemYield
	{
		[UsedImplicitly]
		[HarmonyPriority(Priority.VeryLow)]
		private static void Postfix(ref List<GameObject> __result)
		{
			if (!TreeLogDamage.IsTreeChop)
			{
				return;
			}

			List<GameObject> tmp = new();
			foreach (GameObject item in __result)
			{
				float amount = 1 + TreeLogDamage.TreeChopFactor * (treeYieldFactor.Value - 1);

				for (int num = Mathf.FloorToInt(amount + Random.Range(0f, 1f)); num > 0; --num)
				{
					tmp.Add(item);
				}
			}

			__result = tmp;
		}
	}

	[HarmonyPatch(typeof(TreeLog), nameof(TreeLog.RPC_Damage))]
	public static class TreeLogDamage
	{
		public static bool IsTreeChop = false;
		public static float TreeChopFactor;

		private static void Prefix(HitData hit)
		{
			IsTreeChop = true;
			TreeChopFactor = hit.GetAttacker()?.GetComponent<ZNetView>()?.GetZDO()?.GetFloat("Woodcutting Skill Factor") ?? 0;
		}

		private static void Finalizer() => IsTreeChop = false;
	}

	[HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
	public static class TreeBaseDamage
	{
		private static void Prefix(HitData hit, TreeBase __instance)
		{
			TreeLogDamage.IsTreeChop = true;
			TreeLogDamage.TreeChopFactor = hit.GetAttacker()?.GetComponent<ZNetView>()?.GetZDO()?.GetFloat("Woodcutting Skill Factor") ?? 0;
			Milestones.TryFellingBlow(__instance, TreeLogDamage.TreeChopFactor);
		}

		private static void Finalizer() => TreeLogDamage.IsTreeChop = false;
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetRunSpeedFactor))]
	public static class IncreaseRunMovementSpeedInForest
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			if (WorldGenerator.InForest(__instance.transform.position))
			{
				__result *= 1 + __instance.GetSkillFactor(Skills.SkillType.WoodCutting) * (forestMovementSpeedFactor.Value - 1);
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.GetJogSpeedFactor))]
	public static class IncreaseJogMovementSpeedInForest
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			if (WorldGenerator.InForest(__instance.transform.position))
			{
				__result *= 1 + __instance.GetSkillFactor(Skills.SkillType.WoodCutting) * (forestMovementSpeedFactor.Value - 1);
			}
		}
	}
}
