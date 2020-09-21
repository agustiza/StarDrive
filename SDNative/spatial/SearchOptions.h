#pragma once

namespace spatial
{
    /**
     * Function type for final filtering of search results
     * @return 0 Search filter failed and this object should be excluded
     * @return 1 Search filter Succeeded and object should be included
     */
    using SearchFilterFunc = int (*)(int objectA);

    struct SearchOptions
    {
        /// The initial search origin X, Y coordinates
        int OriginX = 0;
        int OriginY = 0;

        /// Only objects that are within this radius are accepted
        int SearchRadius = 100;

        /// Maximum number of filtered final results until search is terminated
        /// Must be at least 1
        int MaxResults = 10;

        /// Filter search results by object type
        /// 0: disabled
        int FilterByType = 0;

        /// Filter search results by excluding this specific object
        /// -1: disabled
        int FilterExcludeObjectId = -1;

        /// Filter search results by excluding objects with this loyalty
        /// 0: disabled
        int FilterExcludeByLoyalty = 0;

        /// Filter search results by only matching objects with this loyalty
        /// 0: disabled
        int FilterIncludeOnlyByLoyalty = 0;

        /// Filter search results by passing the matched object through this function
        /// null: disabled
        /// Return 0: filter failed, object discarded
        /// Return 1: filter passed, object added to results
        SearchFilterFunc FilterFunction = nullptr;
    };
}
