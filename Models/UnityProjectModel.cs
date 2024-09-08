using Newtonsoft.Json;
using SimpleJSON;
using UnityBuilderDiscordBot.Interfaces;

namespace UnityBuilderDiscordBot.Models;

public class UnityProjectModel
{
    public string name { get; set; }
    public string path { get; set; }
    /// <summary>
    /// SymLink（软连接）路径，适用于既是工作机子又是打包机子的情况。
    /// 非必填字段。
    /// Git拉取还是会在path里进行，只有拉起Unity编辑器那个流程走的这个字段。
    /// </summary>
    public string symLinkPath { get; set; }
    public string unityVersion { get; set; }
    public string playerBuildOutput { get; set; }
    public string addressableBuildOutput { get; set; }
    public string sourceControl { get; set; }
    public string branch { get; set; }
    public string notificationChannel { get; set; }
    
    public List<IAction> deployment { get; set; }
    
    [JsonIgnore]
    public JSONNode ssh { get; set; }

    public override string ToString()
    {
        return
            $"[{GetType()}] name: {name}, path: {path}, unityVersion: {unityVersion}, playerBuildOutput: {playerBuildOutput}, addressableBuildOutput: {addressableBuildOutput}, sourceControl: {sourceControl}, branch: {branch}";
    }
}