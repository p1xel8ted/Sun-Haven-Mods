using MonoMod.Utils;

namespace MoreTheMerrier;

[BepInPlugin(PluginGuid, PluginName, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("p1xel8ted.sunhaven.keepalive")]
public class Plugin : BaseUnityPlugin
{
    private const string PluginGuid = "p1xel8ted.sunhaven.morethemerrier";
    private const string PluginName = "More The Merrier!";
    internal static ManualLogSource Log { get; private set; }

    private void Awake()
    {
        Log = Logger;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
        Shared.ModLogging.Init(Config, Logger);
    }

    private void OnDisable()
    {
        Log.LogError($"Plugin {PluginName} was disabled/destroyed! Unless you are exiting the game, please install Keep Alive! - https://www.nexusmods.com/sunhaven/mods/31");
    }

    private void OnDestroy()
    {
        OnDisable();
    }
}