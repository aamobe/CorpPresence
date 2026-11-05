using System;

namespace CorpPresence;

internal sealed record Choice(string Label, string Reply, Action Then);
