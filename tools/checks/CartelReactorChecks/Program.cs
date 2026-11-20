using System;
using CorpPresence;

static void Check(bool passed) { if (!passed) throw new Exception("Batch state assertion failed"); }
Check(CartelBatch.Advance(0) == 0);
Check(CartelBatch.Advance(1) == 2);
Check(CartelBatch.Advance(2) == 2);
Check(CartelBatch.AfterOutput(2, false) == 2);
Check(CartelBatch.AfterOutput(2, true) == 0);
Console.WriteLine("5 cartel batch checks passed.");
