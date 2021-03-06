﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoQueue.Core.AgentAbstractions;
using MongoQueue.Core.Entities;

namespace MongoQueue.Legacy
{
    public class LegacyMessageStatusManager : IMessageStatusManager
    {
        private readonly LegacyMongoAgent _mongoAgent;

        public LegacyMessageStatusManager(LegacyMongoAgent mongoAgent)
        {
            _mongoAgent = mongoAgent;
        }

        public async Task<bool> TrySetProcessingStartedAt(string route, string messageId, CancellationToken cancellationToken)
        {
            var collection = _mongoAgent.GetEnvelops(route);
            var query = Query.And(Query<Envelope>.EQ(x => x.Id, messageId), Query<Envelope>.EQ(x => x.IsProcessed, false));
            var update = Update<Envelope>.Set(x => x.ProcessingStartedAt, DateTime.UtcNow).Set(x => x.IsProcessingStarted, true);
            var result = collection.FindAndModify(new FindAndModifyArgs
            {
                Query = query,
                Update = update,
                VersionReturned = FindAndModifyDocumentVersion.Modified,
                Upsert = false,
            });
            return result.ModifiedDocument != null;
        }

        public async Task SetProcessedAt(string route, string messageId, CancellationToken cancellationToken)
        {
            var collection = _mongoAgent.GetEnvelops(route);
            var query = Query<Envelope>.EQ(x => x.Id, messageId);
            var update = Update<Envelope>.Set(x => x.ProcessedAt, DateTime.UtcNow).Set(x => x.IsProcessed, true);
            collection.FindAndModify(new FindAndModifyArgs
            {
                Update = update,
                Query = query,
                Upsert = false,
            });
        }

        public async Task<Envelope> TrySetReadAt(string route, string messageId, CancellationToken cancellationToken)
        {
            var collection = _mongoAgent.GetEnvelops(route);
            var notReadAndIdQuery = Query.And(Query<Envelope>.EQ(x => x.Id, messageId),
                Query<Envelope>.EQ(x => x.IsRead, false));
            var update = Update<Envelope>.Set(x => x.ReadAt, DateTime.UtcNow).Set(x => x.IsRead, true);
            var result = collection.FindAndModify(new FindAndModifyArgs
            {
                Update = update,
                Query = notReadAndIdQuery,
                VersionReturned = FindAndModifyDocumentVersion.Modified,
                Upsert = false
            });
            return result.GetModifiedDocumentAs<Envelope>();
        }
    }
}