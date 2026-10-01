using Newtonsoft.Json;
using oomtm450PuckMod_MatcheS.SystemFunc;
using System;
using System.IO;

namespace oomtm450PuckMod_MatcheS.Configs {
    /// <summary>
    /// Class containing the configuration from oomtm450_template_clientconfig.json used for this mod.
    /// </summary>
    public class ClientConfig : IConfig {
        #region Constants
        /// <summary>
        /// String, full path for the config folder.
        /// </summary>
        [JsonIgnore]
        private static readonly string CONFIG_FOLDER_PATH = Path.Combine(Path.GetFullPath("."), "config");

        /// <summary>
        /// String, full path for the config file.
        /// </summary>
        [JsonIgnore]
        private static readonly string CONFIG_PATH = Path.Combine(CONFIG_FOLDER_PATH, Constants.MOD_NAME + "_clientconfig.json");
        #endregion

        #region Fields/Properties
        /// <summary>
        /// Bool, true if the info logs must be printed.
        /// </summary>
        public bool LogInfo { get; set; } = true;

        /// <summary>
        /// Int, number of milliseconds before not blocking the same message from the same player.
        /// </summary>
        public int SpamMillisecondsThreshold { get; set; } = 3000;

        /// <summary>
        /// String, name of the mod.
        /// </summary>
        [JsonIgnore]
        public string ModName { get; } = Constants.MOD_NAME;
        #endregion

        /// <summary>
        /// Function that serialize the ClientConfig object.
        /// </summary>
        /// <returns>String, serialized ClientConfig.</returns>
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Function that unserialize a ClientConfig.
        /// </summary>
        /// <param name="json">String, JSON that is the serialized ClientConfig.</param>
        /// <returns>ClientConfig, unserialized ClientConfig.</returns>
        internal static Configs.ClientConfig SetConfig(string json) {
            return JsonConvert.DeserializeObject<Configs.ClientConfig>(json);
        }

        /// <summary>
        /// Function that reads the config file for the mod and create a ClientConfig object with it.
        /// Also creates the file with the default values, if it doesn't exists.
        /// </summary>
        /// <returns>ClientConfig, parsed config.</returns>
        internal static Configs.ClientConfig ReadConfig() {
            Configs.ClientConfig config;

            try {
                if (!Directory.Exists(CONFIG_FOLDER_PATH))
                    Directory.CreateDirectory(CONFIG_FOLDER_PATH);

                if (File.Exists(CONFIG_PATH)) {
                    string configFileContent = File.ReadAllText(CONFIG_PATH);
                    config = SetConfig(configFileContent);
                    Logging.Log($"Client config read.", config, true);
                }
                else
                    config = new Configs.ClientConfig();

                config.Save();

                return config;
            }
            catch (Exception ex) {
                Logging.LogError($"Can't read the server config file/folder. (Permission error ?)\n{ex}", new Configs.ClientConfig());
            }

            return new Configs.ClientConfig();
        }

        internal void Save() {
            if (string.IsNullOrEmpty(CONFIG_PATH)) {
                Logging.LogError($"Can't write the client config file. ({nameof(CONFIG_PATH)} null or empty)", this);
                return;
            }

            try {
                if (!Directory.Exists(CONFIG_FOLDER_PATH))
                    Directory.CreateDirectory(CONFIG_FOLDER_PATH);

                File.WriteAllText(CONFIG_PATH, ToString());
            }
            catch (Exception ex) {
                Logging.LogError($"Can't write the client config file. (Permission error ?)\n{ex}", this);
            }

            Logging.Log($"Wrote client config : {ToString()}", this, true);
        }
    }
}
