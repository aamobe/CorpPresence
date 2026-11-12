using CorpPresence;

var gate = new GiftDeliveryGate();
int status = 1, placed = 0;
bool Current() => status == 1;
void Complete() => status = 2;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); }
Check(!gate.TryDeliver(() => false, () => { placed++; return true; }, Complete) && placed == 0, "stale session does not place");
Check(!gate.TryDeliver(Current, () => false, Complete) && status == 1, "full counter keeps pending");
try { gate.TryDeliver(Current, () => throw new InvalidOperationException(), Complete); } catch (InvalidOperationException) { }
Check(status == 1, "failed preparation keeps pending");
Check(gate.TryDeliver(Current, () => {
    placed++;
    Check(!gate.TryDeliver(Current, () => { placed++; return true; }, Complete), "placement reentry rejected");
    return true;
}, Complete), "successful delivery commits");
Check(status == 2 && placed == 1, "one delivery exactly");
Check(!gate.TryDeliver(Current, () => { placed++; return true; }, Complete) && placed == 1, "claimed gift cannot repeat");
status = 1;
Check(gate.TryDeliver(Current, () => true, Complete), "new pending session can deliver");

void Reset(){SaveSlot.CurrentStore=new();CorpState.Current=new();SaveSlot.Writable=true;Goods.Made.Clear();Goods.FailKey=null;Crates.Fail=false;Settings.CleanupForUninstall=false;PhoneUIManager.Instance=new();}
// Native phone UI skips the first line startAction, then executes endAction.
void Dial(){var phones=new Dictionary<long,StorePhoneClient>();MangoGift.Register(phones);PhoneUIManager.Instance.currentPhoneClient=phones[1231];var d=MangoGift.Call();d.End?.Invoke();}
StoreClient Arrive(){var s=SaveSlot.CurrentStore;MangoGift.Schedule(s.storeClientManager);var c=s.storeClientManager.clientStack[0];s.storeClientManager.clientStack.RemoveAt(0);s.currentClientInstance=new Client{Blueprint=c};return c;}
Reset();var dict=new Dictionary<long,StorePhoneClient>();MangoGift.Register(dict);
Check(dict[1231].phoneClientType==StorePhoneClient.PhoneClientType.None,"gift is not reputation-gated supplier");
dict[1231].phoneClientType=StorePhoneClient.PhoneClientType.Supplier;MangoGift.Register(dict);
Check(dict[1231].phoneClientType==StorePhoneClient.PhoneClientType.None,"existing gift phone repaired");
var foreign=new StorePhoneClient{locID="foreign"};dict[1231]=foreign;MangoGift.Register(dict);
Check(ReferenceEquals(dict[1231],foreign),"foreign phone preserved");
Dial();Dial();Check(CorpState.Current.MangoGiftStatus==1&&SaveSlot.CurrentStore.storeClientManager.clientStack.Count==1,"repeat calls queue exactly one visitor");
Check(PhoneUIManager.Instance.Ended==2&&PhoneUIManager.Instance.currentPhoneClient==null,"pending calls hang up normally");
var visitor=Arrive();Check(visitor.displayName=="Aamobe"&&visitor.spriteName=="malice_envoy"&&visitor.mainDialogue.Text=="感谢芒果老师的支持","exact visitor appearance name and thanks");
SaveSlot.CurrentStore.Full=true;visitor.mainDialogue.Start();
Check(CorpState.Current.MangoGiftStatus==1&&Goods.Made.All(i=>i.destroyed),"full counter keeps pending and destroys all drafts");
Goods.Made.Clear();SaveSlot.CurrentStore.Full=false;SaveSlot.CurrentStore.OnPlace=()=>visitor.mainDialogue.Start();
visitor.mainDialogue.Start();var gift=SaveSlot.CurrentStore.Placed;
Check(CorpState.Current.MangoGiftStatus==2&&Goods.Made.Count==7,"placement reentry delivers one box only");
Check(gift.Items.Count(i=>i.Key==CargoWarehouse.MangoKey)==1&&gift.Items.Count(i=>i.Key==CargoWarehouse.ModuleKey)==4&&gift.Items.Count(i=>i.Key==CargoWarehouse.ControllerKey)==1,"box contains exactly mango plus four modules plus controller");
Check(MangoGift.BackpackDefinition().W==2&&MangoGift.BackpackDefinition().H==2,"gift box footprint two by two");
Check(!MangoGift.BackpackDefinition().Crate,"outer mango is reusable, never a disposable crate");
var backpack=MangoGift.BackpackDefinition().Shell();
Check(backpack.Backpack && !backpack.destroyed,"native backpack factory, no empty cleanup");
int count=Goods.Made.Count;Dial();visitor.mainDialogue.Start();Check(Goods.Made.Count==count&&PhoneUIManager.Instance.currentPhoneClient==null,"claimed call ends without another gift");
Reset();SaveSlot.Writable=false;Dial();Check(CorpState.Current.MangoGiftStatus==0&&PhoneUIManager.Instance.Ended==1,"protected record not mutated and phone ends");
Reset();Dial();visitor=Arrive();var old=CorpState.Current;SaveSlot.CurrentStore=new();CorpState.Current=new();visitor.mainDialogue.Start();Check(Goods.Made.Count==0&&old.MangoGiftStatus==1,"old visitor cannot deliver into new save");
Reset();Dial();visitor=Arrive();Goods.FailKey=CargoWarehouse.ControllerKey;visitor.mainDialogue.Start();Check(CorpState.Current.MangoGiftStatus==1&&Goods.Made.All(i=>i.destroyed),"missing controller aborts entire gift without claiming");
Reset();Dial();visitor=Arrive();SaveSlot.CurrentStore.ThrowAfterPlacement=true;visitor.mainDialogue.Start();Check(CorpState.Current.MangoGiftStatus==2&&!SaveSlot.CurrentStore.Placed.destroyed,"native post-placement exception still commits observed delivery");

