using EntityDb.Abstractions.Entities;
using EntityDb.Abstractions.Snapshots;
using EntityDb.Abstractions.Transactions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EntityDb.Common.Entities;

internal class EntityRepositoryFactory<TEntity> : IEntityRepositoryFactory<TEntity>
    where TEntity : IEntity<TEntity>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITransactionRepositoryFactory _transactionRepositoryFactory;
    private readonly ISnapshotRepositoryFactory<TEntity>? _snapshotRepositoryFactory;

    public EntityRepositoryFactory
    (
        IServiceProvider serviceProvider,
        ITransactionRepositoryFactory transactionRepositoryFactory,
        ISnapshotRepositoryFactory<TEntity>? snapshotRepositoryFactory = null
    )
    {
        _serviceProvider = serviceProvider;
        _transactionRepositoryFactory = transactionRepositoryFactory;
        _snapshotRepositoryFactory = snapshotRepositoryFactory;
    }

    public async Task<IEntityRepository<TEntity>> CreateRepository(string transactionSessionOptionsName,
        string? snapshotSessionOptionsName = null, CancellationToken cancellationToken = default)
    {
        var transactionRepository =
            await _transactionRepositoryFactory.CreateRepository(transactionSessionOptionsName, cancellationToken);

        if (_snapshotRepositoryFactory == null || snapshotSessionOptionsName == null)
        {
            return EntityRepository<TEntity>.Create(_serviceProvider,
                transactionRepository);
        }

        var snapshotRepository = await _snapshotRepositoryFactory.CreateRepository(snapshotSessionOptionsName, cancellationToken);

        return EntityRepository<TEntity>.Create(_serviceProvider,
            transactionRepository, snapshotRepository);
    }
}
