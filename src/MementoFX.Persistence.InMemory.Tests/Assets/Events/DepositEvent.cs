using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.InMemory.Tests.Assets.Events
{
    class DepositEvent : DomainEvent
    {
        public Guid CurrentAccountId { get; set; }

        public decimal Amount { get; set; }
    }
}
