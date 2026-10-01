using System.Collections.Generic;

namespace oomtm450PuckMod_Template.Configs {
    /// <summary>
    /// Class containing the old configuration from oomtm450_ruleset_serverconfig.json used for this mod.
    /// </summary>
    public class OldServerConfig : ISubConfig {
        #region Properties
        /// <summary>
        /// Bool, true if the info logs must be printed.
        /// </summary>
        public bool LogInfo { get; } = true;

        /// <summary>
        /// Bool, true if the numeric values has to be replaced be the default ones. Make this false to use custom values.
        /// </summary>
        public bool UseDefaultNumericValues { get; } = true;

        /// <summary>
        /// Int, number of skaters that are allowed on the ice at the same time per team.
        /// </summary>
        public int MaxNumberOfSkaters { get; } = 5;

        /// <summary>
        /// Bool, true if team balancing has to be respected.
        /// </summary>
        public bool TeamBalancing { get; } = false;

        /// <summary>
        /// Int, offset in the number of skaters between both teams if TeamBalancing or TeamBalancingGoalie is activated.
        /// </summary>
        public int TeamBalanceOffset { get; } = 0;

        /// <summary>
        /// Bool, if a goalie is playing in the red or blue team and if the other team has the same or more skaters, the next position has to be goalie.
        /// TLDR : Team balancing only if atleast one goalie is playing.
        /// </summary>
        public bool TeamBalancingGoalie { get; } = false;

        /// <summary>
        /// Bool, true if admins can bypass the skaters limit.
        /// </summary>
        public bool AdminBypass { get; } = true;
        #endregion

        #region Methods/Functions
        /// <summary>
        /// Method that updates this config with the new default values, if the old default values were used.
        /// </summary>
        /// <param name="oldConfig">ISubConfig, config with old values.</param>
        /// <exception cref="System.NotImplementedException">The old configs UpdateDefaultValues are not to be used.</exception>
        public void UpdateDefaultValues(ISubConfig oldConfig) {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}
