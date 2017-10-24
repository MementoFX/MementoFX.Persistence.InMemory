using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SharpTestsEx;
using MementoFX.Domain;
using MementoFX.Messaging;
using MementoFX.Persistence.InMemory.Tests.Assets.Events;

namespace MementoFX.Persistence.InMemory.Tests
{
    
    public class InMemoryEventStoreFixture
    {
        IEventStore EventStore = null;

        public InMemoryEventStoreFixture()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            EventStore = new InMemoryEventStore(bus);
        }

        [Fact]
        public void Ctor_should_throw_ArgumentNullException_on_null_documentStore_and_value_of_parameter_should_be_documentStore()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            Executing.This(() => new InMemoryEventStore(null))
                           .Should()
                           .Throw<ArgumentNullException>()
                           .And
                           .ValueOf
                           .ParamName
                           .Should()
                           .Be
                           .EqualTo("eventDispatcher");
        }

        [Fact]
        public void Save_should_throw_ArgumentNullException_on_null_parameter()
        {
            var eventDispatcherMockBuilder = new Mock<IEventDispatcher>();

            var sut = new InMemoryEventStore(eventDispatcherMockBuilder.Object);
            Executing.This(() => sut.Save(null))
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ValueOf
                .ParamName
                .Should()
                .Be
                .EqualTo("event");
        }

        [Fact]
        public void Saving_an_event_should_allow_for_later_retrieval()
        {
            var @event = new WithdrawalEvent() { CurrentAccountId = Guid.NewGuid(), Amount = 101 };
            EventStore.Save(@event);
            var events = EventStore.Find<WithdrawalEvent>(e => e.CurrentAccountId == @event.CurrentAccountId);
            Assert.Single(events);
            Assert.Equal(@event.CurrentAccountId, events.First().CurrentAccountId);
            Assert.Equal(@event.Amount, events.First().Amount);
        }

        [Fact]
        public void Saving_two_events_should_allow_for_later_retrieval_of_one_of_them()
        {
            var withdrawalEvent = new WithdrawalEvent() { CurrentAccountId = Guid.NewGuid(), Amount = 101 };
            EventStore.Save(withdrawalEvent);
            var depositEvent = new DepositEvent() { CurrentAccountId = Guid.NewGuid(), Amount = 42 };
            EventStore.Save(depositEvent);

            var events = EventStore.Find<WithdrawalEvent>(e => e.CurrentAccountId == withdrawalEvent.CurrentAccountId);
            Assert.Single(events);
            Assert.Equal(withdrawalEvent.CurrentAccountId, events.First().CurrentAccountId);
            Assert.Equal(withdrawalEvent.Amount, events.First().Amount);
        }

        [Fact]
        public void Saving_an_event_within_a_timeline_should_allow_for_later_retrieval()
        {
            var @event = new WithdrawalEvent() { CurrentAccountId = Guid.NewGuid(), TimelineId = Guid.NewGuid(), Amount = 101 };
            EventStore.Save(@event);
            var events = EventStore.Find<WithdrawalEvent>(e => e.TimelineId == @event.TimelineId && e.CurrentAccountId == @event.CurrentAccountId);
            Assert.Single(events);
            Assert.Equal(@event.CurrentAccountId, events.First().CurrentAccountId);
            Assert.Equal(@event.Amount, events.First().Amount);
        }

        [Fact]
        public void Saving_an_event_should_dispatch_it_as_well()
        {
            DomainEvent @event = new WithdrawalEvent() { CurrentAccountId = Guid.NewGuid(), Amount = 101 };
            var eventDispatcherMockBuilder = new Mock<IEventDispatcher>();
            eventDispatcherMockBuilder.Setup(v => v.Dispatch(@event));

            var sut = new InMemoryEventStore(eventDispatcherMockBuilder.Object);
            sut.Save(@event);
            eventDispatcherMockBuilder.Verify(v => v.Dispatch(@event), Times.Once());
        }

        [Fact]
        public void Event_retrieval_should_work_properly()
        {
            var eventDispatcherMockBuilder = new Mock<IEventDispatcher>();
            var sut = new InMemoryEventStore(eventDispatcherMockBuilder.Object);

            var sourceAccountId = Guid.NewGuid();
            var destinationAccountId = Guid.NewGuid();

            var withdrawalEvent = new WithdrawalEvent() { CurrentAccountId = sourceAccountId, Amount = 101 };
            sut.Save(withdrawalEvent);

            var depositEvent = new DepositEvent() { CurrentAccountId = sourceAccountId, Amount = 42 };
            sut.Save(depositEvent);

            var moneyTransferredEvent = new MoneyTransferredEvent() { SourceAccountId = sourceAccountId, DestinationAccountId = destinationAccountId, Amount = 42 };
            sut.Save(moneyTransferredEvent);

            var eventDescriptors = new List<EventMapping>()
            {
                new EventMapping
                {
                   AggregateIdPropertyName = nameof(WithdrawalEvent.CurrentAccountId),
                   EventType = typeof(WithdrawalEvent)
                },
                new EventMapping
                {
                   AggregateIdPropertyName = nameof(MoneyTransferredEvent.SourceAccountId),
                   EventType = typeof(MoneyTransferredEvent)
                }
            };

            var events = sut.RetrieveEvents(sourceAccountId, DateTime.Now, eventDescriptors, null);
            Assert.Equal(2, events.Count());
        }

        [Fact]
        public void Event_retrieval_should_discriminate_between_timelines()
        {
            var eventDispatcherMockBuilder = new Mock<IEventDispatcher>();
            var sut = new InMemoryEventStore(eventDispatcherMockBuilder.Object);

            var sourceAccountId = Guid.NewGuid();
            var destinationAccountId = Guid.NewGuid();

            var withdrawalEvent = new WithdrawalEvent() { CurrentAccountId = sourceAccountId, Amount = 101 };
            sut.Save(withdrawalEvent);

            var depositEvent = new DepositEvent() { CurrentAccountId = sourceAccountId, Amount = 42 };
            sut.Save(depositEvent);

            var moneyTransferredEvent = new MoneyTransferredEvent() { SourceAccountId = sourceAccountId, DestinationAccountId = destinationAccountId, Amount = 42, TimelineId = Guid.NewGuid() };
            sut.Save(moneyTransferredEvent);

            var eventDescriptors = new List<EventMapping>()
            {
                new EventMapping
                {
                   AggregateIdPropertyName = nameof(WithdrawalEvent.CurrentAccountId),
                   EventType = typeof(WithdrawalEvent)
                },
                new EventMapping
                {
                   AggregateIdPropertyName = nameof(MoneyTransferredEvent.SourceAccountId),
                   EventType = typeof(MoneyTransferredEvent)
                }
            };

            var events = sut.RetrieveEvents(sourceAccountId, DateTime.Now, eventDescriptors, null);
            Assert.Single(events);
        }
    }
}
