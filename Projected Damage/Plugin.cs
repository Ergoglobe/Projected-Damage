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
			public static GameObject projected_delta_hp;
			public static GameObject projected_delta_shield;


			public static void Projected_Delta_HP_Colors( int delta_hp )
			{

				if (delta_hp == 0) // 0 show white
				{
					projected_delta_hp.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 1f);
				}
				else if ((delta_hp * -1) < 0) // taking damage
				{
					projected_delta_hp.GetComponent<TextMeshProUGUI>().color = new Color(1.000f, .478f, .431f, 1f);
				}
				else if ((delta_hp * -1) > 0) // healing
				{
					projected_delta_hp.GetComponent<TextMeshProUGUI>().color = new Color(.890f, 0.0f, .271f, 1f);
				}
				// TODO if dieing add one more color

			}

			public static void Projected_Delta_Shield_Colors( int delta_shield) {

				if (delta_shield == 0) // 0 show white
				{
					projected_delta_shield.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 1f);
				}
				else if ((delta_shield * -1) < 0) // taking shield damage
				{
					projected_delta_shield.GetComponent<TextMeshProUGUI>().color = new Color(.184f, .643f, 1.000f, 1f);
				}
				else if ((delta_shield * -1) > 0) // gaining shield
				{
					projected_delta_shield.GetComponent<TextMeshProUGUI>().color = new Color(.125f, .345f, .671f, 1f);
				}
				// TODO if dieing add one more color

			}

			[HarmonyPrefix]
			public static void HpBarUI_before_Set(HpEnergyShield currentHpEnergyShield, HpEnergyShield predictedHpEnergyShield, HpBarUI __instance)
			{

				int delta_hp = currentHpEnergyShield.hp - predictedHpEnergyShield.hp;
				int delta_shield = currentHpEnergyShield.energyShield - currentHpEnergyShield.energyShield;
				

				// log.LogInfo($"delta_hp: {delta_hp}");
				// log.LogInfo($"delta_shield: {delta_shield}");

				// Check hierarchy and dont display on party display
				// WorldUI/PartyScreen/CharacterPartyDisplay 1/Player Info/Banner/In Run Stats/HP MP Container/

				// Check hierarchy and dont display on bottom bar OR modify label to place it elsewhere
				// WorldUI/PlayerHpManaDisplayScreen/HP Area/

				// if hierarchy matches the following, display it
				// WorldUI/OverheadScreen/OverheadUIs/OverheadUI(Clone)/OverheadHpDisplay/Container Medium/

				if( __instance.transform.parent.name == "Container Medium")
				{


					// check if Projected Damage Label exists
					Transform HpBarUIchildren = __instance.GetComponentInChildren<Transform>();

					bool labelFound = false;

					foreach (Transform child in HpBarUIchildren)
					{
						if (child.name == "Projected Delta HP")
						{
							labelFound = true;
							child.GetComponent<TextMeshProUGUI>().text = $"{delta_hp * -1} HP";
							Projected_Delta_HP_Colors(delta_hp);
						}
						if (child.name == "Projected Delta Shield")
						{
							labelFound = true;
							child.GetComponent<TextMeshProUGUI>().text = $"{delta_shield * -1} Shield";
							Projected_Delta_Shield_Colors(delta_shield);
						}
					}

					// Instantiate the labels if not found
					if (!labelFound)
					{
						// TODO: Instantiate label from another gameobject to copy the style
						// myLabelObject = new("Projected Damage Label", typeof(RectTransform), typeof(TextMeshProUGUI));

						projected_delta_hp = GameObject.Instantiate(__instance.transform.parent.Find("Enemy Name").gameObject);
						projected_delta_hp.name = "Projected Delta HP";
						projected_delta_hp.transform.localPosition = new Vector3( 0.0f, 65.0f, 0.0f);
						projected_delta_hp.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
						projected_delta_hp.transform.SetParent(__instance.transform, false);
						projected_delta_hp.GetComponent<TextMeshProUGUI>().text = $"{delta_hp*-1} HP";

						Projected_Delta_HP_Colors(delta_hp);

						projected_delta_shield = GameObject.Instantiate(__instance.transform.parent.Find("Enemy Name").gameObject);
						projected_delta_shield.name = "Projected Delta Shield";
						projected_delta_shield.transform.localPosition = new Vector3(0.0f, 75.0f, 0.0f);
						projected_delta_shield.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
						projected_delta_shield.transform.SetParent(__instance.transform, false);
						projected_delta_shield.GetComponent<TextMeshProUGUI>().text = $"{delta_shield*-1} Shield";

						Projected_Delta_Shield_Colors(delta_shield);



					}
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