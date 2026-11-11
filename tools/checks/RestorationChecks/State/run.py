from pathlib import Path
import re,subprocess,argparse
parser=argparse.ArgumentParser()
parser.add_argument("--dotnet",default="dotnet")
parser.add_argument("--artifacts-path")
options=parser.parse_args()
root=Path(__file__).resolve().parents[4]
src=root/'src/CorpPresence'
goods=(src/'Items/Goods.Definitions.cs').read_text(encoding='utf-8-sig')
identities=(src/'Items/ItemIdentity.cs').read_text(encoding='utf-8-sig')
definitions=set(re.findall(r'\["(crate_[^"]+)"\]\s*=',goods))
ids=set(re.findall(r'Good\("(crate_[^"]+)"',identities))
registered=sorted(definitions&ids)
if not registered:
    raise SystemExit('No registered crate definitions found; verify the production definition source path.')
data='namespace CorpPresence { static partial class Goods { internal static readonly System.Collections.Generic.HashSet<string> RegisteredCrates = new() {'+','.join('"'+x+'"' for x in registered)+'}; } }'
(Path(__file__).parent/'RegisteredCrates.cs').write_text(data,encoding='utf-8')
artifacts=Path(options.artifacts_path).resolve() if options.artifacts_path else root/'artifacts/restoration-checks'
command=[options.dotnet,'build',str(Path(__file__).parent/'State.csproj'),'-c','Release','--artifacts-path',str(artifacts)]
code=subprocess.call(command)
if code:raise SystemExit(code)
raise SystemExit(subprocess.call([options.dotnet,str(artifacts/'bin/State/release/State.dll')]))
