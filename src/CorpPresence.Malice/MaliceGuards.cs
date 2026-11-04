namespace CorpPresence.Malice;

internal static class MaliceGuards
{
    internal static bool CanPauseBombDeadline(string chapter, int day, int deadline, int lastPauseDay, bool blocked)
        => blocked && chapter == "bomb" && deadline >= 0 && day <= deadline
           && day > lastPauseDay && deadline < int.MaxValue;

    internal static bool CanDeliverBomb(string chapter, int day, int deadline, bool blocked, string buyerId)
        => chapter == "bomb" && deadline >= 0 && day <= deadline && !blocked
           && buyerId == "rebel_odin_envoy_bomb";
}
