// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System.Collections.Concurrent;

namespace Microsoft.Maui.Controls;

/// <summary>
/// Drop-in replacement for the deprecated MessagingCenter (internal in .NET 10).
/// Replicates the same API so existing code doesn't need changes.
/// </summary>
public static class MessagingCenter
{
    private static readonly ConcurrentDictionary<string, List<Subscription>> _subscriptions = new();
    private static readonly object _lock = new();

    // --- Send<TSender>(sender, message) ---
    public static void Send<TSender>(TSender sender, string message) where TSender : class
    {
        var key = MakeKey<TSender>(message);
        if (!_subscriptions.TryGetValue(key, out var subs)) return;

        List<Subscription> snapshot;
        lock (_lock) { snapshot = subs.ToList(); }

        foreach (var sub in snapshot)
        {
            if (sub is Subscription<TSender> typed)
                typed.Callback(sender);
        }
    }

    // --- Send<TSender, TArgs>(sender, message, args) ---
    public static void Send<TSender, TArgs>(TSender sender, string message, TArgs args) where TSender : class
    {
        var key = MakeKey<TSender, TArgs>(message);
        if (!_subscriptions.TryGetValue(key, out var subs)) return;

        List<Subscription> snapshot;
        lock (_lock) { snapshot = subs.ToList(); }

        foreach (var sub in snapshot)
        {
            if (sub is Subscription<TSender, TArgs> typed)
                typed.Callback(sender, args);
        }
    }

    // --- Subscribe<TSender>(subscriber, message, callback) ---
    public static void Subscribe<TSender>(object subscriber, string message, Action<TSender> callback) where TSender : class
    {
        var key = MakeKey<TSender>(message);
        var sub = new Subscription<TSender>(subscriber, callback);

        lock (_lock)
        {
            var list = _subscriptions.GetOrAdd(key, _ => new List<Subscription>());
            list.Add(sub);
        }
    }

    // --- Subscribe<TSender, TArgs>(subscriber, message, callback) ---
    public static void Subscribe<TSender, TArgs>(object subscriber, string message, Action<TSender, TArgs> callback) where TSender : class
    {
        var key = MakeKey<TSender, TArgs>(message);
        var sub = new Subscription<TSender, TArgs>(subscriber, callback);

        lock (_lock)
        {
            var list = _subscriptions.GetOrAdd(key, _ => new List<Subscription>());
            list.Add(sub);
        }
    }

    // --- Unsubscribe<TSender>(subscriber, message) ---
    public static void Unsubscribe<TSender>(object subscriber, string message) where TSender : class
    {
        var key = MakeKey<TSender>(message);
        if (!_subscriptions.TryGetValue(key, out var subs)) return;

        lock (_lock)
        {
            subs.RemoveAll(s => ReferenceEquals(s.Subscriber, subscriber));
        }
    }

    // --- Unsubscribe<TSender, TArgs>(subscriber, message) ---
    public static void Unsubscribe<TSender, TArgs>(object subscriber, string message) where TSender : class
    {
        var key = MakeKey<TSender, TArgs>(message);
        if (!_subscriptions.TryGetValue(key, out var subs)) return;

        lock (_lock)
        {
            subs.RemoveAll(s => ReferenceEquals(s.Subscriber, subscriber));
        }
    }

    // --- Key helpers ---
    private static string MakeKey<TSender>(string message) => $"{typeof(TSender).FullName}|{message}";
    private static string MakeKey<TSender, TArgs>(string message) => $"{typeof(TSender).FullName}|{typeof(TArgs).FullName}|{message}";

    // --- Subscription types ---
    private abstract class Subscription
    {
        public object Subscriber { get; }
        protected Subscription(object subscriber) => Subscriber = subscriber;
    }

    private sealed class Subscription<TSender> : Subscription where TSender : class
    {
        public Action<TSender> Callback { get; }
        public Subscription(object subscriber, Action<TSender> callback) : base(subscriber) => Callback = callback;
    }

    private sealed class Subscription<TSender, TArgs> : Subscription where TSender : class
    {
        public Action<TSender, TArgs> Callback { get; }
        public Subscription(object subscriber, Action<TSender, TArgs> callback) : base(subscriber) => Callback = callback;
    }
}


