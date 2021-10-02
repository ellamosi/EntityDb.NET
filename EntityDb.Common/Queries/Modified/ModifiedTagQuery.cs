using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Queries.FilterBuilders;
using EntityDb.Abstractions.Queries.SortBuilders;
using EntityDb.Common.Extensions;

namespace EntityDb.Common.Queries.Modified
{
    internal sealed record ModifiedTagQuery(ITagQuery TagQuery, bool InvertFilter, bool ReverseSort, int? ReplaceSkip, int? ReplaceTake) : ModifiedQueryBase(TagQuery, ReplaceSkip, ReplaceTake), ITagQuery
    {
        public TFilter GetFilter<TFilter>(ITagFilterBuilder<TFilter> builder)
        {
            if (InvertFilter)
            {
                return builder.Not
                (
                    TagQuery.GetFilter(builder)
                );
            }

            return TagQuery.GetFilter(builder);
        }

        public TSort? GetSort<TSort>(ITagSortBuilder<TSort> builder)
        {
            if (ReverseSort)
            {
                return TagQuery.GetSort(builder.Reverse());
            }

            return TagQuery.GetSort(builder);
        }
    }
}
