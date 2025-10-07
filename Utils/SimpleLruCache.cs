using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ComicReader.Utils
{
    /// <summary>
    /// Caché LRU (Least Recently Used) simple, segura para hilos (lock interno).
    /// - Lecturas promocionan la entrada al frente (más reciente).
    /// - Escribe/actualiza inserta al frente y, si supera la capacidad, expulsa el menos reciente.
    /// - Sin romper compatibilidad: API existente intacta. Se agregan utilidades opcionales.
    /// </summary>
    public class SimpleLruCache<TKey, TValue> where TKey : notnull
    {
        private int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<(TKey key, TValue value)>> _map;
        private readonly LinkedList<(TKey key, TValue value)> _list;
        private readonly object _sync = new();
    private readonly Action<TKey, TValue>? _onEvicted; // opcional
    private readonly bool _autoDisposeOnEvict;
    private long _hitCount;
    private long _missCount;
    private long _evictionCount;

        public int Count => _map.Count;
        public int Capacity => _capacity;
    public long HitCount => Interlocked.Read(ref _hitCount);
    public long MissCount => Interlocked.Read(ref _missCount);
    public long EvictionCount => Interlocked.Read(ref _evictionCount);

        /// <summary>
        /// Crea la caché con capacidad fija.
        /// </summary>
        public SimpleLruCache(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>();
            _list = new LinkedList<(TKey, TValue)>();
        }

        /// <summary>
        /// Crea la caché con capacidad y comparador de claves personalizado.
        /// </summary>
        public SimpleLruCache(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(comparer);
            _list = new LinkedList<(TKey, TValue)>();
        }

        /// <summary>
        /// Crea la caché con capacidad, comparador y callback opcional que se invoca al expulsar elementos por LRU.
        /// El callback se ejecuta fuera del lock para evitar deadlocks.
        /// </summary>
        public SimpleLruCache(int capacity, Action<TKey, TValue> onEvicted, IEqualityComparer<TKey>? comparer = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _onEvicted = onEvicted;
            _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(comparer ?? EqualityComparer<TKey>.Default);
            _list = new LinkedList<(TKey, TValue)>();
        }

        /// <summary>
        /// Crea la caché con capacidad, comparador y opción para Dispose automático de valores expulsados si implementan IDisposable.
        /// </summary>
        public SimpleLruCache(int capacity, bool autoDisposeOnEvict, IEqualityComparer<TKey>? comparer = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _autoDisposeOnEvict = autoDisposeOnEvict;
            _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(comparer ?? EqualityComparer<TKey>.Default);
            _list = new LinkedList<(TKey, TValue)>();
        }

        public bool TryGet(TKey key, out TValue value)
        {
            bool hit;
            lock (_sync)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    // mover al frente
                    _list.Remove(node);
                    _list.AddFirst(node);
                    value = node.Value.value;
                    hit = true;
                }
                else
                {
                    value = default!;
                    hit = false;
                }
            }
            if (hit) Interlocked.Increment(ref _hitCount); else Interlocked.Increment(ref _missCount);
            return hit;
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            bool hadEviction = false;
            TKey evKey = default!;
            TValue evVal = default!;
            lock (_sync)
            {
                if (_map.TryGetValue(key, out var existing))
                {
                    existing.Value = (key, value);
                    _list.Remove(existing);
                    _list.AddFirst(existing);
                    // no eviction
                }
                else
                {
                    var node = new LinkedListNode<(TKey, TValue)>((key, value));
                    _list.AddFirst(node);
                    _map[key] = node;
                    if (_map.Count > _capacity)
                    {
                        var last = _list.Last;
                        if (last != null)
                        {
                            _list.RemoveLast();
                            _map.Remove(last.Value.key);
                            hadEviction = true;
                            evKey = last.Value.key;
                            evVal = last.Value.value;
                        }
                    }
                }
            }
            // Notificar fuera del lock
            if (hadEviction) NotifyEvicted(evKey, evVal);
        }

        /// <summary>
        /// Obtiene si existe y actualiza la prioridad LRU; si no existe, crea con valueFactory y lo inserta.
        /// Devuelve el valor final.
        /// </summary>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));
            if (TryGet(key, out var existing)) return existing;
            var created = valueFactory(key); // crear fuera del lock para evitar trabajo pesado bajo lock
            AddOrUpdate(key, created);
            return created;
        }

        /// <summary>
        /// Intenta obtener sin mover a más reciente (no modifica el orden LRU).
        /// </summary>
        public bool TryPeek(TKey key, out TValue value)
        {
            lock (_sync)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    value = node.Value.value;
                    return true;
                }
                value = default!;
                return false;
            }
        }

        /// <summary>
        /// Indica si la clave existe (O(1)).
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            lock (_sync) { return _map.ContainsKey(key); }
        }

        /// <summary>
        /// Devuelve una instantánea de las claves en orden del más reciente al menos reciente.
        /// </summary>
        public IReadOnlyList<TKey> KeysSnapshot()
        {
            lock (_sync) { return _list.Select(n => n.key).ToList(); }
        }

        /// <summary>
        /// Devuelve una instantánea de los valores en orden del más reciente al menos reciente.
        /// </summary>
        public IReadOnlyList<TValue> ValuesSnapshot()
        {
            lock (_sync) { return _list.Select(n => n.value).ToList(); }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _map.Clear();
                _list.Clear();
            }
        }

        /// <summary>
        /// Limpia notificando el callback de expulsión por cada elemento (si existe).
        /// </summary>
        public void ClearAndNotify()
        {
            List<(TKey k, TValue v)> removed;
            lock (_sync)
            {
                removed = _list.Select(n => (n.key, n.value)).ToList();
                _map.Clear();
                _list.Clear();
            }
            foreach (var (k, v) in removed)
                NotifyEvicted(k, v);
        }

        public bool TryRemove(TKey key)
        {
            lock (_sync)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    _list.Remove(node);
                    _map.Remove(key);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Reduce o aumenta la capacidad. Si se reduce, expulsa LRU hasta cumplir el límite.
        /// </summary>
        public void SetCapacity(int newCapacity)
        {
            if (newCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(newCapacity));
            List<(TKey k, TValue v)>? evicted = null;
            lock (_sync)
            {
                _capacity = newCapacity;
                if (_map.Count > _capacity)
                {
                    evicted = new List<(TKey, TValue)>();
                    while (_map.Count > _capacity && _list.Last != null)
                    {
                        var last = _list.Last;
                        _list.RemoveLast();
                        _map.Remove(last.Value.key);
                        evicted.Add((last.Value.key, last.Value.value));
                    }
                }
            }
            if (evicted != null)
            {
                foreach (var (k, v) in evicted)
                    NotifyEvicted(k, v);
            }
        }

        /// <summary>
        /// Elimina el menos reciente si existe y lo devuelve.
        /// </summary>
        public bool TryRemoveLeastRecentlyUsed(out TKey key, out TValue value)
        {
            key = default!;
            value = default!;
            bool removed = false;
            lock (_sync)
            {
                var last = _list.Last;
                if (last != null)
                {
                    key = last.Value.key;
                    value = last.Value.value;
                    _list.RemoveLast();
                    _map.Remove(key);
                    removed = true;
                }
            }
            if (removed) NotifyEvicted(key, value);
            return removed;
        }

        /// <summary>
        /// Intenta actualizar el valor de una clave existente (promueve a más reciente); devuelve false si no existe.
        /// </summary>
        public bool TryUpdate(TKey key, TValue newValue)
        {
            lock (_sync)
            {
                if (_map.TryGetValue(key, out var node))
                {
                    node.Value = (key, newValue);
                    _list.Remove(node);
                    _list.AddFirst(node);
                    return true;
                }
                return false;
            }
        }

        private void NotifyEvicted(TKey key, TValue value)
        {
            Interlocked.Increment(ref _evictionCount);
            try { _onEvicted?.Invoke(key, value); } catch { }
            if (_autoDisposeOnEvict && value is IDisposable d)
            {
                try { d.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// Reinicia los contadores de métricas (hits/misses/evictions).
        /// </summary>
        public void ResetMetrics()
        {
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);
            Interlocked.Exchange(ref _evictionCount, 0);
        }
    }
}
