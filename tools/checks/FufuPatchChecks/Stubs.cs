namespace CorpPresence;
internal static class Settings {public static bool CleanupForUninstall => false;}
internal static class SaveSlot {public static PlayerStore CurrentStore => null;}
internal static class FufuDoll {public const string ItemId="corp_presence_fufu_doll";}
internal static class PatchInstallation {public static void Run(string label,System.Func<bool> f,bool required){}}
internal static class ModLog {public static int Warnings;public static void Warn(string message){Warnings++;}}
