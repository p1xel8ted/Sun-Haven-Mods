using BepInEx.Configuration;
using BepInEx.Logging;

namespace Shared;

// One-call setup for each mod's Awake. Registers the settings-change logger for
// this mod's config, and makes sure the one-time startup summary gets scheduled.
internal static class ModLogging
{
    public static void Init(ConfigFile config, ManualLogSource logger)
    {
        SettingsChangeLogger.Register(config, new TimestampedLogger(logger));
        StartupLogger.EnsureStarted();
    }
}