void DialWaste(){var phones=new Dictionary<long,StorePhoneClient>();WasteBatteryGift.Register(phones);PhoneUIManager.Instance.currentPhoneClient=phones[119];WasteBatteryGift.Call().End?.Invoke();}
StoreClient ArriveWaste(){var s=SaveSlot.CurrentStore;WasteBatteryGift.Schedule(s.storeClientManager);var c=s.storeClientManager.clientStack[0];s.storeClientManager.clientStack.RemoveAt(0);s.currentClientInstance=new Client{Blueprint=c};return c;}
Reset();var wastePhones=new Dictionary<long,StorePhoneClient>{{119,foreign}};WasteBatteryGift.Register(wastePhones);Check(ReferenceEquals(wastePhones[119],foreign),"0119 preserves foreign registration");
DialWaste();DialWaste();Check(CorpState.Current.WasteBatteryGiftStatus==1&&SaveSlot.CurrentStore.storeClientManager.clientStack.Count==1,"0119 queues one gift");
visitor=ArriveWaste();Check(visitor.mainDialogue.Text=="感谢久的大力支持","exact Jiu dialogue");
SaveSlot.CurrentStore.Full=true;visitor.mainDialogue.Start();Check(CorpState.Current.WasteBatteryGiftStatus==1&&Goods.Made.All(i=>i.destroyed),"waste gift full counter preserves pending");
Goods.Made.Clear();SaveSlot.CurrentStore.Full=false;SaveSlot.CurrentStore.OnPlace=()=>visitor.mainDialogue.Start();visitor.mainDialogue.Start();
Check(CorpState.Current.WasteBatteryGiftStatus==2&&Goods.Made.Count==1&&Goods.Made[0].Key=="waste_battery","waste gift delivers exactly one battery");
DialWaste();visitor.mainDialogue.Start();Check(Goods.Made.Count==1&&CorpState.Current.MangoGiftStatus==0,"waste gift neither repeats nor claims mango");
Reset();DialWaste();visitor=ArriveWaste();old=CorpState.Current;SaveSlot.CurrentStore=new();CorpState.Current=new();visitor.mainDialogue.Start();Check(Goods.Made.Count==0&&old.WasteBatteryGiftStatus==1,"waste gift rejects stale session");

