using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonTunnelRunner
{
    public sealed class PoolConfigTests
    {
        private PoolConfig config;
        private GameObject prefab;
        [SetUp] public void SetUp()
        {
            config = ScriptableObject.CreateInstance<PoolConfig>();
            prefab = new GameObject("ConfigValidationPrefab");
            prefab.SetActive(false);
            prefab.AddComponent<PooledObject>();
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(config);
        }

        [Test] public void EmptyCatalogIsValidAndDoesNotWriteConfiguration()
        {
            string before = JsonUtility.ToJson(config);
            Assert.That(config.TryValidate(out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(config.EntryCount, Is.Zero);
            Assert.That(JsonUtility.ToJson(config), Is.EqualTo(before));
        }

        [TestCase("duplicate", "duplicate key")]
        [TestCase("negativeKey", "invalid key")]
        [TestCase("largeKey", "invalid key")]
        [TestCase("zero", "capacity must be positive")]
        [TestCase("negative", "capacity must be positive")]
        [TestCase("missingPrefab", "assign a prefab")]
        [TestCase("missingComponent", "PooledObject")]
        [TestCase("disabledComponent", "PooledObject")]
        [TestCase("nestedComponent", "PooledObject")]
        public void InvalidEntriesAreRejectedWithoutRepair(string issue, string diagnostic)
        {
            var key = issue == "negativeKey" ? (PoolKey)(-1) : issue == "largeKey" ? (PoolKey)12 : PoolKey.Coin;
            int capacity = issue == "zero" ? 0 : issue == "negative" ? -1 : 2;
            if (issue == "missingComponent") Object.DestroyImmediate(prefab.GetComponent<PooledObject>());
            if (issue == "disabledComponent") prefab.GetComponent<PooledObject>().enabled = false;
            if (issue == "nestedComponent")
            {
                var child = new GameObject("Nested");
                child.transform.SetParent(prefab.transform, false);
                child.AddComponent<PooledObject>();
            }
            var entry = new PoolConfig.Entry(key, issue == "missingPrefab" ? null : prefab, capacity);
            SetEntries(config, issue == "duplicate" ? new[] { entry, entry } : new[] { entry });
            string before = JsonUtility.ToJson(config);
            Assert.That(config.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain(diagnostic));
            Assert.That(JsonUtility.ToJson(config), Is.EqualTo(before));
        }

        [Test] public void AllTwelveTypedKeysCanBeConfiguredIndependently()
        {
            var entries = new PoolConfig.Entry[12];
            for (int i = 0; i < entries.Length; i++) entries[i] = new PoolConfig.Entry((PoolKey)i, prefab, 1);
            SetEntries(config, entries);
            Assert.That(config.TryValidate(out _), Is.True);
            Assert.That(config.EntryCount, Is.EqualTo(12));
            Assert.That(config.GetEntry(11).Key, Is.EqualTo(PoolKey.Firework));
        }

        private static void SetEntries(PoolConfig target, PoolConfig.Entry[] entries) =>
            typeof(PoolConfig).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, entries);
    }
}
