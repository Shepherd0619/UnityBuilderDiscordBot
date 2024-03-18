using SimpleJSON;

namespace UnityBuilderDiscordBot.Utilities;

public static class ConfigurationUtility
{
    public static JSONNode Configuration;

    public static void Initialize()
    {
        Configuration = JSONNode.Parse(File.ReadAllText("appsettings.json"));
    }
}