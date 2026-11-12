using CorpPresence;
foreach(var (chance,expected) in new (int,float)[]{(-100000,0),(0,0),(1,0.5f),(6,3),(33,16.5f),(40,20),(100,50),(150,50)}){
 if(FufuTheftChance.Apply(chance,true)!=expected)throw new Exception("Wrong active chance "+chance);
 if(FufuTheftChance.Apply(chance,false)!=chance)throw new Exception("Changed vanilla chance "+chance);
 Console.WriteLine("PASS theft chance "+chance);
}
