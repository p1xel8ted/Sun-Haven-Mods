namespace CharacterEditRedux;

[BepInPlugin(PluginGuid, PluginName, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("p1xel8ted.sunhaven.keepalive")]
public class Plugin : BaseUnityPlugin
{
    private const string PluginGuid = "p1xel8ted.sunhaven.charactereditredux";
    private const string PluginName = "Character Edit Redux";
    internal static ManualLogSource Log { get; private set; }

    private void Awake()
    {
        Log = Logger;

        Utils.LoadTexture(Assembly.GetExecutingAssembly(), $"{GetType().Namespace}.assets.mouseover.png", ref Patches.MouseOverImage);
        Utils.LoadTexture(Assembly.GetExecutingAssembly(), $"{GetType().Namespace}.assets.default.png", ref Patches.DefaultImage);

        Config.Bind("01. General Settings", "Open Save Directory", true,
            new ConfigDescription("Open Save Directory", null,
                new ConfigurationManagerAttributes
                    { Order = 1, HideDefaultButton = true, CustomDrawer = OpenSaveDir }));

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
        Shared.ModLogging.Init(Config, Logger);
    }

    private void OnDestroy()
    {
        OnDisable();
    }

    private void OnDisable()
    {
        Log.LogError($"Plugin {PluginName} was disabled/destroyed! Unless you are exiting the game, please install Keep Alive! - https://www.nexusmods.com/sunhaven/mods/31");
    }

    private static void OpenSaveDir(ConfigEntryBase obj)
    {
        var button = GUILayout.Button("Open Save Directory", GUILayout.ExpandWidth(true));
        if (button)
        {
           var path = Path.Combine(Application.persistentDataPath, "Saves");
              if (Directory.Exists(path))
              {
                Process.Start(new ProcessStartInfo
                {
                     FileName = path,
                     UseShellExecute = true,
                     Verb = "open"
                });
              }
              else
              {
                Log.LogError($"Save directory does not exist: {path}");
              }
        }
    }
}