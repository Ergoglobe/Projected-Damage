using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace Projected_Damage
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
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
    }
}