using HarmonyLib;
using oomtm450PuckMod_MatcheS.Configs;
using oomtm450PuckMod_MatcheS.SystemFunc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Unity.Netcode;

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
        #endregion

        #region Properties
        /// <summary>
        /// ClientConfig, config set by the client.
        /// </summary>
        internal static Configs.ClientConfig ClientConfig { get; set; } = new Configs.ClientConfig();
        #endregion

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
