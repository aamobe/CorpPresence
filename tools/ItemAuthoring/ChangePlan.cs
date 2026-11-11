using System.Text;
using System.Text.Json;

namespace CorpPresence.ItemAuthoring;

public sealed record FileChange(string RelativePath, byte[] Before, byte[] After, string Reason);

public sealed class ChangePlan
{
    private readonly string root;
    private readonly List<FileChange> changes = [];
    private readonly Dictionary<string,byte[]> reads = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<FileChange> Changes => changes;
    public IReadOnlyDictionary<string,byte[]> Dependencies => reads;
    public ChangePlan(string root) { this.root = Path.GetFullPath(root); SafePath("."); }

    public string SafePath(string relative)
    {
        if(Path.IsPathRooted(relative) || relative.Contains(':')) Unsafe();
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if(path != root && !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Unsafe();
        // Reject junctions/symlinks, including in the staging/backup path.
        for(string? part = path; part != null; part = Path.GetDirectoryName(part))
        {
            if((File.Exists(part) || Directory.Exists(part)) &&
                (File.GetAttributes(part) & FileAttributes.ReparsePoint) != 0) Unsafe();
        }
        return path;
    }

    public byte[] Read(string relative)
    {
        if(!reads.TryGetValue(relative,out byte[]? bytes))
            reads.Add(relative,bytes=File.ReadAllBytes(SafePath(relative)));
        return bytes;
    }
    public string ReadText(string relative) => Encoding.UTF8.GetString(Read(relative)).TrimStart('\uFEFF');
    public IEnumerable<string> Files(string relative,string extension)
    {
        foreach(string entry in Directory.EnumerateFileSystemEntries(SafePath(relative)))
        {
            string child=Path.GetRelativePath(root,entry).Replace('\\','/');
            SafePath(child);
            if(Directory.Exists(entry)) {foreach(string file in Files(child,extension)) yield return file;}
            else if(entry.EndsWith(extension,StringComparison.OrdinalIgnoreCase)) yield return child;
        }
    }

    public void Add(string relative, string after, string reason = "authoring registration")
        => Add(relative, Encoding.UTF8.GetBytes(after), reason);
    public void Add(string relative, byte[] after, string reason)
    {
        string path = SafePath(relative);
        if(!File.Exists(path)) throw new AuthoringException("MISSING_TARGET", relative);
        if(changes.Any(c => string.Equals(c.RelativePath,relative,StringComparison.OrdinalIgnoreCase)))
            throw new AuthoringException("DUPLICATE_TARGET",relative);
        byte[] before = Read(relative);
        if(!before.AsSpan().SequenceEqual(after)) changes.Add(new(relative,before,after,reason));
    }

    public string? Apply(Action<int>? beforeWrite = null)
    {
        if(changes.Count == 0) return null;
        foreach(var c in changes) Unchanged(c);
        CheckDependencies();
        string staging = SafePath("artifacts/item-authoring"); Directory.CreateDirectory(staging);
        using var gate = new FileStream(SafePath("artifacts/item-authoring/apply.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        foreach(var c in changes) Unchanged(c);
        CheckDependencies();
        string backup = Path.Combine(staging, "backup-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(backup);
        // Save all originals before touching any target. Backup names never derive from spec paths.
        for(int i=0;i<changes.Count;i++) File.WriteAllBytes(Path.Combine(backup,i+".original"),changes[i].Before);
        File.WriteAllText(Path.Combine(backup,"manifest.json"),JsonSerializer.Serialize(changes.Select((c,i)=>new{file=c.RelativePath,original=i+".original",c.Reason})));
        var applied = new List<FileChange>();
        try
        {
            for(int i=0;i<changes.Count;i++)
            {
                beforeWrite?.Invoke(i);
                var change=changes[i]; Unchanged(change);
                AtomicWrite(SafePath(change.RelativePath),change.After);
                applied.Add(change);
            }
            return backup;
        }
        catch(Exception error)
        {
            var conflicts = new List<string>();
            foreach(var change in applied.AsEnumerable().Reverse())
            {
                try
                {
                    string path = SafePath(change.RelativePath);
                    if(!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(change.After))
                    { conflicts.Add(change.RelativePath); continue; }
                    AtomicWrite(path,change.Before);
                }
                catch(IOException) {conflicts.Add(change.RelativePath);}
            }
            if(conflicts.Count>0) throw new AuthoringException("ROLLBACK_CONFLICT",
                "Concurrent edits preserved; originals in " + backup + "; inspect " + string.Join(", ",conflicts) + ". Initial error: " + error.Message);
            throw;
        }
    }
    private void CheckDependencies()
    {
        foreach(var entry in reads)
            if(!File.Exists(SafePath(entry.Key)) || !File.ReadAllBytes(SafePath(entry.Key)).AsSpan().SequenceEqual(entry.Value))
                throw new AuthoringException("CONCURRENT_CHANGE",entry.Key+" changed during planning.");
    }
    private void Unchanged(FileChange change)
    {
        string path = SafePath(change.RelativePath);
        if(!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(change.Before))
            throw new AuthoringException("CONCURRENT_CHANGE",change.RelativePath + " changed since preview; re-preview required.");
    }
    private static void AtomicWrite(string path, byte[] content)
    {
        string temporary=path+".authoring-"+Guid.NewGuid().ToString("N")+".tmp";
        try { File.WriteAllBytes(temporary,content); File.Move(temporary,path,true); }
        finally { if(File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void Unsafe() => throw new AuthoringException("UNSAFE_PATH","Path escapes repository or traverses a link.");
}
