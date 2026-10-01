using HarmonyLib;
using oomtm450PuckMod_Template.Configs;
using oomtm450PuckMod_Template.SystemFunc;
using SingularityGroup.HotReload;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Netcode;

namespace oomtm450PuckMod_Template {
    /// <summary>
    /// Class containing the main code for the Template patch.
    /// </summary>
    public class Template : IPuckPlugin {
        #region Constants
        /// <summary>
        /// Const string, version of the mod.
        /// </summary>
        private const string MOD_VERSION = "0.1.0DEV";

        /// <summary>
        /// ReadOnlyCollection of string, last released versions of the mod.
        /// </summary>
        private static readonly ReadOnlyCollection<string> OLD_MOD_VERSIONS = new ReadOnlyCollection<string>(new List<string> {
            "0.0.0",
        });

        /// <summary>
        /// ReadOnlyCollection of string, collection of datanames to not log.
        /// </summary>
        private static readonly ReadOnlyCollection<string> DATA_NAMES_TO_IGNORE = new ReadOnlyCollection<string>(new List<string> {
        });
        #endregion

        #region Fields
        /// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);

        /// <summary>
        /// Bool, true if the mod has been patched in.
        /// </summary>
        private static bool _harmonyPatched = false;

        /// <summary>
        /// Bool, true if the mod has registered with the named message handler for server/client communication.
        /// </summary>
        private static bool _hasRegisteredWithNamedMessageHandler = false;

        /// <summary>
        /// LockDictionary of ulong and DateTime, last time a mod out of date message was sent to a client (ulong clientId).
        /// </summary>
        private static readonly LockDictionary<ulong, DateTime> _sentOutOfDateMessage = new LockDictionary<ulong, DateTime>();

        #region Client-sided Fields
        /// <summary>
        /// DateTime, last time client asked the server for startup data.
        /// </summary>
        private static DateTime _lastDateTimeAskStartupData = DateTime.MinValue;

        /// <summary>
        /// Bool, true if the server has responded and sent the startup data.
        /// </summary>
        private static bool _serverHasResponded = false;

        /// <summary>
        /// Bool, true if the client asked to be warned because of versionning problems.
        /// </summary>
        private static bool _askForModOutOfDateWarning = false;

        /// <summary>
        /// Bool, true if the client needs to notify the user that the server is running an out of date version of the mod.
        /// </summary>
        private static bool _addServerModVersionOutOfDateMessage = false;

        /// <summary>
        /// Int, number of time client asked the server for startup data.
        /// </summary>
        private static int _askServerForStartupDataCount = 0;
        #endregion
        #endregion

        #region Properties
        /// <summary>
        /// ServerConfig, config set and sent by the server.
        /// </summary>
        internal static Configs.ServerConfig ServerConfig { get; set; } = new Configs.ServerConfig();

        /// <summary>
        /// LockList of string, system chat messages to send next frame.
        /// </summary>
        internal static LockList<string> SystemChatMessages { get; } = new LockList<string>();

        /// <summary>
        /// LockList of list of string, system data to send to all next frame.
        /// </summary>
        internal static LockList<List<string>> DataToSendToAll { get; } = new LockList<List<string>>();

        #region Client-sided Properties
        /// <summary>
        /// ClientConfig, config set by the client.
        /// </summary>
        internal static Configs.ClientConfig ClientConfig { get; set; } = new Configs.ClientConfig();
        #endregion
        #endregion

