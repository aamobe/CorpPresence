using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic.FileIO;
using SearchOption = System.IO.SearchOption;

namespace CorpPresence.ItemAuthoring;

public static class Planner
{
    private const string Definitions = "src/CorpPresence/Items/Goods.Definitions.cs";
    private const string Identities = "src/CorpPresence/Items/ItemIdentity.cs";
    private const string Texts = "src/CorpPresence/Presentation/GoodsText.cs";
    private const string Catalog = "src/CorpPresence/Companies/PastelCo.cs";
    private static string Q(string value) => SymbolDisplay.FormatLiteral(value,true);

    public static ChangePlan Create(string root, Spec spec)
    {
        if(spec.Kind!="ordinary") throw new AuthoringException("REFERENCE_ONLY","Complex categories do not register runtime content.");
        var plan = new ChangePlan(root);
        string artPath=plan.SafePath(spec.ArtPath);
        if(!File.Exists(artPath)) throw new AuthoringException("MISSING_ART",spec.ArtPath);
        byte[] png=plan.Read(spec.ArtPath);
        if(png.Length<33 || !png.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) ||
            BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16,4))<=0 || BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20,4))<=0)
            throw new AuthoringException("INVALID_ART","Expected PNG with nonzero native dimensions");
        string prefix="CorpPresence.art."+spec.Company+".";
        string filename=Path.GetFileNameWithoutExtension(spec.ArtPath);
        if(!filename.StartsWith(prefix,StringComparison.Ordinal)) throw new AuthoringException("INVALID_ART","Image resource must use " + prefix + "<art>.png");
        string art=filename[prefix.Length..];
        if(plan.Files("src/Assets",".png")
            .Count(p=>string.Equals(Path.GetFileName(p),Path.GetFileName(artPath),StringComparison.OrdinalIgnoreCase))!=1)
            throw new AuthoringException("INVALID_ART","Duplicate embedded resource name");
        string textPrefix="items.authored."+string.Concat(spec.Key.Split('_').Select((s,i)=>i==0?s:char.ToUpperInvariant(s[0])+s[1..]));
        string nameKey=textPrefix+".name", descKey=textPrefix+".description";
        string def=$"[{Q(spec.Key)}] = new Def {{ Name = {Q(spec.Names["zh"])}, NameEn = {Q(spec.NameEn)}, Desc = {Q(spec.Description["zh"])}, DescEn = {Q(spec.Description["en"])}, Company = {Q(spec.Company)}, Art = {Q(art)}, Manufacturer = {Q(spec.Company)}, Value = {spec.Value}L, Types = new string[] {{ {string.Join(", ",spec.Types.Select(Q))} }}, W = {spec.Width}, H = {spec.Height}, Shape = {Q(spec.Cells)} }}";
        string identity=$"Good({Q(spec.Key)}, {Q(spec.Base)})";
        string texts=$"[{Q(spec.Key)}] = new Keys({Q(nameKey)}, null, {Q(descKey)}, null, null)";
        string catalog=$"new CatalogEntry {{ Id = {Q("@"+spec.Key)}, Page = {Q(spec.Page)}, MinTier = {spec.MinTier}, Label = {Q(spec.Names["zh"])}, LabelEn = {Q(spec.NameEn)}, LabelKey = {Q(nameKey)} }}";

        // Reject identities/keys outside the four exact editable nodes, including constant-based definitions.
        // This is a conservative source collision scan; compiled checks remain required after Apply.
        foreach(string relative in plan.Files("src/CorpPresence",".cs"))
        {
            if(new[]{Definitions,Identities,Texts,Catalog}.Contains(relative)) continue;
            if(CSharpSyntaxTree.ParseText(plan.ReadText(relative)).GetRoot().DescendantTokens()
                .Any(t=>t.IsKind(SyntaxKind.StringLiteralToken) && (t.ValueText==spec.Key || t.ValueText==spec.Id || t.ValueText=="@"+spec.Key)))
                throw new AuthoringException("REGISTRATION_CONFLICT","Goods identity already referenced by " + relative);
        }
        Edit(plan,Definitions,"initial",spec.Key,def);
        Edit(plan,Identities,"All",spec.Key,identity);
        Edit(plan,Texts,"Definitions",spec.Key,texts);
        Edit(plan,Catalog,"Entries","@"+spec.Key,catalog);
        var rows=new[]{(nameKey,spec.Names),(descKey,spec.Description)};
        AddJson(plan,"src/CorpPresence/Presentation/Translations.Keys.json",rows.Select(r=>(r.Item1,JsonSerializer.SerializeToElement(new[]{Translate(r.Item2,"ru"),Translate(r.Item2,"fr")}))));
        AddJson(plan,"tools/checks/LocalizationChecks/Migration.json",rows.Select(r=>(r.Item1,JsonSerializer.SerializeToElement(r.Item2["en"]))));
        foreach(string language in new[]{"en","ru","fr"})
            AddCsv(plan,"Localization/"+language+".csv",rows.Select(r=>(r.Item1,Translate(r.Item2,language))));
        return plan;
    }
    private static string Translate(IReadOnlyDictionary<string,string> text,string language)
        => text.TryGetValue(language,out string? translated)?translated:text["en"];
    private static void Edit(ChangePlan plan,string relative,string target,string key,string expression)
    {
        string original=plan.ReadText(relative);
        var tree=CSharpSyntaxTree.ParseText(original);
        if(tree.GetDiagnostics().Any(d=>d.Severity==DiagnosticSeverity.Error)) throw new AuthoringException("SOURCE_LAYOUT_CHANGED",relative);
        var root=tree.GetRoot();
        SyntaxNode owner = target=="All"
            ? root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single(n=>n.Identifier.ValueText==target)
            : root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(n=>n.Identifier.ValueText==target);
        var initializer=owner.DescendantNodes().OfType<InitializerExpressionSyntax>().First();
        if(root.DescendantTokens().Any(t=>t.IsKind(SyntaxKind.StringLiteralToken) &&
            (t.ValueText==key || t.ValueText=="corp_presence_"+key) && !initializer.Span.Contains(t.Span)))
            throw new AuthoringException("REGISTRATION_CONFLICT",relative+": key outside editable initializer");
        // Parse dictionary entries inside a valid initializer, rather than as standalone expressions.
        var parsed=SyntaxFactory.ParseExpression("new object {"+expression+"}")
            .DescendantNodes().OfType<InitializerExpressionSyntax>().First().Expressions.Single();
        var matches=initializer.Expressions.Where(e=>e.DescendantTokens().Any(t=>t.IsKind(SyntaxKind.StringLiteralToken) &&
            (t.ValueText==key || t.ValueText=="corp_presence_"+key))).ToArray();
        if(matches.Length>0)
        {
            if(matches.Length!=1 || !SyntaxFactory.AreEquivalent(matches[0],parsed))
                throw new AuthoringException("REGISTRATION_CONFLICT",relative+": "+key);
            return;
        }
        string nl=original.Contains("\r\n")?"\r\n":"\n";
        var updated=initializer.WithExpressions(initializer.Expressions.Add(parsed.NormalizeWhitespace()
            .WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(nl+"        ")).WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(nl))));
        SyntaxNode replaced;
        if(initializer.Parent is ArrayCreationExpressionSyntax array && target=="Entries")
        {
            var sizes=SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,SyntaxFactory.Literal(updated.Expressions.Count)));
            var rank=array.Type.RankSpecifiers.Single().WithSizes(sizes);
            replaced=root.ReplaceNode(array,array.WithInitializer(updated).WithType(array.Type.WithRankSpecifiers(SyntaxFactory.SingletonList(rank))));
        }
        else replaced=root.ReplaceNode(initializer,updated);
        string result=replaced.ToFullString();
        if(CSharpSyntaxTree.ParseText(result).GetDiagnostics().Any(d=>d.Severity==DiagnosticSeverity.Error))
            throw new AuthoringException("GENERATION_ERROR",relative);
        AddPreservingBom(plan,relative,result,"append reviewed C# registration node");
    }
    private static void AddJson(ChangePlan plan,string path,IEnumerable<(string Key,JsonElement Value)> rows)
    {
        string original=plan.ReadText(path);
        using var document=JsonDocument.Parse(original);
        var additions=new List<string>();
        foreach(var row in rows)
        {
            var matches=document.RootElement.EnumerateObject().Where(p=>p.Name==row.Key).ToArray();
            if(matches.Length>0)
            {
                if(matches.Length!=1 || matches[0].Value.ToString()!=row.Value.ToString())
                    throw new AuthoringException("REGISTRATION_CONFLICT",path+": "+row.Key);
            }
            else additions.Add(JsonSerializer.Serialize(row.Key)+": "+row.Value.GetRawText());
        }
        if(additions.Count==0) return;
        int end=original.LastIndexOf('}');
        string nl=original.Contains("\r\n")?"\r\n":"\n";
        string result=original[..end].TrimEnd()+(document.RootElement.EnumerateObject().Any()?",":"")+nl+"  "+string.Join(","+nl+"  ",additions)+nl+original[end..];
        AddPreservingBom(plan,path,result,"append stable text catalog/audit entries");
    }
    private static void AddCsv(ChangePlan plan,string path,IEnumerable<(string Key,string Value)> rows)
    {
        string original=plan.ReadText(path);
        using var parser=new TextFieldParser(new StringReader(original)) {HasFieldsEnclosedInQuotes=true,TrimWhiteSpace=false};
        parser.SetDelimiters(","); var existing=new Dictionary<string,string>();
        while(!parser.EndOfData)
        {
            var fields=parser.ReadFields();
            if(fields==null || fields.Length!=2 || !existing.TryAdd(fields[0],fields[1]))
                throw new AuthoringException("INVALID_CSV",path);
        }
        var additions=new List<string>();
        foreach(var row in rows)
            if(existing.TryGetValue(row.Key,out string? value))
            {if(value!=row.Value) throw new AuthoringException("REGISTRATION_CONFLICT",path+": "+row.Key);}
            else additions.Add(row.Key+",\""+row.Value.Replace("\"","\"\"")+"\"");
        if(additions.Count==0) return;
        string nl=original.Contains("\r\n")?"\r\n":"\n";
        AddPreservingBom(plan,path,original+(original.EndsWith('\n')?"":nl)+string.Join(nl,additions)+nl,"append translations (missing ru/fr use explicit English fallback)");
    }
    private static void AddPreservingBom(ChangePlan plan,string relative,string result,string reason)
    {
        byte[] original=plan.Read(relative);
        byte[] text=Encoding.UTF8.GetBytes(result);
        if(original.AsSpan().StartsWith(new byte[]{239,187,191})) text=[239,187,191,..text];
        plan.Add(relative,text,reason);
    }
}
