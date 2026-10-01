using System;
using System.Linq;
using System.Reflection;

namespace oomtm450PuckMod_MatcheS.SystemFunc {
    public static class SystemFunc {
        public static T GetPrivateField<T>(Type typeContainingField, object instanceOfType, string fieldName) {
            if (instanceOfType == null)
                return (T)typeContainingField.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).GetValue(instanceOfType);
            else
                return (T)typeContainingField.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instanceOfType);
        }

        public static void AddClientChatMessage(string message) {
            ChatMessage chatMsg = new ChatMessage {
                SteamID = null,
                Username = null,
                Team = null,
                Content = message,
                Timestamp = Utils.GetTimestamp(),
                IsQuickChat = false,
                IsTeamChat = false,
                IsSystem = true,
            };
            ChatManager.Instance.AddChatMessage(chatMsg);
        }

        public static string RemoveWhitespace(string input) {
            return new string(input
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }
    }
}
