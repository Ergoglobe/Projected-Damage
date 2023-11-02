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
		public const string PLUGIN_VERSION = "0.5.0";
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
			public static void EntityHandlePostFix(WorldUnitController __instance)
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
			public static GameObject projected_delta_total;


			public static void Projected_Delta_HP_Colors(int delta_hp, Transform transform)
			{

				if (delta_hp == 0) // 0 then set alpha to 0
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
				}
				else if ((delta_hp * -1) < 0) // taking damage same as incoming damage color
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(1.000f, .478f, .431f, 1f);
				}
				else if ((delta_hp * -1) > 0) // healing green
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(.353f, 0.957f, .486f, 1f);
				}
				// TODO if dieing add one more color

			}

			public static void Projected_Delta_Shield_Colors(int delta_shield, Transform transform)
			{

				if (delta_shield == 0) // 0 then set alpha to 0
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
				}
				else if ((delta_shield * -1) < 0) // taking shield damage
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(.184f, .643f, 1.000f, 1f);
				}
				else if ((delta_shield * -1) > 0) // gaining shield
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(.125f, .345f, .671f, 1f);
				}
				// TODO if dieing add one more color

			}

			public static void Projected_Delta_Total_Colors(int delta_total, Transform transform)
			{

				if (delta_total == 0) // 0 then set alpha to 0
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
				}
				else if ((delta_total * -1) < 0) // taking damage same as incoming damage color
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 1f); // old red color 1.000f, .478f, .431f
				}
				else if ((delta_total * -1) > 0) // healing green
				{
					transform.GetComponent<TextMeshProUGUI>().color = new Color(.353f, 0.957f, .486f, 1f);
				}
				// TODO if dieing add one more color

			}

			private static string Format_Damage_Number(int num)
			{


				if (num >= 1000000000)
					return (num / 1000000000D).ToString("0.##") + "B"; // Will this ever be necessary?
				if (num >= 1000000)
					return (num / 1000000D).ToString("0.##") + "M";
				if (num >= 1000)
					return (num / 1000D).ToString("0.##") + "K";

				if (num < 0)
				{
					// if the unit will be healing add a + sign to the number
					return "+" + (num*-1).ToString("#,0");
				}
				else
				{
					return num.ToString("#,0");
				}
			
			}


			[HarmonyPrefix]
			public static void HpBarUI_before_Set(HpEnergyShield currentHpEnergyShield, HpEnergyShield predictedHpEnergyShield, HpBarUI __instance)
			{

				int delta_hp = currentHpEnergyShield.hp - predictedHpEnergyShield.hp;
				int delta_shield = currentHpEnergyShield.energyShield - currentHpEnergyShield.energyShield;
				int delta_total = delta_hp + delta_shield;


				// Check hierarchy and dont display on party display
				// WorldUI/PartyScreen/CharacterPartyDisplay 1/Player Info/Banner/In Run Stats/HP MP Container/

				// Check hierarchy and dont display on bottom bar OR modify label to place it elsewhere
				// WorldUI/PlayerHpManaDisplayScreen/HP Area/

				// if hierarchy matches the following, display it
				// WorldUI/OverheadScreen/OverheadUIs/OverheadUI(Clone)/OverheadHpDisplay/Container Medium/
				if (__instance.transform.parent.name == "Container Medium")
				{


					// check if Projected Damage Label exists
					Transform HpBarUIchildren = __instance.GetComponentInChildren<Transform>();

					bool labelFound = false;

					foreach (Transform child in HpBarUIchildren)
					{
/*						if (child.name == "Projected Delta HP")
						{
							labelFound = true;
							// If delta_hp <= 0 then show 
							child.GetComponent<TextMeshProUGUI>().text = $"{(delta_hp * -1 <= 0 ? delta_hp * -1 : "+" + (delta_hp * -1))} HP";
							Projected_Delta_HP_Colors(delta_hp, child);
						}
						if (child.name == "Projected Delta Shield")
						{
							labelFound = true;
							child.GetComponent<TextMeshProUGUI>().text = $"{(delta_shield * -1 <= 0 ? delta_shield * -1 : "+" + (delta_shield * -1))} Shield";
							Projected_Delta_Shield_Colors(delta_shield, child);
						}*/
						if (child.name == "Projected Delta Total")
						{
							labelFound = true;
							child.GetComponent<TextMeshProUGUI>().text = Format_Damage_Number(delta_total);
							Projected_Delta_Total_Colors(delta_total, child);
						}
					}

					// Instantiate the labels if not found
					if (!labelFound)
					{
						// TODO: Instantiate label from another gameobject to copy the style
						// myLabelObject = new("Projected Damage Label", typeof(RectTransform), typeof(TextMeshProUGUI));
/*
						projected_delta_hp = GameObject.Instantiate(__instance.transform.parent.Find("Enemy Name").gameObject);
						projected_delta_hp.name = "Projected Delta HP";
						projected_delta_hp.transform.localPosition = new Vector3(0.0f, 65.0f, 0.0f);
						projected_delta_hp.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
						projected_delta_hp.transform.SetParent(__instance.transform, false);
						projected_delta_hp.GetComponent<TextMeshProUGUI>().text = $"{delta_hp * -1} HP";

						Projected_Delta_HP_Colors(delta_hp, projected_delta_hp.transform);

						projected_delta_shield = GameObject.Instantiate(__instance.transform.parent.Find("Enemy Name").gameObject);
						projected_delta_shield.name = "Projected Delta Shield";
						projected_delta_shield.transform.localPosition = new Vector3(0.0f, 85.0f, 0.0f);
						projected_delta_shield.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
						projected_delta_shield.transform.SetParent(__instance.transform, false);
						projected_delta_shield.GetComponent<TextMeshProUGUI>().text = $"{delta_shield * -1} Shield";

						Projected_Delta_Shield_Colors(delta_shield, projected_delta_shield.transform);*/

						projected_delta_total = GameObject.Instantiate(__instance.transform.parent.Find("HP Text").gameObject);
						projected_delta_total.name = "Projected Delta Total";

						// right of health bar ( 95 0 0 )
						// projected_delta_total.transform.localPosition = new Vector3(95.0f, 0.0f, 0.0f);

						// between incoming damage and health ( -15 24 0 )
						projected_delta_total.transform.localPosition = new Vector3(-15.0f, 24.0f, 0.0f);

						projected_delta_total.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
						projected_delta_total.transform.SetParent(__instance.transform, false);
						projected_delta_total.GetComponent<TextMeshProUGUI>().text = Format_Damage_Number( delta_total );

						Projected_Delta_Shield_Colors(delta_total, projected_delta_total.transform);




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

				if (__result == true)
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