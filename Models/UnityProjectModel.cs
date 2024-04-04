using SimpleJSON;

namespace UnityBuilderDiscordBot.Models;

public class UnityProjectModel
{
    public string name { get; set; }
    public string path { get; set; }
    public string unityVersion { get; set; }
    public string playerBuildOutput { get; set; }
    public string addressableBuildOutput { get; set; }
    public string sourceControl { get; set; }
    public string branch { get; set; }
    
    public override string ToString()
    {
        return
            $"[{GetType()}] name: {name}, path: {path}, unityVersion: {unityVersion}, playerBuildOutput: {playerBuildOutput}, addressableBuildOutput: {addressableBuildOutput}, sourceControl: {sourceControl}, branch: {branch}";
    }
}