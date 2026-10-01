using Newtonsoft.Json;
using oomtm450PuckMod_Template.SystemFunc;
using System;
using System.IO;

namespace oomtm450PuckMod_Template.Configs {
    /// <summary>
    /// Class containing the configuration from oomtm450_template_serverconfig.json used for this mod.
    /// </summary>
    public class ServerConfig : IConfig {
        #region Constants
        /// <summary>
        /// Const string, name used when sending the config data to the client.
        /// </summary>
        [JsonIgnore]
        public const string CONFIG_DATA_NAME = Constants.MOD_NAME + "_config";

        /// <summary>
        /// String, full path for the config folder.
        /// </summary>
        [JsonIgnore]
        private static readonly string CONFIG_FOLDER_PATH = Path.Combine(Path.GetFullPath("."), "config");

        /// <summary>
        /// String, name of the config file.
        /// </summary>
        [JsonIgnore]
        private static readonly string CONFIG_FILENAME = Constants.MOD_NAME + "_serverconfig.json";

        /// <summary>
        /// String, full path for the config file.
        /// </summary>
        [JsonIgnore]
        private static readonly string CONFIG_PATH = Path.Combine(CONFIG_FOLDER_PATH, CONFIG_FILENAME);
        #endregion

        #region Fields/Properties
        /// <summary>
        /// Bool, true if the info logs must be printed.
        /// </summary>
        public bool LogInfo { get; set; } = true;

        /// <summary>
        /// String, name of the mod.
        /// </summary>
        [JsonIgnore]
        public string ModName { get; } = Constants.MOD_NAME;

        /// <summary>
        /// Bool, true if the numeric values has to be replaced be the default ones. Make this false to use custom values.
        /// </summary>
        public bool UseDefaultNumericValues { get; set; } = true;

        /// <summary>
        /// Int, number of skaters that are allowed on the ice at the same time per team.
        /// </summary>
        public int MaxNumberOfSkaters { get; set; } = 5;

        /// <summary>
        /// Bool, true if team balancing has to be respected.
        /// </summary>
        public bool TeamBalancing { get; set; } = false;

        /// <summary>
        /// Int, offset in the number of skaters between both teams if TeamBalancing or TeamBalancingGoalie is activated.
        /// </summary>
        public int TeamBalanceOffset { get; set; } = 0;

        /// <summary>
        /// Bool, if a goalie is playing in the red or blue team and if the other team has the same or more skaters, the next position has to be goalie.
        /// TLDR : Team balancing only if atleast one goalie is playing.
        /// </summary>
        public bool TeamBalancingGoalie { get; set; } = false;

        /// <summary>
        /// Bool, true if admins can bypass the skaters limit.
        /// </summary>
        public bool AdminBypass { get; set; } = true;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor of ServerConfig.
        /// </summary>
        public ServerConfig() { }

        /// <summary>
        /// Copy constructor of ServerConfig.
        /// </summary>
        /// <param name="serverConfig">ServerConfig, config to copy.</param>
        public ServerConfig(ServerConfig serverConfig) {
            LogInfo = serverConfig.LogInfo;
            ModName = serverConfig.ModName;
            UseDefaultNumericValues = serverConfig.UseDefaultNumericValues;

            MaxNumberOfSkaters = serverConfig.MaxNumberOfSkaters;
            TeamBalancing = serverConfig.TeamBalancing;
            TeamBalanceOffset = serverConfig.TeamBalanceOffset;
            TeamBalancingGoalie = serverConfig.TeamBalancingGoalie;

            AdminBypass = serverConfig.AdminBypass;
        }
        #endregion

        #region Methods/Functions
        /// <summary>
        /// Method that updates this config with the new default values, if the old default values were used.
        /// </summary>
        /// <param name="oldConfig">ISubConfig, config with old values.</param>
        public void UpdateDefaultValues(ISubConfig oldConfig) {
            if (!(oldConfig is OldServerConfig))
                throw new ArgumentException($"oldConfig has to be typeof {nameof(OldServerConfig)}.", nameof(oldConfig));

            OldServerConfig _oldConfig = oldConfig as OldServerConfig;
            ServerConfig newConfig = new ServerConfig();

            /*if (LogInfo == _oldConfig.LogInfo)
                LogInfo = newConfig.LogInfo;*/

            if (MaxNumberOfSkaters == _oldConfig.MaxNumberOfSkaters)
                MaxNumberOfSkaters = newConfig.MaxNumberOfSkaters;

            if (TeamBalancing == _oldConfig.TeamBalancing)
                TeamBalancing = newConfig.TeamBalancing;

            if (TeamBalanceOffset == _oldConfig.TeamBalanceOffset)
                TeamBalanceOffset = newConfig.TeamBalanceOffset;

            /*if (AdminSteamIds.Count() != _oldConfig.AdminSteamIds.Count())
                AdminSteamIds = new List<string>(_oldConfig.AdminSteamIds).ToArray();
            else {
                foreach (string oldAdminSteamId in new List<string>(_oldConfig.AdminSteamIds)) {
                    if (!AdminSteamIds.Contains(oldAdminSteamId)) {
                        AdminSteamIds = new List<string>(_oldConfig.AdminSteamIds).ToArray();
                        break;
                    }
                }
            }*/

            if (AdminBypass == _oldConfig.AdminBypass)
                AdminBypass = newConfig.AdminBypass;
        }

        /// <summary>
        /// Function that serialize the config object.
        /// </summary>
        /// <returns>String, serialized config.</returns>
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Function that unserialize a ServerConfig.
        /// </summary>
        /// <param name="json">String, JSON that is the serialized ServerConfig.</param>
        /// <returns>ServerConfig, unserialized ServerConfig.</returns>
        internal static Configs.ServerConfig SetConfig(string json) {
            return JsonConvert.DeserializeObject<Configs.ServerConfig>(json);
        }

        /// <summary>
        /// Function that reads the config file for the mod and create a ServerConfig object with it.
        /// Also creates the file with the default values, if it doesn't exists.
        /// </summary>
        /// <returns>ServerConfig, parsed config.</returns>
        internal static Configs.ServerConfig ReadConfig() {
            Configs.ServerConfig config = new Configs.ServerConfig();

            try {
                if (!Directory.Exists(CONFIG_FOLDER_PATH))
                    Directory.CreateDirectory(CONFIG_FOLDER_PATH);

                if (File.Exists(CONFIG_PATH)) {
                    string configFileContent = File.ReadAllText(CONFIG_PATH);
                    config = SetConfig(configFileContent);
                    Logging.Log($"Server config read.", config, true);
                }

                config.UpdateDefaultValues(new OldServerConfig());

                try {
                    File.WriteAllText(CONFIG_PATH, config.ToString());
                }
                catch (Exception ex) {
                    Logging.LogError($"Can't write the server config file. (Permission error ?)\n{ex}", new Configs.ServerConfig());
                }

                Logging.Log($"Wrote server config : {config}", config, true);

                if (config.UseDefaultNumericValues) {
                    Configs.ServerConfig defaultConfig = new Configs.ServerConfig {
                        LogInfo = config.LogInfo,
                        UseDefaultNumericValues = config.UseDefaultNumericValues,
                        TeamBalancing = config.TeamBalancing,
                        TeamBalancingGoalie = config.TeamBalancingGoalie,
                        AdminBypass = config.AdminBypass,
                    };

                    config = defaultConfig;
                }
            }
            catch (Exception ex) {
                Logging.LogError($"Can't read the server config file/folder. (Permission error ?)\n{ex}", new Configs.ServerConfig());
            }

            return config;
        }
        #endregion
    }
}
