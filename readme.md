# FishySteamworks
A Steamworks implementation for Fish-Networking.

Thank you [ETdoFresh](https://github.com/sponsors/etdofresh) for your support.


## Dependencies

Fish-Networking https://github.com/FirstGearGames/FishNet

These projects need to be installed and working before you can use this transport.
1. [Epic Online Services Plugin for Unity](https://github.com/PlayEveryWare/eos_plugin_for_unity_upm) FishyEOS relies on PlayEveryWare's EOS Plugin for Unity to communicate with the EOS API(https://dev.epicgames.com/docs/api-ref/interfaces).


## Setting Up

1. Add EOSP2P Transport component to your NetworkManager object. 
2. Set transport on either TransportManager or MultiPass Transport Component.

### As a Server
1. Authenticate with EOS Connect.
2. Specify the EOS SocketName which you would like to listen from.
3. Start Server as normal.

### As a Client
1. Authenticate with EOS Connect.
2. Specify the EOS SocketName which you would like to connect.
3. Specify the ServerProductUserId which you would like to connect.
4. Start Client as normal.

### Host
1. Follow steps from Server.
2. Start Client as normal. (A special "clientHost" will be created on server allowing to play as a client on the server)

### Testing Two Builds Locally
EOS has limitations which prevent you from connecting to yourself using the same ID. To do so, you must have two connect product user ids (but can be on the same device).
