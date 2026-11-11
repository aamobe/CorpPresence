using System.Reflection;
using System.Text.Json;

internal sealed record IdentityEntry(string Id,string LegacyId,string LegacySprite,string Goods,string Company);
internal sealed record IdentityBaseline(string SourceCommit,string DllSha256,List<IdentityEntry> Entries);
internal static class IdentityContract
{
    internal static List<string> Validate(IReadOnlyList<IdentityEntry> current,IReadOnlyList<IdentityEntry> baseline)
    {
        var errors=new List<string>();
        var byId=new Dictionary<string,IdentityEntry>(StringComparer.Ordinal);
        var legacy=new HashSet<(string,string)>();
        foreach(var entry in current)
        {
            if(string.IsNullOrWhiteSpace(entry.Id) || !entry.Id.StartsWith("corp_presence_",StringComparison.Ordinal))
                errors.Add("invalid identity: "+entry.Id);
            else if(!byId.TryAdd(entry.Id,entry)) errors.Add("duplicate identity: "+entry.Id);
            if((entry.Goods==null)==(entry.Company==null)
                || (entry.Goods!=null && string.IsNullOrWhiteSpace(entry.Goods))
                || (entry.Company!=null && string.IsNullOrWhiteSpace(entry.Company)))
                errors.Add("ambiguous or missing factory kind: "+entry.Id);
            if(string.IsNullOrWhiteSpace(entry.LegacySprite)) errors.Add("invalid sprite identity: "+entry.Id);
            if(entry.LegacyId!=null && !legacy.Add((entry.LegacyId,entry.LegacySprite)))
                errors.Add("ambiguous legacy identity: "+entry.Id);
        }
        foreach(var entry in baseline)
        {
            if(!byId.TryGetValue(entry.Id,out var value)) errors.Add("removed identity: "+entry.Id);
            else if(value!=entry) errors.Add("changed identity mapping: "+entry.Id);
        }
        return errors;
    }

    internal static List<IdentityEntry> ReadAssembly(string path)
    {
        var assembly=Assembly.LoadFile(Path.GetFullPath(path));
        var type=assembly.GetType("CorpPresence.ItemIdentity",true);
        var values=(System.Collections.IEnumerable)type.GetProperty("All").GetValue(null);
        var result=new List<IdentityEntry>();
        foreach(var value in values)
        {
            string Get(string name)=>(string)value.GetType().GetProperty(name).GetValue(value);
            var entry=new IdentityEntry(Get("Id"),Get("LegacyId"),Get("LegacySprite"),Get("Goods"),Get("Company"));
            result.Add(entry);
            string Resolve(string id,string sprite)=>(string)type.GetMethod("ResolveId").Invoke(null,new object[]{id,sprite});
            if(Resolve(entry.Id,null)!=entry.Id || (entry.LegacyId!=null && Resolve(entry.LegacyId,entry.LegacySprite)!=entry.Id))
                throw new Exception("compiled resolver disagrees with identity table: "+entry.Id);
        }
        if(type.GetMethod("ResolveId").Invoke(null,new object[]{"water","water"})!=null
            || type.GetMethod("ResolveId").Invoke(null,new object[]{null,null})!=null)
            throw new Exception("compiled resolver adopts an unknown or null identity");
        var rejected=ValidateRejectedPairs((id,sprite)=>(string)type.GetMethod("ResolveId").Invoke(null,new object[]{id,sprite}),result);
        if(rejected.Count!=0)throw new Exception(string.Join(Environment.NewLine,rejected));
        return result;
    }

    internal static IdentityBaseline ReadBaseline(string path)=>JsonSerializer.Deserialize<IdentityBaseline>(
        File.ReadAllText(path),new JsonSerializerOptions {PropertyNameCaseInsensitive=true});

    internal static List<string> ValidateRejectedPairs(Func<string,string,string> resolve,IReadOnlyList<IdentityEntry> entries)
    {
        var errors=new List<string>();
        foreach(var entry in entries)
        {
            if(resolve("\0unknown-id",entry.LegacySprite)!=null || resolve(null,entry.LegacySprite)!=null)
                errors.Add("resolver adopts unknown ID with known sprite: "+entry.Id);
            if(entry.LegacyId!=null && (resolve(entry.LegacyId,"\0unknown-sprite")!=null || resolve(entry.LegacyId,null)!=null))
                errors.Add("resolver adopts legacy ID without matching sprite: "+entry.Id);
        }
        return errors;
    }
}
