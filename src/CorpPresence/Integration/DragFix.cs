using System.Collections.Generic;

namespace CorpPresence;

internal static class DragFix
{
	public static void BeforeEndDrag(List<TreeNodeRender> ___highlightedMayNodes)
	{
		try
		{
			___highlightedMayNodes?.RemoveAll((TreeNodeRender n) => n == null || n.image == null);
		}
		catch
		{
		}
	}

	public static void AfterEndDrag(List<TreeNodeRender> ___highlightedMayNodes)
	{
		try
		{
			___highlightedMayNodes?.Clear();
		}
		catch
		{
		}
	}
}
