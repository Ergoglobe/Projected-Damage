﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

using InkboundModEnabler.Util;
using ShinyShoe;
using ShinyShoe.Ares; // Contains EntityHandle
using System;
using ShinyShoe.SharedDataLoader;

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
		public static class WorldUnit_Patch
		{
			[HarmonyPatch(nameof(WorldUnitController.SetUnitContent))]
			[HarmonyPostfix]
			public static void EntityHandlePostFix( ref EntityHandle entityHandle )
			{
				log.LogInfo($"EntityHandle: {entityHandle}");
			}


			[HarmonyPatch(typeof(ShinyShoe.SharedDataLoader.AssetLibrary), nameof(ShinyShoe.SharedDataLoader.AssetLibrary.Initialize))]
			[HarmonyPostfix]
			public static void AssetLibrary()
			{
				log.LogInfo("DEBUG Asset Library Loaded");
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
	}
}