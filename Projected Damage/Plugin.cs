using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

using InkboundModEnabler.Util;
using ShinyShoe;
using ShinyShoe.Ares; // Contains EntityHandle
using System;
using ShinyShoe.SharedDataLoader;
using UnityEngine;
using TMPro;

namespace Projected_Damage
{
	[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
	[CosmeticPlugin]
	public class ProjectedDamagePlugin : BaseUnityPlugin
	{
		public const string PLUGIN_GUID = "Projected_Damage";
		public const string PLUGIN_NAME = "Projected Damage";
		public const string PLUGIN_VERSION = "0.0.0";
		public static readonly Harmony HarmonyInstance = new Harmony(PLUGIN_GUID);
		internal static ManualLogSource log;
		private void Awake()
		{
			// Plugin startup logic
			log = Logger;
			HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
			Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
		}

		// Code base from ADDB 
		[HarmonyPatch(typeof(WorldUnitController))]
		public static class WorldUnitPatch
		{

			[HarmonyPatch(nameof(WorldUnitController.SetUnitContent))]
			[HarmonyPostfix]
			public static void EntityHandlePostFix( WorldUnitController __instance )
			{
				log.LogInfo($"Entity Handle: {__instance.EntityHandle}");

				log.LogInfo($"GetHPDiff: {GetHPDiffBetweenSimulation(__instance.EntityHandle)}");
			}

			public static int GetHPDiffBetweenSimulation(EntityHandle entityHandle)
			{
				var app_state = ClientApp.Inst.GetStateRo_Debug();
				var oldStats = UnitCombatDBHelper.GetHpEnergyShieldStats(entityHandle, app_state.GetWorldClientRo().GetWorldStateRo().GetUnitCombatDBRo());
				var newStats = UnitCombatDBHelper.GetHpEnergyShieldStats(entityHandle, app_state.GetCombatPreviewSystemStateRo().SimulatedTurnWorldStateCopy.GetUnitCombatDBRo());
				var totalOld = oldStats.hp + oldStats.energyShield;
				var totalNew = newStats.hp + newStats.energyShield;
				return totalOld - totalNew;
			}
		}



		[HarmonyPatch(typeof(HpBarUI), nameof(HpBarUI.Set))]
		public static class HpBarUI_Sandbox
		{
			public static GameObject myLabelObject;

			[HarmonyPrefix]
			public static void HpBarUI_before_Set(HpEnergyShield currentHpEnergyShield, HpEnergyShield predictedHpEnergyShield, HpBarUI __instance)
			{

				int delta_hp = currentHpEnergyShield.hp - predictedHpEnergyShield.hp;
				int delta_shield = currentHpEnergyShield.energyShield - currentHpEnergyShield.energyShield;
				

				log.LogInfo($"delta_hp: {delta_hp}");
				log.LogInfo($"delta_shield: {delta_shield}");

				// check if Projected Damage Label exists
				Transform HpBarUIchildren = __instance.GetComponentInChildren<Transform>();

				bool labelFound = false;

				foreach ( Transform child in HpBarUIchildren )
				{
					if ( child.name == "Projected Damage Label")
					{
						labelFound = true;
						child.GetComponent<TextMeshProUGUI>().text = $"hp: {delta_hp} shield: {delta_shield}";
						break;
					}
				}

				if ( !labelFound) 
				{
					myLabelObject = new("Projected Damage Label", typeof(RectTransform), typeof(TextMeshProUGUI));
					myLabelObject.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
					myLabelObject.transform.SetParent(__instance.transform, false);
					myLabelObject.GetComponent<TextMeshProUGUI>().text = $"hp: {delta_hp} shield: {delta_shield}";
				}

			}


		}

		[HarmonyPatch(typeof(ShinyShoe.AbilityHandScreen), nameof(ShinyShoe.AbilityHandScreen.IsTargeting))]
		public static class AbilityHand_Sandbox
		{

			[HarmonyPostfix]
			public static void IsTargeting(ref bool __result)
			{

				log.LogInfo($"IsTargeting: {__result}");

				if( __result == true )
				{
					log.LogInfo("DEBUG IsTargeting");
				}

			}

		}


		[HarmonyPatch(typeof(ShinyShoe.SharedDataLoader.AssetLibrary), nameof(ShinyShoe.SharedDataLoader.AssetLibrary.Initialize))]
		public static class AssetLibrary_Example
		{
			
			[HarmonyPostfix]
			static void AssetLibrary()
			{
				log.LogInfo("DEBUG Asset Library Loaded");
			}

		}
	}
}