        /// <summary>
        /// Class that patches the Update event from PhysicsManager.
        /// </summary>
        [HarmonyPatch(typeof(PhysicsManager), "Update")]
        public class PhysicsManager_Update_ClientPatch {
            [HarmonyPostfix]
            public static void Postfix() {
                try {
                    // If this is the server, do not use the patch.
                    if (ServerFunc.IsDedicatedServer() || !NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton == null || NetworkManager.Singleton.CustomMessagingManager == null)
                        return;

                    if (!_hasRegisteredWithNamedMessageHandler || !_serverHasResponded) {
                        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(Constants.FROM_SERVER_TO_CLIENT, ReceiveData);
                        _hasRegisteredWithNamedMessageHandler = true;

                        DateTime now = DateTime.UtcNow;
                        if (_lastDateTimeAskStartupData + TimeSpan.FromSeconds(5) < now && _askServerForStartupDataCount++ < 12) {
                            _lastDateTimeAskStartupData = now;
                            NetworkCommunication.SendData(Constants.ASK_SERVER_FOR_STARTUP_DATA, "1", NetworkManager.ServerClientId, Constants.FROM_CLIENT_TO_SERVER, ClientConfig);
                        }

                        return;
                    }
                    else if (_askForModOutOfDateWarning) {
                        _askForModOutOfDateWarning = false;
                        NetworkCommunication.SendData(Constants.MOD_NAME + "_kick", "1", NetworkManager.ServerClientId, Constants.FROM_CLIENT_TO_SERVER, ClientConfig);
                    }
                    else if (_addServerModVersionOutOfDateMessage) {
                        _addServerModVersionOutOfDateMessage = false;
                        SystemFunc.SystemFunc.AddClientChatMessage($"Server's {Constants.WORKSHOP_MOD_NAME} mod is out of date. Some functionalities might not work properly.");
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(PhysicsManager_Update_ClientPatch)} Postfix().\n{ex}", ClientConfig ?? new ClientConfig());
                }
            }
        }

        /// <summary>
        /// Class that patches the Update event from PhysicsManager.
        /// </summary>
        [HarmonyPatch(typeof(PhysicsManager), "Update")]
        public class PhysicsManager_Update_Patch {
            [HarmonyPostfix]
            public static void Postfix() {
                // If this is not the server or game is not started, do not use the patch.
                if (!ServerFunc.IsDedicatedServer() || PlayerManager.Instance == null || PuckManager.Instance == null)
                    return;

                try {
                    if (SystemChatMessages.Count != 0) {
                        List<string> systemChatMessages = new List<string>(SystemChatMessages);
                        SystemChatMessages.Clear();

                        foreach (string message in systemChatMessages)
                            ChatManager.Instance.Server_BroadcastChatMessage(message);
                    }

                    if (DataToSendToAll.Count != 0) {
                        List<List<string>> dataToSendToAll = new List<List<string>>(DataToSendToAll);
                        DataToSendToAll.Clear();

                        foreach (List<string> data in dataToSendToAll)
                            NetworkCommunication.SendDataToAll(data[0], data[1], data[2], ServerConfig);
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(PhysicsManager_Update_Patch)} Postfix().\n{ex}", ServerConfig);
                }
            }
        }

