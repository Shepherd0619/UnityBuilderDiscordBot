using SimpleJSON;
using UnityBuilderDiscordBot.Utilities;

namespace UnityBuilderDiscordBot.Controllers;

public static class UnityEditorController
{
    // 打包参数
    // -nographics -batchmode -quit -buildWindowsPlayer "${WORKSPACE}\Build\Windows\${BUILD_NUMBER}\output\your_game.exe"

    public static Dictionary<string, string> EditorInstallations;
    public static string ProjectPath;
    public static string UnityVersion;
    
    public static bool Initialize()
    {
        EditorInstallations = new Dictionary<string, string>();
        foreach (JSONNode node in ConfigurationUtility.Configuration["Unity"].AsArray)
        {
            foreach (var kvp in node)
            {
                EditorInstallations.Add(kvp.Key, kvp.Value.Value);
                Console.WriteLine($"[UnityEditorController] Found a Unity Editor installation! {kvp.Key}, {kvp.Value}");
            }
        }

        ProjectPath = ConfigurationUtility.Configuration["Project"]["Path"];
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            Console.WriteLine($"[UnityEditorController] ProjectPath is empty!");
            return false;
        }
        if (!Directory.Exists(ProjectPath))
        {
            Console.WriteLine($"[UnityEditorController] Project {Directory.GetParent(ProjectPath).Name} does not exist!");
            return false;
        }
        
        UnityVersion = ConfigurationUtility.Configuration["Project"]["unityVersion"];
        if (string.IsNullOrWhiteSpace(UnityVersion) || !EditorInstallations.ContainsKey(UnityVersion))
        {
            Console.WriteLine(
                $"[UnityEditorController] Project {Directory.GetParent(ProjectPath).Name} requires Unity Editor Installation of {UnityVersion}!");
            return false;
        }
        
        return true;
    }
}