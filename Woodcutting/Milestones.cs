using HarmonyLib;
using UnityEngine;

namespace Woodcutting;

public static class Milestones
{
	private static TreeBase? pendingTree;
	private static float pendingTime;
	private static bool pendingActive;

	private static ItemDrop.ItemData? swingStartItem;
	private static float swingStartDurability;
	private static bool conservationArmed;

	internal static float Progress(float level, int startLevel, float maxValue)
	{
		if (startLevel >= 100 || level <= startLevel)
		{
			return 0f;
		}

		return maxValue * Mathf.Clamp01((level - startLevel) / (100f - startLevel));
	}

	internal static void TryFellingBlow(TreeBase tree, float attackerFactor)
	{
		if (Woodcutting.enableFellingBlow.Value != Woodcutting.Toggle.On || attackerFactor <= 0f)
		{
			return;
		}

		float chance = Progress(attackerFactor * 100f, Woodcutting.fellingBlowStartLevel.Value, Woodcutting.fellingBlowMaxChance.Value);
		if (chance <= 0f || Random.value * 100f >= chance)
		{
			return;
		}

		ZNetView nview = tree.GetComponent<ZNetView>();
		if (!nview || !nview.IsOwner())
		{
			return;
		}

		ZDO? zdo = nview.GetZDO();
		if (zdo != null)
		{
			zdo.Set("health", 0.01f);
		}
	}

	internal static void TrackFelledTree(HitData hit, TreeBase tree)
	{
		if (Woodcutting.enableFellerStamina.Value != Woodcutting.Toggle.On)
		{
			return;
		}

		Player player = Player.m_localPlayer;
		if (!player || hit.GetAttacker() != player)
		{
			return;
		}

		if (Woodcutting.GetWoodCuttingSkillLevel() < Woodcutting.fellerStaminaStartLevel.Value)
		{
			return;
		}

		pendingTree = tree;
		pendingTime = Time.time;
		pendingActive = true;
	}

	internal static void ApplyLumberjackDirection(HitData hit)
	{
		if (Woodcutting.enableLumberjack.Value != Woodcutting.Toggle.On || hit.m_ranged)
		{
			return;
		}

		Player player = Player.m_localPlayer;
		if (!player || hit.GetAttacker() != player)
		{
			return;
		}

		if (Woodcutting.GetWoodCuttingSkillLevel() < Woodcutting.lumberjackSkillLevel.Value)
		{
			return;
		}

		Vector3 dir = player.GetLookDir();
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.0001f)
		{
			return;
		}

		hit.m_dir = dir.normalized;
	}

	internal static void CheckFelledTree(Player player)
	{
		if (!pendingActive)
		{
			return;
		}

		if (pendingTree)
		{
			if (Time.time - pendingTime > 5f)
			{
				pendingActive = false;
				pendingTree = null;
			}

			return;
		}

		pendingActive = false;
		pendingTree = null;

		if (Woodcutting.enableFellerStamina.Value != Woodcutting.Toggle.On)
		{
			return;
		}

		float amount = Progress(Woodcutting.GetWoodCuttingSkillLevel(), Woodcutting.fellerStaminaStartLevel.Value, Woodcutting.fellerStaminaMax.Value);
		if (amount > 0f)
		{
			player.AddStamina(amount);
		}
	}

	private static void ArmConservation(HitData hit)
	{
		if (Woodcutting.enableAxeConservation.Value != Woodcutting.Toggle.On)
		{
			return;
		}

		Player player = Player.m_localPlayer;
		if (!player || hit.GetAttacker() != player)
		{
			return;
		}

		if (Woodcutting.GetWoodCuttingSkillLevel() < Woodcutting.axeConservationSkillLevel.Value)
		{
			return;
		}

		float chance = Woodcutting.axeConservationChance.Value + Progress(Woodcutting.GetWoodCuttingSkillLevel(), Woodcutting.axeConservationSkillLevel.Value, Woodcutting.axeConservationMaxChance.Value - Woodcutting.axeConservationChance.Value);
		if (chance <= 0f || Random.value * 100f >= chance)
		{
			return;
		}

		if (player.RightItem == null)
		{
			return;
		}

		conservationArmed = true;
	}

	[HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
	public static class TreeBase_Damage_Patch
	{
		private static void Prefix(TreeBase __instance, HitData hit)
		{
			TrackFelledTree(hit, __instance);
			ArmConservation(hit);
			ApplyLumberjackDirection(hit);
		}
	}

	[HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
	public static class TreeLog_Damage_Patch
	{
		private static void Prefix(HitData hit) => ArmConservation(hit);
	}

	[HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
	public static class Attack_DoMeleeAttack_Patch
	{
		private static void Prefix()
		{
			conservationArmed = false;
			swingStartItem = null;
			swingStartDurability = 0f;

			Player player = Player.m_localPlayer;
			if (player)
			{
				swingStartItem = player.RightItem;
				swingStartDurability = swingStartItem?.m_durability ?? 0f;
			}
		}

		private static void Postfix()
		{
			if (conservationArmed && swingStartItem != null && swingStartItem.m_durability < swingStartDurability)
			{
				swingStartItem.m_durability = swingStartDurability;
			}

			conservationArmed = false;
			swingStartItem = null;
		}
	}

	[HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
	public static class RaiseSkill_XpBoost_Patch
	{
		private static void Prefix(Skills __instance, Skills.SkillType skillType, ref float factor)
		{
			if (skillType != Skills.SkillType.WoodCutting)
			{
				return;
			}

			if (Woodcutting.enableXpBoost.Value != Woodcutting.Toggle.On)
			{
				return;
			}

			Player player = Player.m_localPlayer;
			if (!player || __instance != player.GetSkills())
			{
				return;
			}

			if (__instance.GetSkillLevel(Skills.SkillType.WoodCutting) < Woodcutting.xpBoostSkillLevel.Value)
			{
				return;
			}

			factor *= Woodcutting.xpBoostMultiplier.Value;
		}
	}
}