        /*/// <summary>
        /// Class that patches the Event_Client_OnPositionSelectClickPosition event from PlayerPositionManagerController.
        /// </summary>
        [HarmonyPatch(typeof(PlayerPositionManagerController), "Event_Client_OnPositionSelectClickPosition")]
        public class PlayerPositionManagerControllerPatch {
            /// <summary>
            /// Prefix patch function to check if the player is authorized to claim the selected position.
            /// </summary>
            /// <param name="message">Dictionary of string and object, content of the event.</param>
            /// <returns>Bool, true if the user is authorized.</returns>
            [HarmonyPrefix]
            public static bool Prefix(Dictionary<string, object> message) {
                // If this is the server or the config was not sent by server (mod not installed on the server ?), do not use the patch.
                if (ServerFunc.IsDedicatedServer() || !_serverHasResponded)
                    return true;

                Logging.Log("Event_Client_OnPositionSelectClickPosition", ClientConfig);

                // From this point on to the end of the function, this is old custom code that is left for example. //
                PlayerPosition currentPPosition = (PlayerPosition)message["playerPosition"];

                // Goalie bypass.
                if (PlayerFunc.IsGoalie(currentPPosition, false))
                    return true;

                // Admin bypass.
                if (ServerConfig.AdminBypass && PlayerFunc.IsAdmin(ServerConfig, ClientConfig))
                    return true;

                // Get blue team infos.
                bool hasBlueGoalie = false;
                int numberOfBlueSkaters = 0;
                foreach (PlayerPosition pPosition in PlayerPositionManager.Instance.BluePositions) {
                    if (PlayerFunc.IsAttacker(pPosition))
                        numberOfBlueSkaters++;
                    if (PlayerFunc.IsGoalie(pPosition))
                        hasBlueGoalie = true;
                }

                // Get red team infos.
                bool hasRedGoalie = false;
                int numberOfRedSkaters = 0;
                foreach (PlayerPosition pPosition in PlayerPositionManager.Instance.RedPositions) {
                    if (PlayerFunc.IsAttacker(pPosition))
                        numberOfRedSkaters++;
                    if (PlayerFunc.IsGoalie(pPosition))
                        hasRedGoalie = true;
                }

                int maxNumberOfSkaters = ServerConfig.MaxNumberOfSkaters;
                bool teamBalancing = TeamBalancing(hasBlueGoalie, hasRedGoalie);

                // Get certain informations depending the player's team.
                int numberOfSkaters;
                bool goalieAvailable = true;
                switch (currentPPosition.Team) {
                    case PlayerTeam.Blue:
                        numberOfSkaters = numberOfBlueSkaters;

                        if (teamBalancing) {
                            int newMaxNumberOfSkaters = numberOfRedSkaters + ServerConfig.TeamBalanceOffset + 1;
                            if (newMaxNumberOfSkaters < maxNumberOfSkaters)
                                maxNumberOfSkaters = newMaxNumberOfSkaters;
                        }

                        if (hasBlueGoalie)
                            goalieAvailable = false;

                        break;

                    case PlayerTeam.Red:
                        numberOfSkaters = numberOfRedSkaters;

                        if (teamBalancing) {
                            int newMaxNumberOfSkaters = numberOfBlueSkaters + ServerConfig.TeamBalanceOffset + 1;
                            if (newMaxNumberOfSkaters < maxNumberOfSkaters)
                                maxNumberOfSkaters = newMaxNumberOfSkaters;
                        }

                        if (hasRedGoalie)
                            goalieAvailable = false;

                        break;

                    default:
                        Logging.LogError("No team assigned to the current player position ?", ClientConfig);
                        return true;
                }

                // Logging for client debugging //
                if (teamBalancing)
                    Logging.Log("Team balancing is on.", ClientConfig);

                Logging.Log($"Current team : {nameof(currentPPosition.Team)} with {numberOfSkaters} skaters.", ClientConfig);
                Logging.Log($"Current number of skaters on red team : {numberOfRedSkaters}.", ClientConfig);
                Logging.Log($"Current number of skaters on blue team : {numberOfBlueSkaters}.", ClientConfig);
                //                              //

                if (numberOfSkaters >= maxNumberOfSkaters) {
                    if (teamBalancing) {
                        if (goalieAvailable)
                            SystemFunc.SystemFunc.AddClientChatMessage($"Teams are unbalanced ({maxNumberOfSkaters}). Go goalie or switch teams.");
                        else
                            SystemFunc.SystemFunc.AddClientChatMessage($"Teams are unbalanced ({maxNumberOfSkaters}). Switch teams.");
                    }
                    else {
                        if (goalieAvailable)
                            SystemFunc.SystemFunc.AddClientChatMessage($"Team is full ({maxNumberOfSkaters}). Only {PlayerFunc.GOALIE_POSITION} position is available.");
                        else
                            SystemFunc.SystemFunc.AddClientChatMessage($"Team is full ({maxNumberOfSkaters}). Switch teams.");
                    }
                        
                    return false;
                }

                return true;

                // End of old example code. //
            }
        }*/

        /// <summary>
        /// Method called when a client has connected (joined a server) on the server-side.
        /// Used to set server-sided stuff after the game has loaded.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_Everyone_OnClientConnected(Dictionary<string, object> message) {
            if (!ServerFunc.IsDedicatedServer())
                return;

