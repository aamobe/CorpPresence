namespace CorpPresence;

internal static class ReadingPace
{
	private const float DialoguePauseMultiplier = 1.15f;
	private const float TypewriterSpeedMultiplier = 0.9f;
	internal const float NoticeExtraSeconds = 1.5f;

	internal static void AfterDialoguePause(ref float __result) => __result *= DialoguePauseMultiplier;

	internal static void AfterTypewriterSpeed(ref float __result) => __result *= TypewriterSpeedMultiplier;

	internal static void AfterNotification(ReputationUIManager __instance) => __instance.notifierTimer += NoticeExtraSeconds;
}
