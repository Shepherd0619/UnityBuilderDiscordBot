# UnityBuilderDiscordBot
This is a Discord DevOps bot helps everyone (no matter whether he or she is programmer or not) build the Unity game executables and hot updates.

Developed by Shepherd Zhu (@Shepherd0619). **Under BSD-Clause-3 license.**

To contribute, please submit a pull request.

PLEASE NOTICE THAT THIS BOT IS JUST A TEMPLATE. YOU SHOULD FORK IT AND MODIFY IT TO SUIT YOUR PROJECT'S NEEDS.

For the hot update part, please refer to [JenkinsBuildUnity](https://github.com/Shepherd0619/JenkinsBuildUnity).

## Features
1. Build Unity executables and hot updates via Slash Command.
2. Upload hot updates via SFTP. **(Work in progress)**

## How to use
### Set up Discord App
1. Visit https://discord.com/developers/applications and create an application.

![alt text](image.png)

2. Generate OAuth2 URL (with permissions assigned) and visit it to invite the bot into your server.

![alt text](image-1.png)

3. Enable all Privileged Gateway Intents.

![alt text](image-2.png)

4. Generate appsettings.json.

```json
{
  "Discord": {
    "token": "your-bot-token",
    "channel": "your-channel-id",
    "logChannel": "your-bot-log-channel-id (leave it empty if you dont want log. )"
  },
  "Unity": [
    {
      "2022.3.14f1": "E:\\Program Files\\Unity 2022.3.14f1\\Editor\\Unity.exe"
    }
  ],
  "Projects": [
    {
      "name": "example",
      "path": "D:\\Unity_Projects\\example",
      "unityVersion": "2022.3.14f1"
    }
  ]
}
```

5. Run UnityBuilderDiscordBot.exe (for Windows) or UnityBuilderDiscordBot.dll (for Linux).

6. Type "/" to see available commands.