namespace UnityBuilderDiscordBot.Models;

public class UnityProjectModel
{
    public string name { get; set; }
    public string path { get; set; }
    public string unityVersion { get; set; }

    public override string ToString()
    {
        return $"[{GetType()}] name: {name}, path: {path}, unityVersion: {unityVersion}";
    }
}