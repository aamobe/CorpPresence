// Only the game locale/frame boundary is substituted; translation code and JSON are real.
namespace UnityEngine
{
    internal static class Time { internal static int frameCount; }
    internal class Sprite { }
    internal class Texture2D { }
}
internal static class FontLoader { internal static object Instance = new(); }
internal static class LocHelper
{
    internal static string Locale = "zh";
    internal static string GetCurrentLocaleCode() => Locale;
}
namespace CorpPresence
{
    internal static class Goods
    {
        internal sealed class Def { internal GoodsText.Keys TextKeys; }
        internal static GameItem Make(string id) => DirectoryMaster.Item(id, false);
        internal static UnityEngine.Sprite FindSprite(string id) => null;
        internal static UnityEngine.Sprite VanillaByName(string id) => null;
    }
    internal static class ModLog
    {
        internal static void Warn(string message) => Console.WriteLine(message);
        internal static void Info(string message) { }
    }
    internal class GameItem
    {
        internal string identifier = null, spritePath = null, spriteAtlasPath = null, flavorText = null, shortDescription = null, Name = null;
        internal string GetDisplayName() => Name;
        internal void Destroy() { }
    }
    internal static class DirectoryMaster
    {
        internal static GameItem Item(string key, bool isOwned) => new() { identifier = key,
            Name = L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"), flavorText = "Description" };
    }
    internal sealed class Company { internal string Id = null, Name = null; }
    internal sealed class CompanyState { internal System.Collections.Generic.List<string> Brand = new(); }
    internal static class Companies { internal static Company Find(string id) => null; internal static CompanyState StateOf(Company company) => null; }
    internal static class ItemIdentity { internal static string GoodsKey(string id, string sprite) => null; }
    internal sealed class EmporiumEntry
    {
        internal static EmporiumEntry Instance => null;
        internal System.Collections.Generic.List<GameItem> GetAllOwnedItems() => new();
    }
    internal static class RenderHandler { internal static UnityEngine.Sprite LoadFromAtlas(string atlas, string sprite) => null; }
}
namespace CorpPresence.UI { internal static class Tex { internal static UnityEngine.Texture2D FromSprite(UnityEngine.Sprite sprite) => null; } }
