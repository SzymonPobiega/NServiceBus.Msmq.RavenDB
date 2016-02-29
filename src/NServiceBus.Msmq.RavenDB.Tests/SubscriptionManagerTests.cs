using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NUnit.Framework;
using Raven.Client.Document;

namespace NServiceBus.Msmq.RavenDB.Tests
{
    [TestFixture]
    public class SubscriptionManagerTests : RavenDBPersistenceTestBase
    {
        [Test]
        public async Task It_inserts_subscription_entries()
        {
            var endpoint = Guid.NewGuid().ToString();
            var transportAddress = Guid.NewGuid().ToString();
            var manager = CreateSubscriptionManager(endpoint, transportAddress);
            var reader = CreateSubscriptionReadeer();
            await manager.Subscribe(typeof(MyEvent), new ContextBag());

            var insertedValues = await reader.GetSubscribersFor(new [] {typeof(MyEvent)});
            var list = insertedValues.ToList();

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(endpoint, list[0].Endpoint);
            Assert.AreEqual(transportAddress, list[0].TransportAddress);
        }

        SubscriptionManager CreateSubscriptionManager(string endpoint, string transportAddress)
        {
            var manager = new SubscriptionManager(endpoint, transportAddress, store);
            return manager;
        }

        SubscriptionReader CreateSubscriptionReadeer()
        {
            var reader = new SubscriptionReader(store);
            return reader;
        }

        [Test]
        public async Task Subscription_insert_is_idempotent()
        {
            var endpoint = Guid.NewGuid().ToString();
            var transportAddress = Guid.NewGuid().ToString();
            var manager = CreateSubscriptionManager(endpoint, transportAddress);
            var reader = CreateSubscriptionReadeer();

            await manager.Subscribe(typeof(MyEvent), new ContextBag());
            await manager.Subscribe(typeof(MyEvent), new ContextBag());

            var insertedValues = await reader.GetSubscribersFor(new[] { typeof(MyEvent) });
            var list = insertedValues.ToList();

            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public async Task It_returns_multiple_event_subscriptions()
        {
            var endpoint = Guid.NewGuid().ToString();
            var transportAddress = Guid.NewGuid().ToString();
            var manager = CreateSubscriptionManager(endpoint, transportAddress);
            var reader = CreateSubscriptionReadeer();

            await manager.Subscribe(typeof(MyOtherEvent), new ContextBag());

            var insertedValues = await reader.GetSubscribersFor(new[] { typeof(MyOtherEvent), typeof(MyPolimorphic) });
            var list = insertedValues.ToList();

            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public async Task It_returns_entries_only_if_subscription_exists()
        {
            var endpoint = Guid.NewGuid().ToString();
            var transportAddress = Guid.NewGuid().ToString();
            var manager = CreateSubscriptionManager(endpoint, transportAddress);
            var reader = CreateSubscriptionReadeer();

            await manager.Subscribe(typeof(MyEvent), new ContextBag());

            var insertedValues = await reader.GetSubscribersFor(new[] { typeof(MyOtherEvent) });
            var list = insertedValues.ToList();

            Assert.AreEqual(0, list.Count);
        }

        public class MyEvent
        {
        }

        public class MyOtherEvent
        {

        }

        public class MyPolimorphic : MyOtherEvent
        {

        }
    }
}
