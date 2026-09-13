using MonoMod.Utils;

namespace MoreScythesRedux;

[BepInPlugin(PluginGuid, PluginName, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("p1xel8ted.sunhaven.keepalive")]
public class Plugin : BaseUnityPlugin
{
    private const string PluginGuid = "p1xel8ted.sunhaven.morescythesredux";
    private const string PluginName = "More Scythes Redux";
    public static ManualLogSource LOG { get; private set; }

    private void Awake()
    {
        SceneManager.sceneLoaded += (_, _) =>
        {
            try
            {
                // The item database doesn't exist on the first scene load; a later load will try again.
                if (!Database.Instance) return;
                ItemHandler.CreateScytheItems();
            }
            catch (Exception e)
            {
                LOG?.LogError($"Failed to create scythes: {e}");
            }
        };
        
        LOG = Logger;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
        Shared.ModLogging.Init(Config, Logger);
    }
    
    private void OnDestroy()
    {
        OnDisable();
    }
    
    private void OnDisable()
    {
        LOG.LogError($"Plugin {PluginName} was disabled/destroyed! Unless you are exiting the game, please install Keep Alive! - https://www.nexusmods.com/sunhaven/mods/31");
    }

}