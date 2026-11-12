using CorpPresence;
int count=0;
void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);count++;}
var def=UfoBackpack.Definition();var bag=def.Shell();bag.identifier=UfoBackpack.ItemId;bag.Owned=true;
var row=(MachineryPanelRow)bag.contentWindow.childElement;var main=DirectoryMaster.Main;var bay=(GameSlotInventory)row.Panels[1].Content;
Check(def.W==3&&def.H==3&&def.Shape=="###|###|###","3x3 footprint");
Check(main.Width==8&&main.Height==9&&ReferenceEquals(main,row.Panels[0].Content),"original 8x9 inventory preserved as first saved inventory");
Check(bag.NativeEquipment&&bag.NativeAudio,"native equipment and audio retained");
Check(DirectoryMaster.Created.Count==1&&bay.childItem==null,"factory creates empty saved topology, not disposable child items");
Check(!main.mayInventoryAddItemFunc(new(){identifier="forbidden",Owned=true},main),"native main storage restrictions retained");
Check(bay.mayInventoryAddItemFunc(new(){identifier="toolbox",Owned=true},bay),"dedicated bay accepts owned native toolbox");
Check(!bay.mayInventoryAddItemFunc(new(){identifier="toolbox",Owned=false},bay)&&!bay.mayInventoryAddItemFunc(new(){identifier="other",Owned=true},bay),"bay rejects unowned and unrelated items");
Check(!bay.mayInventoryRemoveItemFunc(null,bay),"built-in toolbox fixed in bay");
Settings.CleanupForUninstall=true;Check(bay.mayInventoryRemoveItemFunc(null,bay),"uninstall can recover built-in toolbox");Settings.CleanupForUninstall=false;
Check(UfoBackpack.InitializeGift(bag)&&bay.childItem.identifier=="toolbox"&&bay.childItem.Owned,"gift includes actual owned native toolbox");
var original=bay.childItem;int made=DirectoryMaster.Created.Count;
Check(UfoBackpack.InitializeGift(bag)&&ReferenceEquals(original,bay.childItem)&&made==DirectoryMaster.Created.Count,"repeated initialization preserves existing toolbox and its contents");
bay.childItem=new(){identifier="foreign"};Check(!UfoBackpack.InitializeGift(bag)&&bay.childItem.identifier=="foreign","unexpected saved child never replaced");
bay.childItem=null;GameSlotInventory.FailAccept=true;
Check(!UfoBackpack.InitializeGift(bag)&&DirectoryMaster.Created.Last().Destroyed,"failed insertion cleans newly created toolbox");
GameSlotInventory.FailAccept=false;
Check(!UfoBackpack.InitializeGift(new(){identifier="other"}),"initialization limited to UFO backpack");
Console.WriteLine($"{count} UFO BACKPACK CHECKS PASSED");

bay.childItem=null;GameSlotInventory.ThrowAfterAccept=true;
try { UfoBackpack.InitializeGift(bag); } catch(Exception) { }
Check(bay.childItem!=null&&!bay.childItem.Destroyed,"post-insertion failure leaves child owned by bag for cleanup");
GameSlotInventory.ThrowAfterAccept=false;