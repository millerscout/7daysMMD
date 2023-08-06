using System.Reflection;

namespace RainCollector.Harmony
{
    /// <summary>
    /// Harmony initializer for the Rain Collector mod.
    /// </summary>
    public class RainCollector : IModApi
    {
        /// <summary>
        /// Whether or not to print debug messages to the console.
        /// </summary>
        public static bool Debug => GamePrefs.GetBool(EnumGamePrefs.DebugMenuEnabled);

        /// <summary>
        /// Initializes the mod.
        /// </summary>
        /// <param name="_modInstance"></param>
        public void InitMod(Mod _modInstance)
        {
            var harmony = new HarmonyLib.Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Convenience method to log debug messages consistently.
        /// </summary>
        /// <param name="msg"></param>
        public static void DebugLog(string msg)
        {
            if (Debug)
            {
                Log.Out($"RainCollector: {msg}");
            }
        }
    }
}
