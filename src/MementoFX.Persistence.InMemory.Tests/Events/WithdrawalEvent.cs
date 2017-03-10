using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.InMemory.Tests.Events
{
    class WithdrawalEvent : DomainEvent
    {
        public Guid CurrentAccountId { get; set; }

        public decimal Amount { get; set; }
    }
}
