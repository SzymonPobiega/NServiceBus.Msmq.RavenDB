﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_publishing_to_scaled_out_subscribers_on_unicast_transports : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Each_event_should_be_delivered_to_single_instance_of_each_subscriber()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.SubscribersCounter == 4, async (session, c) =>
                {
                    await session.Publish(new MyEvent());
                }))
                .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("1")))
                .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("2")))
                .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("1")))
                .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("2")))
                .Done(c => c.ProcessedByA > 0 && c.ProcessedByB > 0)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c =>
                {
                    Assert.AreEqual(1, c.ProcessedByA);
                    Assert.AreEqual(1, c.ProcessedByB);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            int subscribersCounter;
            int processedByA;
            int processedByB;

            public int SubscribersCounter => subscribersCounter;

            public int ProcessedByA => processedByA;

            public int ProcessedByB => processedByB;

            public void IncrementA()
            {
                Interlocked.Increment(ref processedByA);
            }

            public void IncrementB()
            {
                Interlocked.Increment(ref processedByB);
            }

            public void IncrementSubscribersCounter()
            {
                Interlocked.Increment(ref subscribersCounter);
            }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        context.IncrementSubscribersCounter();
                    });
                });
            }
        }

        public class SubscriberA : EndpointConfigurationBuilder
        {
            public SubscriberA()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var publisher = new EndpointName(Conventions.EndpointNamingConvention(typeof(Publisher)));
                    c.Publishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstance(publisher));
                });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Context.IncrementA();
                    return Task.FromResult(0);
                }
            }
        }
        
        public class SubscriberB : EndpointConfigurationBuilder
        {
            public SubscriberB()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var publisher = new EndpointName(Conventions.EndpointNamingConvention(typeof(Publisher)));
                    c.Publishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstance(publisher));
                });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }
                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Context.IncrementB();
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}
