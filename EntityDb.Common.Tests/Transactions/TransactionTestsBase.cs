﻿using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Facts;
using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Queries.FilterBuilders;
using EntityDb.Abstractions.Queries.SortBuilders;
using EntityDb.Abstractions.Tags;
using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Extensions;
using EntityDb.Common.Queries;
using EntityDb.Common.Tags;
using EntityDb.Common.Transactions;
using EntityDb.TestImplementations.Commands;
using EntityDb.TestImplementations.Entities;
using EntityDb.TestImplementations.Facts;
using EntityDb.TestImplementations.Queries;
using EntityDb.TestImplementations.Source;
using EntityDb.TestImplementations.Tags;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace EntityDb.Common.Tests.Transactions
{
    public abstract class TransactionTestsBase
    {
        private class ExpectedObjects
        {
            public readonly List<Guid> TrueTransactionIds = new();
            public readonly List<Guid> FalseTransactionIds = new();

            public readonly List<Guid> TrueEntityIds = new();
            public readonly List<Guid> FalseEntityIds = new();

            public readonly List<object> TrueSources = new();
            public readonly List<object> FalseSources = new();

            public readonly List<ICommand<TransactionEntity>> TrueCommands = new();
            public readonly List<ICommand<TransactionEntity>> FalseCommands = new();

            public readonly List<IFact<TransactionEntity>> TrueFacts = new();
            public readonly List<IFact<TransactionEntity>> FalseFacts = new();

            public readonly List<ITag> TrueTags = new();
            public readonly List<ITag> FalseTags = new();

            public void Add(bool condition, Guid transactionId, Guid entityId, object source, ICommand<TransactionEntity>[] commands, IFact<TransactionEntity>[] facts, ITag[] tags)
            {
                if (condition)
                {
                    TrueTransactionIds.Add(transactionId);
                    TrueEntityIds.Add(entityId);
                    TrueSources.Add(source);
                    TrueCommands.AddRange(commands);
                    TrueFacts.AddRange(facts);
                    TrueTags.AddRange(tags);
                }
                else
                {
                    FalseTransactionIds.Add(transactionId);
                    FalseEntityIds.Add(entityId);
                    FalseSources.Add(source);
                    FalseCommands.AddRange(commands);
                    FalseFacts.AddRange(facts);
                    FalseTags.AddRange(tags);
                }
            }
        }

        private readonly IServiceProvider _serviceProvider;

        protected TransactionTestsBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private Task<ITransactionRepository<TransactionEntity>> CreateRepository(bool readOnly = false, bool tolerateLag = false)
        {
            return _serviceProvider.CreateTransactionRepository<TransactionEntity>(new TransactionSessionOptions
            {
                ReadOnly = readOnly,
                SecondaryPreferred = tolerateLag,
            });
        }

        private async Task TestGet<TResult>
        (
            List<ITransaction<TransactionEntity>> transactions,
            Func<bool, TResult[]> getExpectedResults,
            Func<ITransactionRepository<TransactionEntity>, bool, bool, int?, int?, Task<TResult[]>> getActualResults
        )
        {
            // ARRANGE

            var expectedTrueResults = getExpectedResults.Invoke(false);
            var expectedFalseResults = getExpectedResults.Invoke(true);
            var reversedExpectedTrueResults = expectedTrueResults.Reverse().ToArray();
            var reversedExpectedFalseResults = expectedFalseResults.Reverse().ToArray();
            var expectedSkipTakeResults = expectedTrueResults.Skip(1).Take(1);

            await using var transactionRepository = await CreateRepository();

            foreach (var transaction in transactions)
            {
                var transactionInserted = await transactionRepository.PutTransaction(transaction);

                Assert.True(transactionInserted);
            }

            // ACT

            var actualTrueResults = await getActualResults.Invoke(transactionRepository, false, false, null, null);
            var actualFalseResults = await getActualResults.Invoke(transactionRepository, true, false, null, null);
            var reversedActualTrueResults = await getActualResults.Invoke(transactionRepository, false, true, null, null);
            var reversedActualFalseResults = await getActualResults.Invoke(transactionRepository, true, true, null, null);
            var actualSkipTakeResults = await getActualResults.Invoke(transactionRepository, false, false, 1, 1);

            // ASSERT

            Assert.True(expectedTrueResults.SequenceEqual(actualTrueResults));
            Assert.True(expectedFalseResults.SequenceEqual(actualFalseResults));
            Assert.True(reversedExpectedTrueResults.SequenceEqual(reversedActualTrueResults));
            Assert.True(reversedExpectedFalseResults.SequenceEqual(reversedActualFalseResults));
            Assert.True(expectedSkipTakeResults.SequenceEqual(actualSkipTakeResults));
        }

        private Task TestGetTransactionIds(ISourceQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ISourceQuery, ISourceQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTransactionIds : expectedObjects.TrueTransactionIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTransactionIds(modifiedQuery);
                }
            );
        }

        private Task TestGetTransactionIds(ICommandQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ICommandQuery, ICommandQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTransactionIds : expectedObjects.TrueTransactionIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTransactionIds(modifiedQuery);
                }
            );
        }

        private Task TestGetTransactionIds(IFactQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<IFactQuery, IFactQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTransactionIds : expectedObjects.TrueTransactionIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTransactionIds(modifiedQuery);
                }
            );
        }

        private Task TestGetTransactionIds(ITagQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ITagQuery, ITagQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTransactionIds : expectedObjects.TrueTransactionIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetTransactionIds(modifiedQuery);
                }
            );
        }

        private Task TestGetEntityIds(ISourceQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ISourceQuery, ISourceQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseEntityIds : expectedObjects.TrueEntityIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetEntityIds(modifiedQuery);
                }
            );
        }

        private Task TestGetEntityIds(ICommandQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ICommandQuery, ICommandQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseEntityIds : expectedObjects.TrueEntityIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetEntityIds(modifiedQuery);
                }
            );
        }

        private Task TestGetEntityIds(IFactQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<IFactQuery, IFactQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseEntityIds : expectedObjects.TrueEntityIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetEntityIds(modifiedQuery);
                }
            );
        }

        private Task TestGetEntityIds(ITagQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ITagQuery, ITagQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseEntityIds : expectedObjects.TrueEntityIds).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetEntityIds(modifiedQuery);
                }
            );
        }

        private Task TestGetSources(ISourceQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ISourceQuery, ISourceQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseSources : expectedObjects.TrueSources).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetSources(modifiedQuery);
                }
            );
        }

        private Task TestGetCommands(ICommandQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ICommandQuery, ICommandQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseCommands : expectedObjects.TrueCommands).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetCommands(modifiedQuery);
                }
            );
        }

        private Task TestGetFacts(IFactQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<IFactQuery, IFactQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseFacts : expectedObjects.TrueFacts).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedQuery = filter.Invoke(modifiedQuery);
                    }

                    return transactionRepository.GetFacts(modifiedQuery);
                }
            );
        }

        private Task TestGetTags(ITagQuery query, List<ITransaction<TransactionEntity>> transactions, ExpectedObjects expectedObjects, Func<ITagQuery, ITagQuery>? filter = null)
        {
            return TestGet
            (
                transactions,
                (invert) => (invert ? expectedObjects.FalseTags : expectedObjects.TrueTags).ToArray(),
                (transactionRepository, invertFilter, reverseSort, skip, take) =>
                {
                    var modifiedTagQuery = query.Modify(invertFilter: invertFilter, reverseSort: reverseSort, replaceSkip: skip, replaceTake: take);

                    if (filter != null)
                    {
                        modifiedTagQuery = filter.Invoke(modifiedTagQuery);
                    }

                    return transactionRepository.GetTags(modifiedTagQuery);
                }
            );
        }

        private ITransaction<TransactionEntity> BuildTransaction(Guid transactionId, Guid entityId, object source, ICommand<TransactionEntity>[] commands, DateTime? timeStampOverride = null)
        {
            var transactionBuilder = new TransactionBuilder<TransactionEntity>(new ClaimsPrincipal(), _serviceProvider);

            transactionBuilder.Create(entityId, commands[0]);

            for (var i = 1; i < commands.Length; i++)
            {
                transactionBuilder.Append(entityId, commands[i]);
            }

            return transactionBuilder.Build(transactionId, source, timeStampOverride);
        }

        private Guid[] GetSortedGuids(int numberOfGuids)
        {
            return Enumerable
                .Range(1, numberOfGuids)
                .Select(_ => Guid.NewGuid())
                .OrderBy(guid => guid)
                .ToArray();
        }

        [Fact]
        public async Task GivenNonUniqueTransactionIds_WhenPuttingTransactions_ThenSecondPutReturnsFalse()
        {
            // ARRANGE

            var transactionId = Guid.NewGuid();

            static ITransaction<TransactionEntity> NewTransaction(Guid transactionId)
            {
                return new Transaction<TransactionEntity>
                (
                    transactionId,
                    DateTime.UtcNow,
                    new NoSource(),
                    new[]
                    {
                        new TransactionCommand<TransactionEntity>
                        (
                            Guid.NewGuid(),
                            0,
                            new DoNothing(),
                            Array.Empty<TransactionFact<TransactionEntity>>(),
                            Array.Empty<ITag>(),
                            Array.Empty<ITag>()
                        ),
                    }
                );
            }

            await using var transactionRepository = await CreateRepository();

            // ACT

            var firstTransactionInserted = await transactionRepository.PutTransaction(NewTransaction(transactionId));
            var secondTransactionInserted = await transactionRepository.PutTransaction(NewTransaction(transactionId));

            // ASSERT

            Assert.True(firstTransactionInserted);
            Assert.False(secondTransactionInserted);
        }

        [Fact]
        public async Task GivenNonUniqueVersionNumbers_WhenInsertingCommands_ThenReturnFalse()
        {
            // ARRANGE

            var entityId = Guid.NewGuid();
            ulong previousVersionNumber = 0;

            var transaction = new Transaction<TransactionEntity>
            (
                Guid.NewGuid(),
                DateTime.UtcNow,
                new NoSource(),
                new[]
                {
                    new TransactionCommand<TransactionEntity>
                    (
                        entityId,
                        previousVersionNumber,
                        new DoNothing(),
                        Array.Empty<TransactionFact<TransactionEntity>>(),
                        Array.Empty<ITag>(),
                        Array.Empty<ITag>()
                    ),
                    new TransactionCommand<TransactionEntity>
                    (
                        entityId,
                        previousVersionNumber,
                        new DoNothing(),
                        Array.Empty<TransactionFact<TransactionEntity>>(),
                        Array.Empty<ITag>(),
                        Array.Empty<ITag>()
                    ),
                }
            );

            await using var transactionRepository = await CreateRepository();

            // ACT

            var transactionInserted = await transactionRepository.PutTransaction(transaction);

            // ASSERT

            Assert.False(transactionInserted);
        }

        [Fact]
        public async Task GivenNonUniqueSubversionNumbers_WhenInsertingFacts_ThenReturnFalse()
        {
            // ARRANGE

            var entityId = Guid.NewGuid();
            ulong subversionNumber = 0;

            var transaction = new Transaction<TransactionEntity>
            (
                Guid.NewGuid(),
                DateTime.UtcNow,
                new NoSource(),
                new[]
                {
                    new TransactionCommand<TransactionEntity>
                    (
                        entityId,
                        0,
                        new DoNothing(),
                        new[]
                        {
                            new TransactionFact<TransactionEntity>
                            (
                                subversionNumber,
                                new NothingDone()
                            ),
                            new TransactionFact<TransactionEntity>
                            (
                                subversionNumber,
                                new NothingDone()
                            )
                        },
                        Array.Empty<ITag>(),
                        Array.Empty<ITag>()
                    )
                }
            );

            await using var transactionRepository = await CreateRepository();

            // ACT

            var transactionInserted = await transactionRepository.PutTransaction(transaction);

            // ASSERT

            Assert.False(transactionInserted);
        }

        [Fact]
        public async Task GivenNonUniqueTags_WhenInsertingTagDocuments_ThenReturnFalse()
        {
            // ARRANGE

            var tag = new Tag("Foo", "Bar", "Baz");

            var transaction = new Transaction<TransactionEntity>
            (
                Guid.NewGuid(),
                DateTime.UtcNow,
                new NoSource(),
                new[]
                {
                    new TransactionCommand<TransactionEntity>
                    (
                        Guid.NewGuid(),
                        0,
                        new DoNothing(),
                        Array.Empty<TransactionFact<TransactionEntity>>(),
                        Array.Empty<ITag>(),
                        new[]
                        {
                            tag,
                        }
                    ),
                    new TransactionCommand<TransactionEntity>
                    (
                        Guid.NewGuid(),
                        0,
                        new DoNothing(),
                        Array.Empty<TransactionFact<TransactionEntity>>(),
                        Array.Empty<ITag>(),
                        new[]
                        {
                            tag,
                        }
                    ),
                }
            );

            await using var transactionRepository = await CreateRepository();

            // ACT

            var transactionInserted = await transactionRepository.PutTransaction(transaction);

            // ASSERT

            Assert.False(transactionInserted);
        }

        [Fact]
        public async Task GivenEntityInserted_WhenGettingEntity_ThenReturnEntity()
        {
            // ARRANGE

            var expectedEntity = new TransactionEntity
            {
                VersionNumber = 1,
            };

            var entityId = Guid.NewGuid();

            await using var transactionRepository = await CreateRepository();

            var transaction = BuildTransaction(Guid.NewGuid(), entityId, new NoSource(), new[] { new DoNothing() });

            await transactionRepository.PutTransaction(transaction);

            // ACT

            var actualEntity = await _serviceProvider.GetEntity(entityId, transactionRepository);

            // ASSERT

            Assert.Equal(expectedEntity, actualEntity);
        }

        [Fact]
        public async Task GivenEntityInsertedWithTags_WhenRemovingAllTags_ThenFinalEntityHasNoTags()
        {
            // ARRANGE

            var transactionBuilder = new TransactionBuilder<TransactionEntity>(new ClaimsPrincipal(), _serviceProvider);

            var expectedInitialTags = new[]
            {
                new Tag("Foo", "Bar", "Baz"),
            };

            var entityId = Guid.NewGuid();

            await using var transactionRepository = await CreateRepository();

            transactionBuilder.Create(entityId, new AddTag("Foo", "Bar", "Baz"));

            var initialTransaction = transactionBuilder.Build(Guid.NewGuid(), new NoSource());

            await transactionRepository.PutTransaction(initialTransaction);

            var tagQueryMock = new Mock<ITagQuery>(MockBehavior.Strict);

            var tagQuery = new DeleteTagsQuery(entityId, expectedInitialTags);

            // ACT

            var actualInitialTags = await transactionRepository.GetTags(tagQuery);

            transactionBuilder.Append(entityId, new RemoveAllTags());

            var finalTransaction = transactionBuilder.Build(Guid.NewGuid(), new NoSource());

            await transactionRepository.PutTransaction(finalTransaction);

            var actualFinalTags = await transactionRepository.GetTags(tagQuery);

            // ASSERT

            Assert.Equal(expectedInitialTags, actualInitialTags);
            Assert.Empty(actualFinalTags);
        }

        [Theory]
        [InlineData(60, 20, 30)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByTransactionTimeStamp_ThenReturnExpectedObjects(int timeSpanInMinutes, int gteInMinutes, int lteInMinutes)
        {
            var originTimeStamp = DateTime.UnixEpoch;

            var transactions = new List<ITransaction<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            var transactionIds = GetSortedGuids(timeSpanInMinutes);
            var entityIds = GetSortedGuids(timeSpanInMinutes);

            DateTime? gte = null;
            DateTime? lte = null;

            for (var i = 1; i <= timeSpanInMinutes; i++)
            {
                var currentTransactionId = transactionIds[i - 1];
                var currentEntityId = entityIds[i - 1];

                var currentTimeStamp = originTimeStamp.AddMinutes(i);

                var source = new Counter(i);

                var commands = new ICommand<TransactionEntity>[]
                {
                    new Count(i),
                };

                var facts = new IFact<TransactionEntity>[]
                {
                    new Counted(i),
                    _serviceProvider.GetVersionNumberFact<TransactionEntity>(1),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                expectedObjects.Add(gteInMinutes <= i && i <= lteInMinutes, currentTransactionId, currentEntityId, source, commands, facts, tags);

                if (i == lteInMinutes)
                {
                    lte = currentTimeStamp;
                }
                else if (i == gteInMinutes)
                {
                    gte = currentTimeStamp;
                }

                var transaction = BuildTransaction(currentTransactionId, currentEntityId, source, commands, currentTimeStamp);

                transactions.Add(transaction);
            }

            Assert.NotNull(gte);
            Assert.NotNull(lte);

            var query = new TransactionTimeStampQuery(gte!.Value, lte!.Value);

            await TestGetTransactionIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ITagQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ITagQuery, transactions, expectedObjects);
            await TestGetSources(query, transactions, expectedObjects);
            await TestGetCommands(query, transactions, expectedObjects);
            await TestGetFacts(query, transactions, expectedObjects);
            await TestGetTags(query, transactions, expectedObjects);
        }

        [Theory]
        [InlineData(10, 5)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByTransactionId_ThenReturnExpectedObjects(int numberOfTransactionIds, int whichTransactionId)
        {
            var transactions = new List<ITransaction<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            Guid? transactionId = null;

            var transactionIds = GetSortedGuids(numberOfTransactionIds);
            var entityIds = GetSortedGuids(numberOfTransactionIds);

            for (var i = 1; i <= numberOfTransactionIds; i++)
            {
                var currentTransactionId = transactionIds[i - 1];
                var currentEntityId = entityIds[i - 1];

                var source = new Counter(i);

                var commands = new ICommand<TransactionEntity>[]
                {
                    new Count(i),
                };

                var facts = new IFact<TransactionEntity>[]
                {
                    new Counted(i),
                    _serviceProvider.GetVersionNumberFact<TransactionEntity>(1),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                expectedObjects.Add(i == whichTransactionId, currentTransactionId, currentEntityId, source, commands, facts, tags);

                if (i == whichTransactionId)
                {
                    transactionId = currentTransactionId;
                }

                var transaction = BuildTransaction(currentTransactionId, currentEntityId, source, commands);

                transactions.Add(transaction);
            }

            Assert.NotNull(transactionId);

            var query = new TransactionIdQuery(transactionId!.Value);

            await TestGetTransactionIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ITagQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ITagQuery, transactions, expectedObjects);
            await TestGetSources(query, transactions, expectedObjects);
            await TestGetCommands(query, transactions, expectedObjects);
            await TestGetFacts(query, transactions, expectedObjects);
            await TestGetTags(query, transactions, expectedObjects);
        }

        [Theory]
        [InlineData(10, 5)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByEntityId_ThenReturnExpectedObjects(int numberOfEntityIds, int whichEntityId)
        {
            var transactions = new List<ITransaction<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            Guid? entityId = null;

            var transactionIds = GetSortedGuids(numberOfEntityIds);
            var entityIds = GetSortedGuids(numberOfEntityIds);

            for (var i = 1; i <= numberOfEntityIds; i++)
            {
                var currentTransactionId = transactionIds[i - 1];
                var currentEntityId = entityIds[i - 1];

                var source = new Counter(i);

                var commands = new ICommand<TransactionEntity>[]
                {
                    new Count(i),
                };

                var facts = new IFact<TransactionEntity>[]
                {
                    new Counted(i),
                    _serviceProvider.GetVersionNumberFact<TransactionEntity>(1),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                expectedObjects.Add(i == whichEntityId, currentTransactionId, currentEntityId, source, commands, facts, tags);

                if (i == whichEntityId)
                {
                    entityId = currentEntityId;
                }

                var transaction = BuildTransaction(currentTransactionId, currentEntityId, source, commands);

                transactions.Add(transaction);
            }

            Assert.NotNull(entityId);

            var query = new EntityIdQuery(entityId!.Value);

            await TestGetTransactionIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetTransactionIds(query as ITagQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ISourceQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ICommandQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as IFactQuery, transactions, expectedObjects);
            await TestGetEntityIds(query as ITagQuery, transactions, expectedObjects);
            await TestGetSources(query, transactions, expectedObjects);
            await TestGetCommands(query, transactions, expectedObjects);
            await TestGetFacts(query, transactions, expectedObjects);
            await TestGetTags(query, transactions, expectedObjects);
        }

        [Theory]
        [InlineData(20, 5, 15)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByEntityVersionNumber_ThenReturnExpectedObjects(int numberOfVersionNumbers, int gteAsInt, int lteAsInt)
        {
            var commands = new List<ICommand<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            for (var i = 1; i <= numberOfVersionNumbers; i++)
            {
                var command = new Count(i);

                var facts = new[]
                {
                    new Counted(i),
                    _serviceProvider.GetVersionNumberFact<TransactionEntity>((ulong)i),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                commands.Add(command);

                expectedObjects.Add(gteAsInt <= i && i <= lteAsInt, default, default, default!, new[] { command }, facts, tags);
            }

            var transaction = BuildTransaction(Guid.NewGuid(), Guid.NewGuid(), new NoSource(), commands.ToArray());

            var transactions = new List<ITransaction<TransactionEntity>> { transaction };

            var query = new EntityVersionNumberQuery((ulong)gteAsInt, (ulong)lteAsInt);

            await TestGetCommands(query, transactions, expectedObjects);
            await TestGetFacts(query, transactions, expectedObjects);
            await TestGetTags(query, transactions, expectedObjects);
        }

        [Theory]
        [InlineData(20, 5, 15)]
        public async Task GivenTransactionAlreadyInserted_WhenQueryingByData_ThenReturnExpectedObjects(int countTo, int gte, int lte)
        {
            var transactions = new List<ITransaction<TransactionEntity>>();
            var expectedObjects = new ExpectedObjects();

            var transactionIds = GetSortedGuids(countTo);
            var entityIds = GetSortedGuids(countTo);

            for (var i = 1; i <= countTo; i++)
            {
                var currentTransactionId = transactionIds[i - 1];
                var currentEntityId = entityIds[i - 1];

                var source = new Counter(i);

                var commands = new ICommand<TransactionEntity>[]
                {
                    new Count(i),
                };

                var facts = new IFact<TransactionEntity>[]
                {
                    new Counted(i),
                };

                var tags = new[]
                {
                    new CountTag(i),
                };

                expectedObjects.Add(gte <= i && i <= lte, currentTransactionId, currentEntityId, source, commands, facts, tags);

                var transaction = BuildTransaction(currentTransactionId, currentEntityId, source, commands);

                transactions.Add(transaction);
            }

            ISourceQuery FilterSources(ISourceQuery sourceQuery)
            {
                return sourceQuery.Filter(new CountFilter());
            }

            ICommandQuery FilterCommands(ICommandQuery commandQuery)
            {
                return commandQuery.Filter(new CountFilter());
            }

            IFactQuery FilterFacts(IFactQuery factQuery)
            {
                return factQuery.Filter(new CountFilter());
            }

            ITagQuery FilterTags(ITagQuery tagQuery)
            {
                return tagQuery.Filter(new CountFilter());
            }

            var query = new CountQuery<TransactionEntity>(gte, lte);

            await TestGetTransactionIds(query, transactions, expectedObjects, FilterSources);
            await TestGetTransactionIds(query, transactions, expectedObjects, FilterCommands);
            await TestGetTransactionIds(query, transactions, expectedObjects, FilterFacts);
            await TestGetTransactionIds(query, transactions, expectedObjects, FilterTags);
            await TestGetEntityIds(query, transactions, expectedObjects, FilterSources);
            await TestGetEntityIds(query, transactions, expectedObjects, FilterCommands);
            await TestGetEntityIds(query, transactions, expectedObjects, FilterFacts);
            await TestGetEntityIds(query, transactions, expectedObjects, FilterTags);
            await TestGetSources(query, transactions, expectedObjects, FilterSources);
            await TestGetCommands(query, transactions, expectedObjects, FilterCommands);
            await TestGetFacts(query, transactions, expectedObjects, FilterFacts);
            await TestGetTags(query, transactions, expectedObjects, FilterTags);
        }
    }
}
