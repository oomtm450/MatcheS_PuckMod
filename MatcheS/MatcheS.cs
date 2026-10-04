using HarmonyLib;
using oomtm450PuckMod_MatcheS.Configs;
using oomtm450PuckMod_MatcheS.SystemFunc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace oomtm450PuckMod_MatcheS {
    /// <summary>
    /// Class containing the main code for the MatcheS patch.
    /// </summary>
    public class MatcheS : IPuckPlugin {
        #region Constants
        /// <summary>
        /// Const string, version of the mod.
        /// </summary>
        private const string MOD_VERSION = "1.0.1";

        /// <summary>
        /// ReadOnlyCollection of string, last released versions of the mod.
        /// </summary>
        private static readonly ReadOnlyCollection<string> OLD_MOD_VERSIONS = new ReadOnlyCollection<string>(new List<string> {
        });

        /// <summary>
        /// ReadOnlyCollection of string, collection of datanames to not log.
        /// </summary>
        private static readonly ReadOnlyCollection<string> DATA_NAMES_TO_IGNORE = new ReadOnlyCollection<string>(new List<string> {
            "eventName",
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
        /// LockDictionary of string and ChatMsg, dictionnary of the last message of every player in the current session.
        /// </summary>
        private static readonly LockDictionary<string, ChatMsg> _lastMessages = new LockDictionary<string, ChatMsg>();

        /// <summary>
        /// String, steamId of the client using the mod (to not remove spammed messages of client player).
        /// </summary>
        private static string _localPlayerSteamId = "";
        #endregion

        #region Properties
        /// <summary>
        /// ClientConfig, config set by the client.
        /// </summary>
        internal static Configs.ClientConfig ClientConfig { get; set; } = new Configs.ClientConfig();
        #endregion

        /// <summary>
        /// Class that patches the AddChatMessage function from UIChat.
        /// </summary>
        [HarmonyPatch(typeof(UIChat), nameof(UIChat.AddChatMessage))]
        public class UIChat_AddChatMessage_Patch {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.VeryHigh)]
            public static bool Prefix(ChatMessage chatMessage, Units units, bool filterProfanity) {
                try {
                    string chatMessageSteamId = "";
                    try {
                        chatMessageSteamId = chatMessage.SteamID.Value.ToString();
                    }
                    catch {
                        return true;
                    }

                    if (string.IsNullOrEmpty(chatMessageSteamId))
                        return true;

                    if (string.IsNullOrEmpty(_localPlayerSteamId)) {
                        _localPlayerSteamId = PlayerManager.Instance.GetLocalPlayer().SteamId.Value.ToString();

                        if (string.IsNullOrEmpty(_localPlayerSteamId))
                            return true;
                    }

                    if (chatMessageSteamId == _localPlayerSteamId)
                        return true;

                    if (!_lastMessages.TryGetValue(chatMessageSteamId, out var lastChatMessage)) {
                        _lastMessages.Add(chatMessageSteamId, new ChatMsg(chatMessage.Content.Value.ToString(), chatMessage.IsTeamChat));
                        return true;
                    }

                    if (chatMessage.IsTeamChat == lastChatMessage.IsTeamChat &&
                        chatMessage.Content.Value.ToString() == lastChatMessage.Message &&
                        (DateTime.UtcNow - lastChatMessage.DateTime).TotalMilliseconds < ClientConfig.SpamMillisecondsThreshold) {
                        return false;
                    }

                    _lastMessages.AddOrUpdate(chatMessageSteamId, new ChatMsg(chatMessage.Content.Value.ToString(), chatMessage.IsTeamChat));
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(UIChat_AddChatMessage_Patch)} Prefix().\n{ex}", ClientConfig);
                }

                return true;
            }
        }

        /// <summary>
        /// Class that patches the OnGameStateChanged event from BaseGameMode.
        /// </summary>
        [HarmonyPatch(typeof(BaseGameMode<BaseGameModeConfig>), "OnGameStateChanged")]
        public class BaseGameMode_OnGameStateChanged_Patch {
            [HarmonyPrefix]
            public static bool Prefix(GameState oldGameState, GameState newGameState) {
                try {
                    if (oldGameState.Phase == newGameState.Phase)
                        return true;

                    if (newGameState.Phase != GamePhase.PreGame)
                        return true;

                    _lastMessages.Clear();
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(BaseGameMode_OnGameStateChanged_Patch)} Prefix().\n{ex}", ClientConfig);
                }

                return true;
            }
        }

        /// <summary>
        /// Method that launches when the mod is being enabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully enabled.</returns>
        public bool OnEnable() {
            try {
                Logging.Log($"Enabling...", ClientConfig, true);

                _harmony.PatchAll();

                Logging.Log($"Enabled.", ClientConfig, true);

                Logging.Log("Setting client sided config.", ClientConfig, true);
                ClientConfig = ClientConfig.ReadConfig();

                _harmonyPatched = true;
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to enable.\n{ex}", new Configs.ClientConfig());
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

                Logging.Log($"Disabling...", ClientConfig, true);

                _harmony.UnpatchSelf();

                Logging.Log($"Disabled.", ClientConfig, true);

                _harmonyPatched = false;
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to disable.\n{ex}", new Configs.ClientConfig());
                return false;
            }
        }
    }

    internal class ChatMsg {
        internal string Message { get; set; } = "";

        internal DateTime DateTime { get; set; } = DateTime.UtcNow;

        internal bool IsTeamChat { get; set; } = false;

        internal ChatMsg(string message, bool isTeamChat) {
            Message = message;
            IsTeamChat = isTeamChat;
        }
    }
}
