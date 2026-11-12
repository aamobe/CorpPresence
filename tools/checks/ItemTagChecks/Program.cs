using CorpPresence;
int passed=0;
void Require(bool value,string name){if(!value)throw new Exception(name);}
void Check(string name,Action run){run();passed++;Console.WriteLine("PASS "+name);}
GameItem Sample(string id="corp_presence_silver_blood") {
 var item=new GameItem {identifier=id};
 foreach(var state in new[]{item.state,item.modifiedState}){
  state.dict["LIQUID_CONTAINER_TAG"]=1;state.dict["LIQUID_CONTAINER_BASE_PRICE_TAG"]=3;
  state.dict["LIQUID_CONTAINER_CAPACITY"]=50000;state.dict["CURRENT_PART_WATER"]=28000;
  state.dict["CURRENT_PART_CORP_ISOMER_RAW"]=10000;state.dict["CONTAINER_TEMP_TAG"]=67;
 }
 item.itemTypes.Add("WATER");return item;
}
void VerifyRepair(GameItem item,bool filled,Action<GameItem> repair){
 var state=item.state.dict.ToArray();var modified=item.modifiedState.dict.ToArray();
 long value=item.unitValue;int called=0;
 WaterHelper.OnClassify=x=>{
  Require(ReferenceEquals(x,item),"wrong container");Require(x.itemTypes.Contains("MEDICAL"),"classification must follow static types");called++;
  // Contract fixture for the original API's result, not a reimplementation of its formula.
  x.RemoveGameItemType(filled?"HOUSEHOLD_GOOD":"WATER");x.SetGameItemType(filled?"WATER":"HOUSEHOLD_GOOD");
 };
 repair(item);
 Require(called==1,"native classification must be restored once");
 Require(item.itemTypes.Contains(filled?"WATER":"HOUSEHOLD_GOOD"),"dynamic classification lost");
 Require(item.itemTypes.Contains("MEDICAL"),"content category lost");
 Require(state.SequenceEqual(item.state.dict)&&modified.SequenceEqual(item.modifiedState.dict)&&value==item.unitValue,"repair mutated contents, temperature, capacity or price");
}
Check("loaded silver blood restores native classification without changing contents",()=>VerifyRepair(Sample(),true,ItemTags.Repair));
Check("empty isomer vial uses native empty-container classification",()=>{
 var item=Sample("corp_presence_isomer_vial");foreach(var s in new[]{item.state,item.modifiedState}){s.dict["CURRENT_PART_WATER"]=0;s.dict["CURRENT_PART_CORP_ISOMER_RAW"]=0;}
 VerifyRepair(item,false,ItemTags.Repair);
});
Check("non-water constituent still delegates classification to native API",()=>{
 var item=Sample();foreach(var s in new[]{item.state,item.modifiedState})s.dict["CURRENT_PART_WATER"]=0;
 VerifyRepair(item,true,ItemTags.Repair);
});
Check("repeated repair preserves restored category and saved state",()=>{var item=Sample();VerifyRepair(item,true,ItemTags.Repair);VerifyRepair(item,true,ItemTags.Repair);});
Check("creation Apply also respects dynamic classification",()=>VerifyRepair(Sample(),true,x=>ItemTags.Apply(x,new[]{"MEDICAL"},Array.Empty<string>())));
Check("ordinary item never enters native liquid classifier",()=>{
 WaterHelper.OnClassify=_=>throw new Exception("non-liquid classified");var item=new GameItem{identifier="corp_presence_tool"};ItemTags.Repair(item);Require(item.itemTypes.SequenceEqual(new[]{"MEDICAL"}),"ordinary types changed");
});
Check("native and foreign containers remain untouched during repair",()=>{
 WaterHelper.OnClassify=_=>throw new Exception("foreign container classified");
 foreach(var id in new[]{"water_bottle","another_mod_bottle"}){var item=Sample(id);var tags=item.modifiedState.dict.ToArray();ItemTags.Repair(item);Require(item.itemTypes.SequenceEqual(new[]{"WATER"})&&tags.SequenceEqual(item.modifiedState.dict),"foreign item changed");}
});
Check("AfterDecode repairs nested mod liquid without classifying its native root",()=>{
 var item=Sample();var root=new GameItem{identifier="native_crate"};root.Children.Add(item);
 VerifyRepair(item,true,_=>ItemTags.AfterDecode(root));
});
Console.WriteLine($"ITEM TAG CHECKS PASSED: {passed}");

Check("UFO backpack keeps native capability after category normalization",()=>{
 var bag=new GameItem{identifier=UfoBackpack.ItemId};
 bag.EnableTag("backpack");bag.EnableTag("equippable");bag.EnableTag("CONTAINER_TAG");
 ItemTags.Apply(bag,Array.Empty<string>(),Array.Empty<string>());
 Require(bag.IsTag("BACKPACK_TAG")&&bag.IsTag("backpack")&&bag.IsTag("equippable")&&bag.IsTag("CONTAINER_TAG"),"backpack capability lost");
});