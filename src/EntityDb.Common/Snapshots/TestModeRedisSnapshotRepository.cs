﻿using EntityDb.Abstractions.Snapshots;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace EntityDb.Common.Snapshots
{
    internal sealed class TestModeSnapshotRepository<TEntity> : ISnapshotRepository<TEntity>
    {
        private readonly ISnapshotRepository<TEntity> _snapshotRepository;
        private readonly TestModeSnapshotManager _testModeSnapshotManager;

        public TestModeSnapshotRepository
        (
            ISnapshotRepository<TEntity> snapshotRepository,
            TestModeSnapshotManager testModeSnapshotManager
        )
        {
            _snapshotRepository = snapshotRepository;
            _testModeSnapshotManager = testModeSnapshotManager;
        }

        public Task<bool> PutSnapshot(Guid entityId, TEntity entity)
        {
            _testModeSnapshotManager.AddEntityId(entityId);

            return _snapshotRepository.PutSnapshot(entityId, entity);
        }

        public Task<TEntity?> GetSnapshot(Guid entityId)
        {
            return _snapshotRepository.GetSnapshot(entityId);
        }

        public Task<bool> DeleteSnapshots(Guid[] entityIds)
        {
            _testModeSnapshotManager.RemoveEntityIds(entityIds);

            return _snapshotRepository.DeleteSnapshots(entityIds);
        }

        [ExcludeFromCodeCoverage(Justification = "Proxy for DisposeAsync")]
        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
        }

        public async ValueTask DisposeAsync()
        {
            await _snapshotRepository.DisposeAsync();
        }
    }
}
