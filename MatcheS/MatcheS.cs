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
        private const string MOD_VERSION = "1.0.0";

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
        /// LockDictionary of string and ChatMessage and DateTime, dictionnary of the last message of every player in the current session.
        /// </summary>
        private static readonly LockDictionary<string, (ChatMessage ChatMessage, DateTime DateTime)> _lastMessages = new LockDictionary<string, (ChatMessage, DateTime)>();

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

                    DateTime now = DateTime.UtcNow;

                    if (!_lastMessages.TryGetValue(chatMessageSteamId, out var lastChatMessage)) {
                        _lastMessages.Add(chatMessageSteamId, (chatMessage, now));
                        return true;
                    }

                    if (chatMessage.IsTeamChat == lastChatMessage.ChatMessage.IsTeamChat &&
                        chatMessage.Content.ToString() == lastChatMessage.ChatMessage.Content.ToString() &&
                        (now - lastChatMessage.DateTime).TotalMilliseconds < ClientConfig.SpamMillisecondsThreshold) {
                        SystemFunc.SystemFunc.AddClientChatMessage($"Blocked {chatMessage.Username.Value.ToString()} : {chatMessage.Content.ToString()}");
                        return false;
                    }

                    _lastMessages.AddOrUpdate(chatMessageSteamId, (chatMessage, now));
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(UIChat_AddChatMessage_Patch)} Prefix().\n{ex}", ClientConfig);
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
}
