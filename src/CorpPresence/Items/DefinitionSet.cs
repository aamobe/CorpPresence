using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;

namespace CorpPresence;

internal sealed class DefinitionSet<T> : IEnumerable<KeyValuePair<string, T>> where T : class
{
    private readonly string defaultSource;
    private readonly Dictionary<string, T> values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> sources = new(StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, T> Values { get; }
    internal IReadOnlyDictionary<string, string> Sources { get; }
    internal DefinitionSet(string source = null)
    {
        defaultSource = source;
        Values = new ReadOnlyDictionary<string, T>(values);
        Sources = new ReadOnlyDictionary<string, string>(sources);
    }
    public T this[string key] { get => values[key]; set => Add(defaultSource, key, value); }
    public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    internal void Add(string source, string key, T value) =>
        AddRange(source, new[] { new KeyValuePair<string, T>(key, value) });
    internal void AddRange(string source, IEnumerable<KeyValuePair<string, T>> entries)
    {
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("定义来源不能为空", nameof(source));
        if (entries == null) throw new ArgumentNullException(nameof(entries));
        var pending = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var pair in entries)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                throw new ArgumentException($"无效物品定义：{source}/{pair.Key}");
            if (sources.TryGetValue(pair.Key, out var previous))
                throw new ArgumentException($"物品定义重复：{pair.Key}；来源 {previous} 与 {source}");
            if (pending.ContainsKey(pair.Key))
                throw new ArgumentException($"物品定义重复：{pair.Key}；来源 {source} 内重复");
            pending.Add(pair.Key, pair.Value);
        }
        foreach (var pair in pending)
        {
            values.Add(pair.Key, pair.Value);
            sources.Add(pair.Key, source);
        }
    }
}