            try {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null && !_hasRegisteredWithNamedMessageHandler) {
                    Logging.Log($"RegisterNamedMessageHandler {Constants.FROM_CLIENT_TO_SERVER}.", ServerConfig);
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(Constants.FROM_CLIENT_TO_SERVER, ReceiveData);
                    _hasRegisteredWithNamedMessageHandler = true;
                }

                ulong clientId = (ulong)message["clientId"];
                string clientSteamId = PlayerManager.Instance.GetPlayerByClientId(clientId).SteamId.Value.ToString();
                try {
                    PlayerFunc.Players_ClientId_SteamId.Add(clientId, "");
                }
                catch {
                    PlayerFunc.Players_ClientId_SteamId.Remove(clientId);
                    PlayerFunc.Players_ClientId_SteamId.Add(clientId, "");
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_Everyone_OnClientConnected)}.\n{ex}", ServerConfig);
            }
        }

        /// <summary>
        /// Method called when a client has disconnect (left a server) on the server-side.
        /// Used to unset data linked to the player like rule status.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_Everyone_OnClientDisconnected(Dictionary<string, object> message) {
            if (!ServerFunc.IsDedicatedServer())
                return;

            try {
                ulong clientId = (ulong)message["clientId"];

                _sentOutOfDateMessage.Remove(clientId);

                /*string clientSteamId;
                try {
                    clientSteamId = PlayerFunc.Players_ClientId_SteamId[clientId];
                }
                catch {
                    return;
                }*/

                PlayerFunc.Players_ClientId_SteamId.Remove(clientId);
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_Everyone_OnClientDisconnected)}.\n{ex}", ServerConfig);
            }
        }

        /// <summary>
        /// Method called when a player changes their state.
        /// Used to set a link between steamIds and clientIds.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_Everyone_OnPlayerGameStateChanged(Dictionary<string, object> message) {
            // Use the event to link client Ids to Steam Ids.
            Dictionary<ulong, (string SteamId, string Username)> playersInfo_ToChange = new Dictionary<ulong, (string, string)>();
            foreach (var kvp in PlayerFunc.Players_ClientId_SteamId) {
                if (!string.IsNullOrEmpty(kvp.Value))
                    continue;

                Player player = PlayerManager.Instance.GetPlayerByClientId(kvp.Key);
                playersInfo_ToChange.Add(kvp.Key, (player.SteamId.Value.ToString(), player.Username.Value.ToString()));
            }

            foreach (var kvp in playersInfo_ToChange) {
                if (string.IsNullOrEmpty(kvp.Value.SteamId))
                    continue;

                PlayerFunc.Players_ClientId_SteamId[kvp.Key] = kvp.Value.SteamId;
                Logging.Log($"Added clientId {kvp.Key} linked to Steam Id {kvp.Value} ({kvp.Value.Username}).", ServerConfig);
            }
        }

        /// <summary>
        /// Method called when the client has stopped on the client-side.
        /// Used to reset the config so that it doesn't carry over between servers.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_OnClientStopped(Dictionary<string, object> message) {
            if (NetworkManager.Singleton == null || ServerFunc.IsDedicatedServer())
                return;

            try {
                ServerConfig = new Configs.ServerConfig();

                _serverHasResponded = false;
                _askServerForStartupDataCount = 0;
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnClientStopped)}.\n{ex}", ClientConfig);
            }
        }

        /// <summary>
        /// Method that manages received data from client-server communications.
        /// </summary>
        /// <param name="clientId">Ulong, Id of the client that sent the data. (0 if the server sent the data)</param>
        /// <param name="reader">FastBufferReader, stream containing the received data.</param>
        public static void ReceiveData(ulong clientId, FastBufferReader reader) {
            try {
                string dataName, dataStr;
                if (clientId == 0) { // If client Id is 0, we received data from the server, so we are client-sided.
                    Logging.Log("ReceiveData", ClientConfig);
                    (dataName, dataStr) = NetworkCommunication.GetData(clientId, reader, ClientConfig);
                }
                else {
                    Logging.Log("ReceiveData", ServerConfig);
                    (dataName, dataStr) = NetworkCommunication.GetData(clientId, reader, ServerConfig);
                }

                switch (dataName) {
                    case Constants.MOD_NAME + "_" + nameof(MOD_VERSION): // CLIENT-SIDE : Mod version check, kick if client and server versions are not the same.
                        _serverHasResponded = true;

                        if (MOD_VERSION == dataStr)
                            break;
                        else if (OLD_MOD_VERSIONS.Contains(dataStr)) {
                            _addServerModVersionOutOfDateMessage = true;
                            break;
                        }

                        _askForModOutOfDateWarning = true;
                        break;

                    case Constants.MOD_NAME + "_kick": // SERVER-SIDE : Kick the client that asked to be kicked.
                        if (dataStr != "1")
                            break;

                        ServerManager.Instance.Server_KickPlayer(PlayerManager.Instance.GetPlayerByClientId(clientId), DisconnectionCode.Kicked,
                            $"{Constants.WORKSHOP_MOD_NAME} mod is out of date or {Constants.WORKSHOP_MOD_NAME} is enabled. Disable the mod, unsubscribe in the workshop and restart your game to update.", false);

                        if (!_sentOutOfDateMessage.TryGetValue(clientId, out DateTime lastCheckTime)) {
                            lastCheckTime = DateTime.MinValue;
                            _sentOutOfDateMessage.Add(clientId, lastCheckTime);
                        }

                        DateTime utcNow = DateTime.UtcNow;
                        if (lastCheckTime + TimeSpan.FromSeconds(900) < utcNow) {
                            if (string.IsNullOrEmpty(PlayerManager.Instance.GetPlayerByClientId(clientId).Username.Value.ToString()))
                                break;

                            Logging.Log($"Warning client {clientId} mod out of date.", ServerConfig);
                            SystemChatMessages.Add($"{PlayerManager.Instance.GetPlayerByClientId(clientId).Username.Value} : {Constants.WORKSHOP_MOD_NAME} Mod is out of date or was enabled manually. Disable all Rulesets and/or unsubscribe from {Constants.WORKSHOP_MOD_NAME} in the workshop and restart your game to update.");
                            _sentOutOfDateMessage[clientId] = utcNow;
                        }
                        break;

                    case Constants.ASK_SERVER_FOR_STARTUP_DATA: // SERVER-SIDE : Send the necessary data to client.
                        if (dataStr != "1")
                            break;

                        NetworkCommunication.SendData(Constants.MOD_NAME + "_" + nameof(MOD_VERSION), MOD_VERSION, clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        NetworkCommunication.SendData(Configs.ServerConfig.CONFIG_DATA_NAME, ServerConfig.ToString(), clientId, Constants.FROM_SERVER_TO_CLIENT, ServerConfig);
                        break;
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Error in ReceiveData.\n{ex}", ServerConfig);
            }
        }

        /// <summary>
        /// Method that launches when the mod is being enabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully enabled.</returns>
        public bool OnEnable() {
            try {
                Logging.Log($"Enabling...", ServerConfig, true);

                _harmony.PatchAll();

                Logging.Log($"Enabled.", ServerConfig, true);

                NetworkCommunication.AddToNotLogList(DATA_NAMES_TO_IGNORE);

                if (ServerFunc.IsDedicatedServer()) {
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null) {
                        Logging.Log($"RegisterNamedMessageHandler {Constants.FROM_CLIENT_TO_SERVER}.", ServerConfig);
                        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(Constants.FROM_CLIENT_TO_SERVER, ReceiveData);
                        _hasRegisteredWithNamedMessageHandler = true;
                    }

                    Logging.Log("Setting server sided config.", ServerConfig, true);
                    ServerConfig = Configs.ServerConfig.ReadConfig();
                }
                else {
                    Logging.Log("Setting client sided config.", ServerConfig, true);
                    ClientConfig = ClientConfig.ReadConfig();
                }

                Logging.Log("Subscribing to events.", ServerConfig, true);
                if (ServerFunc.IsDedicatedServer()) {
                    // Server-side events.
                    EventManager.AddEventListener(nameof(Event_Everyone_OnClientConnected), Event_Everyone_OnClientConnected);
                    EventManager.AddEventListener(nameof(Event_Everyone_OnClientDisconnected), Event_Everyone_OnClientDisconnected);
                    EventManager.AddEventListener(nameof(Event_Everyone_OnPlayerGameStateChanged), Event_Everyone_OnPlayerGameStateChanged);
                }
                else {
                    // Client-side events.
                    EventManager.AddEventListener(nameof(Event_OnClientStopped), Event_OnClientStopped);
                }

                Logging.Log("Unpatching unused code depending on ServerConfig.", ServerConfig, true);

                if (ServerFunc.IsDedicatedServer()) {
                    _harmony.Unpatch(typeof(PhysicsManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance), typeof(PhysicsManager_Update_ClientPatch).GetMethod("Postfix"));
                }
                else {
                    _harmony.Unpatch(typeof(PhysicsManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance), typeof(PhysicsManager_Update_Patch).GetMethod("Postfix"));
                }

                _harmonyPatched = true;
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to enable.\n{ex}", ServerConfig);
                return false;
            }
        }

        /// <summary>
        /// Method that launches when the mod is being disabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully disabled.</returns>
        public bool OnDisable() {
            try {
                if (!_harmonyPatched)
                    return true;

                Logging.Log($"Disabling...", ServerConfig, true);

                Logging.Log("Unsubscribing from events.", ServerConfig, true);
                NetworkCommunication.RemoveFromNotLogList(DATA_NAMES_TO_IGNORE);
                if (ServerFunc.IsDedicatedServer()) {
                    EventManager.RemoveEventListener(nameof(Event_Everyone_OnClientConnected), Event_Everyone_OnClientConnected);
                    EventManager.RemoveEventListener(nameof(Event_Everyone_OnClientDisconnected), Event_Everyone_OnClientDisconnected);
                    EventManager.RemoveEventListener(nameof(Event_Everyone_OnPlayerGameStateChanged), Event_Everyone_OnPlayerGameStateChanged);
                    NetworkManager.Singleton?.CustomMessagingManager?.UnregisterNamedMessageHandler(Constants.FROM_CLIENT_TO_SERVER);
                }
                else {
                    EventManager.RemoveEventListener(nameof(Event_OnClientStopped), Event_OnClientStopped);
                    Event_OnClientStopped(new Dictionary<string, object>());
                    NetworkManager.Singleton?.CustomMessagingManager?.UnregisterNamedMessageHandler(Constants.FROM_SERVER_TO_CLIENT);
                }

                _hasRegisteredWithNamedMessageHandler = false;
                _serverHasResponded = false;
                _askServerForStartupDataCount = 0;

                _harmony.UnpatchSelf();

                Logging.Log($"Disabled.", ServerConfig, true);

                _harmonyPatched = false;
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to disable.\n{ex}", ServerConfig);
                return false;
            }
        }

        /// <summary>
        /// Function that returns true if team balancing is activated.
        /// </summary>
        /// <param name="hasBlueGoalie">Bool, true if blue team has a goalie.</param>
        /// <param name="hasRedGoalie">Bool, true if red team has a goalie.</param>
        /// <returns>Bool, true if team balancing is activated.</returns>
        private static bool TeamBalancing(bool hasBlueGoalie, bool hasRedGoalie) {
            if (ServerConfig.TeamBalancing)
                return true;

            if (!ServerConfig.TeamBalancingGoalie)
                return false;

            if (hasBlueGoalie && hasRedGoalie)
                return false;

            if (hasBlueGoalie || hasRedGoalie)
                return true;

            return false;
        }
    }
}
