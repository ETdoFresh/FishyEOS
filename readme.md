# !!Still under development! Currently in Pre-Production!!
But posting it here in case you want to help try it out and help me test. :smile:  
If there are issues you want to report, let me know either here in [Issues](https://github.com/ETdoFresh/FishyEOS/issues).  
Or ask me [ETdoFresh] in [FirstGearGames Discord](https://discord.gg/Ta9HgDh4Hj).

# FishyEOS
An Epic Online Services (EOS) implementation for Fish-Networking.

Thank you [ETdoFresh](https://github.com/sponsors/etdofresh) for your support.


## Dependencies

1. Fish-Networking https://github.com/FirstGearGames/FishNet
2. [Epic Online Services Plugin for Unity](https://github.com/PlayEveryWare/eos_plugin_for_unity_upm) FishyEOS relies on PlayEveryWare's EOS Plugin for Unity to communicate with the [EOS API](https://dev.epicgames.com/docs/api-ref/interfaces).  
    a. [Configuring the Plugin in the EOS Plugin for Unity Readme](https://github.com/PlayEveryWare/eos_plugin_for_unity#configuring-the-plugin) are useful directions if you need to setup the plugin in your project.


## Installation

### As a git Unity Package

1. Open Unity Package Manager. **Unity > Window > Package Manager**
2. Add a package from git URL. **Package Manager > + > Add package from git URL...**
3. Enter this url: `https://github.com/ETdoFresh/FishyEOS.git?path=/FishNet/Plugins/FishyEOS`

### As a local Unity Package

You are not required to use git to download the package. Alternatively, you can download this repository to your **Assets** folder or **Packages** folder. If following this method, it is recommended to download to `{ProjectDirectory}\Packages\com.etdofresh.fishyeos`.


## Setting Up

1. Add an EOSManager Component somewhere in your scene. Probably best on the **NetworkManager** GameObject.
2. Add FishyEOS Transport component to **NetworkManager** GameObject. 
3. Set transport varaible on the **TransportManager** Component to FishyEOS.

### As a Server
1. Before starting Server, authenticate with EOS Connect.  
   For Example: `EOSManager.Instance.StartConnectLoginWithDeviceToken(...)`
2. Specify the EOS SocketName which you would like to listen from.
3. Start Server as normal.  
   `NetworkManager.ServerManager.StartConnection()`

### As a Client
1. Authenticate with EOS Connect.
   For Example: `EOSManager.Instance.StartConnectLoginWithDeviceToken(...)`
2. Specify the EOS SocketName which you would like to connect.
3. Specify the ServerProductUserId which you would like to connect.
4. Start Client as normal.  
   `NetworkManager.ClientManager.StartConnection()`

### As a Host
1. Follow steps from [As a Server](#as-a-server).
2. Start Client as normal.  
   `NetworkManager.ClientManager.StartConnection()`  
   (A special "clientHost" will be created on server allowing to play as a client on the server)

### Testing Two Builds Locally
EOS has limitations which prevent you from connecting to yourself using the same ID. To do so, you must have two Epic Connect Product User IDs. You can use the same device to log into both if they are different sign in providers (I often use EpicAccountId and DeviceToken).