void DialUfo(){var phones=new Dictionary<long,StorePhoneClient>();UfoBackpackGift.Register(phones);PhoneUIManager.Instance.currentPhoneClient=phones[9985];UfoBackpackGift.Call().End?.Invoke();}
StoreClient ArriveUfo(){var s=SaveSlot.CurrentStore;UfoBackpackGift.Schedule(s.storeClientManager);var c=s.storeClientManager.clientStack[0];s.storeClientManager.clientStack.RemoveAt(0);s.currentClientInstance=new Client{Blueprint=c};return c;}
Reset();var ufoPhones=new Dictionary<long,StorePhoneClient>{{9985,foreign}};UfoBackpackGift.Register(ufoPhones);Check(ReferenceEquals(ufoPhones[9985],foreign),"9985 preserves foreign registration");
DialUfo();DialUfo();Check(CorpState.Current.UfoBackpackGiftStatus==1&&SaveSlot.CurrentStore.storeClientManager.clientStack.Count==1,"9985 queues one gift");
visitor=ArriveUfo();Check(visitor.mainDialogue.Text=="飞碟赠礼","exact UFO gift dialogue");
SaveSlot.CurrentStore.Full=true;visitor.mainDialogue.Start();Check(CorpState.Current.UfoBackpackGiftStatus==1&&Goods.Made.All(i=>i.destroyed),"ufo gift full counter preserves pending");
Goods.Made.Clear();SaveSlot.CurrentStore.Full=false;SaveSlot.CurrentStore.OnPlace=()=>visitor.mainDialogue.Start();visitor.mainDialogue.Start();
Check(CorpState.Current.UfoBackpackGiftStatus==2&&Goods.Made.Count==1&&Goods.Made[0].Key=="ufo_backpack","ufo gift delivers exactly one backpack");
DialUfo();visitor.mainDialogue.Start();Check(Goods.Made.Count==1&&CorpState.Current.MangoGiftStatus==0,"ufo gift neither repeats nor claims mango");
Reset();DialUfo();visitor=ArriveUfo();old=CorpState.Current;SaveSlot.CurrentStore=new();CorpState.Current=new();visitor.mainDialogue.Start();Check(Goods.Made.Count==0&&old.UfoBackpackGiftStatus==1,"ufo gift rejects stale session");

Reset();UfoBackpack.Fail=true;DialUfo();visitor=ArriveUfo();visitor.mainDialogue.Start();
Check(CorpState.Current.UfoBackpackGiftStatus==1 && Goods.Made.Count==1 && Goods.Made[0].destroyed && SaveSlot.CurrentStore.Placed==null,"failed built-in toolbox prevents delivery and cleans bag");
UfoBackpack.Fail=false;visitor.mainDialogue.Start();
Check(CorpState.Current.UfoBackpackGiftStatus==2 && SaveSlot.CurrentStore.Placed!=null,"toolbox initialization failure remains retryable");
void DialFufu(){var phones=new Dictionary<long,StorePhoneClient>();FufuDollGift.Register(phones);PhoneUIManager.Instance.currentPhoneClient=phones[1215];FufuDollGift.Call().End?.Invoke();}
StoreClient ArriveFufu(){var s=SaveSlot.CurrentStore;FufuDollGift.Schedule(s.storeClientManager);var c=s.storeClientManager.clientStack[0];s.storeClientManager.clientStack.RemoveAt(0);s.currentClientInstance=new Client{Blueprint=c};return c;}
Reset();var fufuPhones=new Dictionary<long,StorePhoneClient>{{1215,foreign}};FufuDollGift.Register(fufuPhones);Check(ReferenceEquals(fufuPhones[1215],foreign),"1215 preserves foreign phone");
DialFufu();DialFufu();Check(CorpState.Current.FufuDollGiftStatus==1&&SaveSlot.CurrentStore.storeClientManager.clientStack.Count==1,"1215 queues exactly once");
visitor=ArriveFufu();Check(visitor.mainDialogue.Text=="感谢芙老师的支持。","exact Fufu thank-you dialogue");
SaveSlot.CurrentStore.Full=true;visitor.mainDialogue.Start();Check(CorpState.Current.FufuDollGiftStatus==1&&Goods.Made.All(i=>i.destroyed),"full counter retains Fufu gift");
SaveSlot.CurrentStore.Full=false;Goods.Made.Clear();visitor.mainDialogue.Start();Check(CorpState.Current.FufuDollGiftStatus==2&&Goods.Made.Count==1&&Goods.Made[0].Key=="fufu_doll","Fufu delivered once");
DialFufu();visitor.mainDialogue.Start();Check(Goods.Made.Count==1&&CorpState.Current.MangoGiftStatus==0,"Fufu no repeat or unrelated claims");
Reset();DialFufu();visitor=ArriveFufu();old=CorpState.Current;SaveSlot.CurrentStore=new();CorpState.Current=new();visitor.mainDialogue.Start();Check(Goods.Made.Count==0&&old.FufuDollGiftStatus==1,"Fufu rejects stale save");
Reset();SaveSlot.Writable=false;DialFufu();Check(CorpState.Current.FufuDollGiftStatus==0,"Fufu respects protected save");
