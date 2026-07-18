using MonoMod.Utils;

namespace NoTimeToStopAndEat;

[BepInPlugin(PluginGuid, PluginName, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("p1xel8ted.sunhaven.keepalive")]
public class Plugin : BaseUnityPlugin
{
    private const string PluginGuid = "p1xel8ted.sunhaven.notimetostopandeat";
    private const string PluginName = "No Time To Stop & Eat!";
    
    internal static ConfigEntry<bool> HideFoodItemWhenEating { get; private set; }

    private void Awake()
    {
        HideFoodItemWhenEating = Config.Bind("01. General", "Hide Food Item When Eating", true, "Hide the food item when eating.");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
        Shared.ModLogging.Init(Config, Logger);
    }
    
    private void OnDestroy()
    {
        OnDisable();
    }
    
    private void OnDisable()
    {
        Logger.LogError($"Plugin {PluginName} was disabled/destroyed! Unless you are exiting the game, please install Keep Alive! - https://www.nexusmods.com/sunhaven/mods/31");
    }
}