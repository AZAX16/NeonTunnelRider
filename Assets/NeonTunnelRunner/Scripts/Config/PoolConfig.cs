using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    [CreateAssetMenu(menuName = "Neon Tunnel Runner/Pool Config")]
    public sealed class PoolConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] private PoolKey key;
            [SerializeField] private GameObject prefab;
            [SerializeField, Min(1)] private int capacity;
            public PoolKey Key => key;
            public GameObject Prefab => prefab;
            public int Capacity => capacity;
            public Entry(PoolKey key, GameObject prefab, int capacity)
            {
                this.key = key; this.prefab = prefab; this.capacity = capacity;
            }
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        public int EntryCount => entries == null ? 0 : entries.Length;
        public Entry GetEntry(int index) => entries[index];
        internal const int KeyCount = 12;
        internal static bool IsValidKey(PoolKey key) => (uint)key < KeyCount;

        /// <summary>Empty is valid until a caller requires content. Never repairs authoring data at runtime.</summary>
        public bool TryValidate(out string error)
        {
            int seen = 0;
            for (int i = 0; i < EntryCount; i++)
            {
                Entry entry = entries[i];
                string issue = null;
                if (!IsValidKey(entry.Key)) issue = "invalid key";
                else if ((seen & (1 << (int)entry.Key)) != 0) issue = "duplicate key";
                else if (entry.Capacity <= 0) issue = "capacity must be positive";
                else if (!entry.Prefab) issue = "assign a prefab";
                else
                {
                    var component = entry.Prefab.GetComponent<PooledObject>();
                    if (!component || !component.enabled || entry.Prefab.GetComponentsInChildren<PooledObject>(true).Length != 1)
                        issue = "prefab requires exactly one enabled PooledObject (or subclass) on its root";
                }
                if (issue != null)
                {
                    error = $"Pools entry {i} ({entry.Key}): {issue}. Fix PoolConfig before StartRun.";
                    return false;
                }
                seen |= 1 << (int)entry.Key;
            }
            error = null;
            return true;
        }
    }
}
