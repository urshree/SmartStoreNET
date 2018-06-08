using System;
using System.Collections.Generic;
using System.Linq;
using SmartStore.Core;
using SmartStore.Core.Data;
using SmartStore.Core.Domain.Stores;
using SmartStore.Core.Domain.Topics;
using SmartStore.Core.Events;
using SmartStore.Data.Caching;
using SmartStore.Services.Stores;

namespace SmartStore.Services.Topics
{
    public partial class TopicService : ITopicService
    {
		private readonly IRepository<Topic> _topicRepository;
		private readonly IRepository<StoreMapping> _storeMappingRepository;
		private readonly IStoreMappingService _storeMappingService;
		private readonly IEventPublisher _eventPublisher;

		public TopicService(
			IRepository<Topic> topicRepository,
			IRepository<StoreMapping> storeMappingRepository,
			IStoreMappingService storeMappingService,
			IEventPublisher eventPublisher)
        {
            _topicRepository = topicRepository;
			_storeMappingRepository = storeMappingRepository;
			_storeMappingService = storeMappingService;
			_eventPublisher = eventPublisher;

			this.QuerySettings = DbQuerySettings.Default;
		}

		public DbQuerySettings QuerySettings { get; set; }

        public virtual void DeleteTopic(Topic topic)
        {
			Guard.NotNull(topic, nameof(topic));

			_topicRepository.Delete(topic);
        }

        public virtual Topic GetTopicById(int topicId)
        {
            if (topicId == 0)
                return null;

            return _topicRepository.GetById(topicId);
        }

		public virtual Topic GetTopicBySystemName(string systemName, int storeId = 0)
        {
			if (systemName.IsEmpty())
				return null;

			var query = GetAllTopicsQuery(systemName, storeId);
			var result = query.FirstOrDefaultCached("db.topic.bysysname-{0}-{1}".FormatInvariant(systemName, storeId));

			return result;
        }

		public virtual IPagedList<Topic> GetAllTopics(int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
        {
			var query = GetAllTopicsQuery(null, storeId);
			return new PagedList<Topic>(query, pageIndex, pageSize);
		}

		protected virtual IQueryable<Topic> GetAllTopicsQuery(string systemName, int storeId)
		{
			var query = _topicRepository.Table;

			// Store mapping
			if (storeId > 0 && !QuerySettings.IgnoreMultiStore)
			{
				query = from t in query
						join sm in _storeMappingRepository.Table
						on new { c1 = t.Id, c2 = "Topic" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into t_sm
						from sm in t_sm.DefaultIfEmpty()
						where !t.LimitedToStores || storeId == sm.StoreId
						select t;

				// Only distinct items (group by ID)
				query = from t in query
						group t by t.Id into tGroup
						orderby tGroup.Key
						select tGroup.FirstOrDefault();
			}

			if (systemName.HasValue())
			{
				query = query.Where(x => x.SystemName == systemName);
			}

			query = query.OrderBy(t => t.Priority).ThenBy(t => t.SystemName);

			return query;
		}

        public virtual void InsertTopic(Topic topic)
        {
			Guard.NotNull(topic, nameof(topic));

			_topicRepository.Insert(topic);
        }

        public virtual void UpdateTopic(Topic topic)
        {
			Guard.NotNull(topic, nameof(topic));

			_topicRepository.Update(topic);
        }
    }
}
