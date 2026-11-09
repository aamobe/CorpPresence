using CorpPresence;
using UnityEngine.Localization.Settings;

foreach (var code in new[] { "en", "zh", "ru", "de", "ja", "fr", "es", "pl", "ko" }) {
    GameResources.Begin();
    LocHelper.Startup = code;
    LocalizationSettings.SelectedLocale = new() { Identifier = new(code) };
    LocalizationSettings.StringDatabase.Requests = 0;
    if (!GameResources.Poll() || !GameResources.Ready) throw new Exception("Startup remains blocked for " + code);
    if (LocalizationSettings.StringDatabase.LastLocale != code) throw new Exception("Wrong table locale: " + code);
    Console.WriteLine("PASS startup completes with matching Mechanic table: " + code);
}
GameResources.Begin();
LocHelper.Startup = "ko";
LocalizationSettings.SelectedLocale = new() { Identifier = new("en") };
LocalizationSettings.StringDatabase.Requests = 0;
if (GameResources.Poll() || LocalizationSettings.StringDatabase.Requests != 0) throw new Exception("Requested a table before the game applied its selected language");
LocalizationSettings.SelectedLocale = new() { Identifier = new("ko") };
if (!GameResources.Poll()) throw new Exception("Startup did not resume after Korean selection completed");
Console.WriteLine("PASS startup waits for language selection then resumes for Korean");

internal static class LocHelper { internal static string Startup = "en"; public static string ResolveStartupLocale() => Startup; }
namespace UnityEngine.Localization
{
    public readonly record struct LocaleIdentifier(string Code);
    public sealed class Locale { public LocaleIdentifier Identifier; }
}
namespace UnityEngine.ResourceManagement.AsyncOperations
{
    public enum AsyncOperationStatus { None, Succeeded, Failed }
    public struct AsyncOperationHandle<T>
    {
        public T Result;
        public bool IsDone => true;
        public bool IsValid() => Result != null;
        public AsyncOperationStatus Status => AsyncOperationStatus.Succeeded;
        public Exception OperationException => null;
    }
}
namespace UnityEngine.Localization.Tables
{
    public sealed class StringTable
    {
        public UnityEngine.Localization.LocaleIdentifier LocaleIdentifier;
        public object GetEntry(string key) => key == "mech_condition_pure_water" ? new object() : null;
    }
}
namespace UnityEngine.Localization.Settings
{
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.Localization.Tables;
    public static class LocalizationSettings
    {
        public static AsyncOperationHandle<object> InitializationOperation => new() { Result = new() };
        public static UnityEngine.Localization.Locale SelectedLocale;
        public static readonly TableDatabase StringDatabase = new();
    }
    public sealed class TableDatabase
    {
        public int Requests;
        public string LastLocale;
        public AsyncOperationHandle<StringTable> GetTableAsync(string table, UnityEngine.Localization.Locale locale)
        {
            Requests++; LastLocale = locale.Identifier.Code;
            if (table != "Mechanic") throw new Exception("Unexpected table");
            return new() { Result = new() { LocaleIdentifier = locale.Identifier } };
        }
    }
}
