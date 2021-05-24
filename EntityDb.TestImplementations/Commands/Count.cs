﻿using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Facts;
using EntityDb.TestImplementations.Entities;
using EntityDb.TestImplementations.Facts;
using System.Collections.Generic;

namespace EntityDb.TestImplementations.Commands
{
    public record Count(int Number) : ICommand<TransactionEntity>
    {
        public IEnumerable<IFact<TransactionEntity>> Execute(TransactionEntity entity)
        {
            yield return new Counted(Number);
        }
    }
}
