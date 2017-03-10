using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.InMemory.Tests.Events
{
    public class MoneyTransferredEvent : DomainEvent
    {
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
