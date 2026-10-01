using System.Linq;

namespace oomtm450PuckMod_MatcheS.SystemFunc {
    internal class PlayerFunc {
        #region Constants
        /// <summary>
        /// Const string, position name for the goalie.
        /// </summary>
        internal const string GOALIE_POSITION = "G";
        /// <summary>
        /// Const string, position name for the left winger.
        /// </summary>
        internal const string LEFT_WINGER_POSITION = "LW";
        /// <summary>
        /// Const string, position name for the center.
        /// </summary>
        internal const string CENTER_POSITION = "C";
        /// <summary>
        /// Const string, position name for the right winger.
        /// </summary>
        internal const string RIGHT_WINGER_POSITION = "RW";
        /// <summary>
        /// Const string, position name for the left defender.
        /// </summary>
        internal const string LEFT_DEFENDER_POSITION = "LD";
        /// <summary>
        /// Const string, position name for the right defender.
        /// </summary>
        internal const string RIGHT_DEFENDER_POSITION = "RD";
        #endregion

        #region Properties
        /// <summary>
        /// LockDictionary of ulong and string, dictionary of all players
        /// </summary>
        internal static LockDictionary<ulong, string> Players_ClientId_SteamId { get; } = new LockDictionary<ulong, string>();
        #endregion

        #region Methods/Functions
        /// <summary>
        /// Function that checks if a player is on the ice playing.
        /// </summary>
        /// <param name="player">Player, player to check.</param>
        /// <returns>Bool, is player playing or not.</returns>
        public static bool IsPlayerPlaying(Player player) {
            return !(!player || player.Role == PlayerRole.None || !player.IsCharacterSpawned || (player.Team != PlayerTeam.Red && player.Team != PlayerTeam.Blue));
        }

        /// <summary>
        /// Function that finds a team's goalie.
        /// </summary>
        /// <param name="team">PlayerTeam, team of the goalie.</param>
        /// <returns>Player, goalie found or null.</returns>
        public static Player GetTeamGoalie(PlayerTeam team) {
            return PlayerManager.Instance.GetPlayersByTeam(team).FirstOrDefault(x => x.Role == PlayerRole.Goalie);
        }

        /// <summary>
        /// Function that finds the other team's goalie.
        /// </summary>
        /// <param name="team">PlayerTeam, opposing team of the goalie.</param>
        /// <returns>Player, goalie found or null.</returns>
        public static Player GetOtherTeamGoalie(PlayerTeam team) {
            return PlayerManager.Instance.GetPlayersByTeam(TeamFunc.GetOtherTeam(team)).FirstOrDefault(x => x.Role == PlayerRole.Goalie);
        }

        /// <summary>
        /// Function that returns true if the PlayerPosition has the given role, and if it is claimed depending on hasToBeClaimed.
        /// </summary>
        /// <param name="pPosition">PlayerPosition, object to check.</param>
        /// <param name="role">PlayerRole, role to check for in the PlayerPosition.</param>
        /// <param name="hasToBeClaimed">Bool, true if the PlayerPosition has to be claimed.</param>
        /// <returns>Bool, true if the PlayerPosition has the given PlayerRole, and is claimed or not depending of hasToBeClaimed.</returns>
        internal static bool IsRole(PlayerPosition pPosition, PlayerRole role, bool hasToBeClaimed = true) {
            bool output = pPosition.Role == role;
            if (hasToBeClaimed)
                return output && pPosition.IsClaimed;

            return output;
        }

        /// <summary>
        /// Function that returns true if the PlayerPosition is an attacker (skater), and if it is claimed depending on hasToBeClaimed.
        /// </summary>
        /// <param name="pPosition">PlayerPosition, object to check.</param>
        /// <param name="hasToBeClaimed">Bool, true if the PlayerPosition has to be claimed.</param>
        /// <returns>Bool, true if the PlayerPosition is an attacker (skater), and is claimed or not depending of hasToBeClaimed.</returns>
        internal static bool IsAttacker(PlayerPosition pPosition, bool hasToBeClaimed = true) {
            return IsRole(pPosition, PlayerRole.Attacker, hasToBeClaimed);
        }

        /// <summary>
        /// Function that returns true if the PlayerPosition is a goalie, and if it is claimed depending on hasToBeClaimed.
        /// </summary>
        /// <param name="pPosition">PlayerPosition, object to check.</param>
        /// <param name="hasToBeClaimed">Bool, true if the PlayerPosition has to be claimed.</param>
        /// <returns>Bool, true if the PlayerPosition is a goalie, and is claimed or not depending of hasToBeClaimed.</returns>
        internal static bool IsGoalie(PlayerPosition pPosition, bool hasToBeClaimed = true) {
            return IsRole(pPosition, PlayerRole.Goalie, hasToBeClaimed);
        }
        #endregion
    }
}
