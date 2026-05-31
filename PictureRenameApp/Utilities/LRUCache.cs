using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureRenameApp.Utilities
{
    /// <summary>
    /// Generic LRU (Least Recently Used) cache implementation with configurable capacity and expiration.
    /// Provides efficient memory management for frequently accessed items.
    /// </summary>
    /// <typeparam name="TKey">Type of cache key</typeparam>
    /// <typeparam name="TValue">Type of cached value</typeparam>
    public class LRUCache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly Dictionary<TKey, CacheEntry> _cache;
        private readonly LinkedList<TKey> _accessOrder;
        private readonly int _maxCapacity;
        private readonly TimeSpan _expirationTime;
        private readonly object _lockObject = new object();

        private class CacheEntry
        {
            public TValue Value { get; set; }
            public DateTime CreatedAt { get; set; }
            public LinkedListNode<TKey>? OrderNode { get; set; }

            public CacheEntry(TValue value)
            {
                Value = value;
                CreatedAt = DateTime.UtcNow;
            }

            public bool IsExpired(TimeSpan expirationTime)
            {
                return DateTime.UtcNow - CreatedAt > expirationTime;
            }
        }

        /// <summary>
        /// Initializes a new instance of LRUCache.
        /// </summary>
        /// <param name="maxCapacity">Maximum number of items to store</param>
        /// <param name="expirationMinutes">Minutes before an item expires (0 = no expiration)</param>
        public LRUCache(int maxCapacity = 100, int expirationMinutes = 60)
        {
            if (maxCapacity <= 0)
                throw new ArgumentException("Max capacity must be greater than 0", nameof(maxCapacity));

            _maxCapacity = maxCapacity;
            _expirationTime = expirationMinutes > 0 ? TimeSpan.FromMinutes(expirationMinutes) : TimeSpan.MaxValue;
            _cache = new Dictionary<TKey, CacheEntry>();
            _accessOrder = new LinkedList<TKey>();
        }

        /// <summary>
        /// Attempts to get a value from the cache.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue? value)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (entry.IsExpired(_expirationTime))
                    {
                        RemoveInternal(key);
                        value = default;
                        return false;
                    }

                    // Move to end (most recently used)
                    if (entry.OrderNode != null)
                    {
                        _accessOrder.Remove(entry.OrderNode);
                    }
                    entry.OrderNode = _accessOrder.AddLast(key);
                    value = entry.Value;
                    return true;
                }

                value = default;
                return false;
            }
        }

        /// <summary>
        /// Adds or updates a value in the cache.
        /// If capacity is exceeded, removes the least recently used item.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            lock (_lockObject)
            {
                // Remove existing entry if present
                if (_cache.ContainsKey(key))
                {
                    RemoveInternal(key);
                }

                // If cache is full, remove LRU item
                if (_cache.Count >= _maxCapacity && _accessOrder.First != null)
                {
                    RemoveInternal(_accessOrder.First.Value);
                }

                // Add new entry
                var entry = new CacheEntry(value);
                entry.OrderNode = _accessOrder.AddLast(key);
                _cache[key] = entry;
            }
        }

        /// <summary>
        /// Removes a specific key from the cache.
        /// </summary>
        public bool Remove(TKey key)
        {
            lock (_lockObject)
            {
                return RemoveInternal(key);
            }
        }

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _cache.Clear();
                _accessOrder.Clear();
            }
        }

        /// <summary>
        /// Gets the current number of items in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _cache.Count;
                }
            }
        }

        private bool RemoveInternal(TKey key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.OrderNode != null)
                {
                    _accessOrder.Remove(entry.OrderNode);
                }
                _cache.Remove(key);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
