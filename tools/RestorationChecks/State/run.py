from pathlib import Path
import re,subprocess
root=Path(__file__).resolve().parents[3]
src=root/'src/CorpPresence'
goods=(src/'Items/Goods.cs').read_text(encoding='utf-8-sig')
identities=(src/'Items/ItemIdentity.cs').read_text(encoding='utf-8-sig')
definitions=set(re.findall(r'\["(crate_[^"]+)"\]\s*=',goods))
ids=set(re.findall(r'Good\("(crate_[^"]+)"',identities))
registered=sorted(definitions&ids)
data='namespace CorpPresence { static partial class Goods { internal static readonly System.Collections.Generic.HashSet<string> RegisteredCrates = new() {'+','.join('"'+x+'"' for x in registered)+'}; } }'
(Path(__file__).parent/'RegisteredCrates.cs').write_text(data,encoding='utf-8')
raise SystemExit(subprocess.call(['dotnet','run','--project',str(Path(__file__).parent/'State.csproj'),'-c','Release']))
