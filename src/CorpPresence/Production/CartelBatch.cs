namespace CorpPresence;

internal static class CartelBatch
{
    internal static int Advance(int state) => state == 1 ? 2 : state;
    internal static int AfterOutput(int state, bool placed) => state == 2 && placed ? 0 : state;
}
