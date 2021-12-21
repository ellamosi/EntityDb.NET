﻿using EntityDb.MongoDb.Sessions;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityDb.MongoDb.Queries
{
    internal record DocumentQuery<TDocument>
    (
        IMongoSession MongoSession,
        string CollectionName,
        FilterDefinition<BsonDocument> Filter,
        SortDefinition<BsonDocument>? Sort,
        int? Skip,
        int? Limit
    )
    {
        public Task<List<TDocument>> Execute(ProjectionDefinition<BsonDocument, TDocument> projection)
        {
            var find = MongoSession
                .Find(CollectionName, Filter)
                .Project(projection);

            if (Sort != null)
            {
                find = find.Sort(Sort);
            }

            if (Skip != null)
            {
                find = find.Skip(Skip);
            }

            if (Limit != null)
            {
                find = find.Limit(Limit);
            }

            return find.ToListAsync();
        }
    }
}
