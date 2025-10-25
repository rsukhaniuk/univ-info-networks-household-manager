using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Common
{
    /// <summary>
    /// Base class for query parameters with pagination, sorting, and searching
    /// </summary>
    public abstract class BaseQueryParameters
    {
        /// <summary>
        /// Maximum allowed page size to prevent abuse
        /// </summary>
        private const int MaxPageSize = 100;

        private int _pageSize = 20;

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page (max 100)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        /// <summary>
        /// Field to sort by
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort order: 'asc' or 'desc' (default: 'desc')
        /// </summary>
        public string SortOrder { get; set; } = "desc";

        /// <summary>
        /// Search term for text-based searching
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Indicates if sorting is in ascending order
        /// </summary>
        [JsonIgnore]
        [SwaggerIgnore]
        public bool IsAscending => SortOrder?.ToLower() == "asc";

        /// <summary>
        /// Calculates the number of items to skip for pagination
        /// </summary>
        [JsonIgnore]
        [SwaggerIgnore]
        public int Skip => (Page - 1) * PageSize;
    }
}
