# FishyEOS
An Epic Online Services (EOS) implementation for Fish-Networking.

Thank you [ETdoFresh](https://github.com/sponsors/etdofresh) for your support.

If you have further questions, come find us in the [FirstGearGames Discord](https://discord.gg/Ta9HgDh4Hj)!


## Dependencies

1. Fish-Networking https://github.com/FirstGearGames/FishNet
2. PlayEveryWare/eos_plugin_for_unity https://github.com/PlayEveryWare/eos_plugin_for_unity
   1. Use `git clone https://github.com/PlayEveryWare/eos_plugin_for_unity` when downloading **eos_plugin_for_unity**. If you download as zip, you may have errors with the included DLLs.
   2. Copy **Assets/Plugins** from repository to your project's **Assets/Plugins** folder.
   3. Requires Unity 2020.1.11f1 or newer.
   4. Requires Unity Package: `com.unity.editorcoroutines`
   5. [Configure the plugin](https://github.com/PlayEveryWare/eos_plugin_for_unity#steps) with your Epic Online Services project information.


## Installation

### Unity Package (git url)

1. Open Unity Package Manager. **Unity > Window > Package Manager**
2. Add a package from git URL. **Package Manager > + > Add package from git URL...**
3. Enter this url: `https://github.com/ETdoFresh/FishyEOS.git?path=/FishNet/Plugins/FishyEOS`

### Direct Download (Assets Folder)

After installing FishNet, download this repository directly to your **Assets** folder.

### Direct Download (Packages Folder)

Copy the **FishNet/Plugins/FishyEOS** folder to your **Packages** folder.


## Setting Up

1. Add FishyEOS Transport component to **NetworkManager** GameObject.
2. Set transport variable on the **TransportManager** Component to FishyEOS.

### As a Server
1. Specify the EOS SocketName which you would like to listen from.  
   Example `FishyEOS`
2. Start Server as normal.  
   `NetworkManager.ServerManager.StartConnection()`

### As a Client
1. Specify the EOS SocketName which you would like to connect.  
   Example `FishyEOS`
2. Specify the ServerProductUserId which you would like to connect.  
   Example `0002780586644887316944b9a41246b3`
3. Start Client as normal.  
   `NetworkManager.ClientManager.StartConnection()`

### As a Host (aka Server + Client)
1. Follow steps from [As a Server](#as-a-server).
2. Start Client as normal.  
   `NetworkManager.ClientManager.StartConnection()`  
   (A special "clientHost" will be created on server allowing to play as a client on the server)

### Testing Two Builds Locally
EOS has limitations which prevent you from connecting to yourself using the same ID. To do so, you must have two Epic Connect Product User IDs. You can use the same device to log into both if they are different sign in providers (I often use Developer and DeviceCode).

## Supported Platforms
FishyEOS relies on the [PlayEveryWare/eos_plugin_for_unity](https://github.com/PlayEveryWare/eos_plugin_for_unity) plugin to function. The code provided in this repository only uses the calls from this plugin to function. Please refer to the plugin's documentation for [supported platforms](https://github.com/PlayEveryWare/eos_plugin_for_unity#supported-platforms).

### Non-Windows Support

As of **PlayEveryWare/eos_plugin_for_unity@2.1.5**. Android, IOS, and MacOS support is in preview. However, I had to make some changes to the plugin to get it to work on these preview platforms. Here are those changes:

1. For all non-windows platforms, set the following **Edit > Project Settings > Player Settings > Other Settings**
   1. Add `EOS_PREVIEW_PLATFORM` to the list.
   2. Press the **Apply** button.
2. For Android specifically, also set the following **Edit > Project Settings > Player Settings**
   1. Other Settings > Identification > Minimum API Level = 23 or higher
   2. Publishing Settings > Uncheck Custom Main Gradle Template
      1. If prompter to update gradeTemplate.properties, click **Yes**.
2. Do not add **EOSManager** to your scene.
   1. Instead only rely on FishyEOS provided `EOS.GetManager()` and `EOS.GetPlatformInterface()`.
   2. These functions detect if you are on windows and calls platform specific initialization function.
3. If you have your EOS configured, you will have a config file located in **Assets/StreamingAssets/EOS/EpicOnlineServicesConfig.json**
   1. For Android, Duplicate **Assets/StreamingAssets/EOS/**`EpicOnlineServicesConfig.json` to `eos_android_config.json`
   2. For MacOS, Duplicate **Assets/StreamingAssets/EOS/**`EpicOnlineServicesConfig.json` to `eos_macos_config.json`
4. Android Builds require Android Build Tools 30.0.3. If you are using Unity 2020, you probably will need to update your build tools. (Otherwise, I think you are fine?)
   ```shell
   rem Run an administrator privledged command prompt/terminal
   cd \Program Files\Unity\Hub\Editor\2020.3.33f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\tools\bin
   sdkmanager.bat --install build-tools;30.0.3
   ```
5. If you are trying to use MacOS preview on M1, here's how I got it to work: https://github.com/PlayEveryWare/eos_plugin_for_unity/issues/141#issuecomment-1285576199 