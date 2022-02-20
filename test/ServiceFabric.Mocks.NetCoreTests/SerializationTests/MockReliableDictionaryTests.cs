﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using ServiceFabric.Mocks.ReliableCollections;
using System.Threading;

namespace ServiceFabric.Mocks.NetCoreTests.SerializationTests
{
    [TestClass]
    public class MockReliableDictionaryTests
    {
        const string originalContentValue = "original value";
        const string modifiedContentValue = "modified value";

        /// <summary>
        /// The value is stored as serialized object, so changes made after storing it are not visible in the queried result.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DictionarySerializedValueChangesIgnoredTest()
        {
            const string key = "key";

            var value = new ModifyablePayload
            { 
                Content = originalContentValue
            };

            System.Collections.Concurrent.ConcurrentDictionary<Type, object> serializers = new();
            serializers.TryAdd(typeof(ModifyablePayload), new ModifyablePayloadSerializer());
            var dictionary = new MockReliableDictionary<string, ModifyablePayload>(new Uri("fabric://MockReliableDictionary"), serializers);
            var tx = new MockTransaction(null, 1);

            await dictionary.AddAsync(tx, key, value);

            //modify in-memory state
            value.Content = modifiedContentValue;

            var actual = await dictionary.TryGetValueAsync(tx, key);

            //original content remains the same
            Assert.AreEqual(originalContentValue, actual.Value.Content);
            Assert.AreNotSame(value, actual);
        }

        /// <summary>
        /// The value is stored as serialized object, so a different value instance is returned when querying.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DictionarySerializedValueEqualWhenRetrievedTest()
        {
            const string key = "key";

            var value = new ModifyablePayload
            {
                Content = originalContentValue
            };

            System.Collections.Concurrent.ConcurrentDictionary<Type, object> serializers = new();
            serializers.TryAdd(typeof(ModifyablePayload), new ModifyablePayloadSerializer());
            var dictionary = new MockReliableDictionary<string, ModifyablePayload>(new Uri("fabric://MockReliableDictionary"), serializers);
            var tx = new MockTransaction(null, 1);

            await dictionary.AddAsync(tx, key, value);

            var actual = await dictionary.TryGetValueAsync(tx, key);

            Assert.AreEqual(originalContentValue, actual.Value.Content);
            //original value remains the same, but different instance
            Assert.AreEqual(value, actual.Value);
            Assert.AreNotSame(value, actual);
        }

        /// <summary>
        /// Keys are stored as serialized objects, so an equal but different key instance is returned when iterating keys.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DictionaryCreateKeyEnumerableAsyncTest()
        {
            var key = new ModifyablePayload { Content = originalContentValue };
            const string value = "value";

            System.Collections.Concurrent.ConcurrentDictionary<Type, object> serializers = new();
            serializers.TryAdd(typeof(ModifyablePayload), new ModifyablePayloadSerializer());
            var dictionary = new MockReliableDictionary<ModifyablePayload, string>(new Uri("fabric://MockReliableDictionary"), serializers);
            var tx = new MockTransaction(null, 1);

            await dictionary.AddAsync(tx, key, value);
            var enumerable = await dictionary.CreateKeyEnumerableAsync(tx);
            var enumerator = enumerable.GetAsyncEnumerator();
            await enumerator.MoveNextAsync(CancellationToken.None);
            var actual = enumerator.Current;

            Assert.AreEqual(key, actual); //equal value
            Assert.AreNotSame(key, actual); //different instance
        }
    }
}