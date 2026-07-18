using MonoMod.Utils;

namespace CheatEnabler;

[BepInPlugin(PluginGuid, PluginName, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("p1xel8ted.sunhaven.keepalive")]
public class Plugin : BaseUnityPlugin
{
    private const string PluginGuid = "p1xel8ted.sunhaven.cheatenabler";
    private const string PluginName = "Cheat Enabler";
    internal static ManualLogSource LOG { get; private set; }
    internal static ConfigEntry<bool> Debug { get; private set; }
    
    private void Awake()
    {
        Debug = Config.Bind("01. General", "Debug", false, "Enable debug logging");
        LOG = Logger;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
        Shared.ModLogging.Init(Config, Logger);
    }
    
    private void OnDestroy()
    {
        LOG.LogError($"Plugin {PluginName} was destroyed! Unless you are exiting the game, please install Keep Alive! - https://www.nexusmods.com/sunhaven/mods/31");
    }
}