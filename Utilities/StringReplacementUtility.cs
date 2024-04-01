using UnityBuilderDiscordBot.Models;

namespace UnityBuilderDiscordBot.Utilities;

public class StringReplacementUtility
{
    // TODO: 抽象成接口
    
    public StringReplacementUtility(UnityProjectModel project)
    {
        _registeredKeywordsAndResults["{projectName}"] = project.name;
        _registeredKeywordsAndResults["{projectPath}"] = project.path;
        _registeredKeywordsAndResults["{playerBuildOutput}"] = project.playerBuildOutput;
        _registeredKeywordsAndResults["{addressableBuildOutput}"] = project.addressableBuildOutput;
    }

    private readonly Dictionary<string, string> _registeredKeywordsAndResults = new Dictionary<string, string>()
    {
        {"{projectName}", ""},
        {"{projectPath}", ""},
        {"{playerBuildOutput}", ""},
        {"{addressableBuildOutput}", ""}
    };

    public string Replace(string input)
    {
        foreach (var kvp in _registeredKeywordsAndResults)
        {
            input = input.Replace(kvp.Key, kvp.Value);
        }

        return input;
    }
}