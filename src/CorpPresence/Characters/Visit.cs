using System;

namespace CorpPresence;

internal sealed class Visit
{
	public string[] Lines;

	public string Exit;

	public bool Buy;

	public bool Both;

	public int Discount;

	public string[] Stock = Array.Empty<string>();

	public string[] Gift = Array.Empty<string>();

	public string[] Extra = Array.Empty<string>();
	public bool RequireExtra;

	public string Wrong = L.T("别糊弄我。", "Don't try to fool me.");

	public bool TalkOnly;

	public string Ask;

	public Choice[] Choices = Array.Empty<Choice>();
}
