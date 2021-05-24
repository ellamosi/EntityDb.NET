﻿using EntityDb.Common.Transactions;
using System;

namespace EntityDb.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an actor passes an entity id to <see cref="TransactionBuilder{TEntity}.Load(Guid, Abstractions.Transactions.ITransactionRepository{TEntity}, Abstractions.Snapshots.ISnapshotRepository{TEntity}?)"/> with an entity id that loads with a version number of zero.
    /// </summary>
    public sealed class EntityNotCreatedException : Exception
    {
    }
}
