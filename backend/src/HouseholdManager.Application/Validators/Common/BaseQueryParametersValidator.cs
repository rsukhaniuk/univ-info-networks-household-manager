using FluentValidation;
using HouseholdManager.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Validators.Common
{
    /// <summary>
    /// Base validator for query parameters (pagination, sorting, searching)
    /// </summary>
    public class BaseQueryParametersValidator : AbstractValidator<BaseQueryParameters>
    {
        public BaseQueryParametersValidator()
        {
            // Page validation
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            // PageSize validation (max 100 enforced in BaseQueryParameters)
            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(100)
                .WithMessage("Page size cannot exceed 100");

            // SortOrder validation
            RuleFor(x => x.SortOrder)
                .Must(order => order == null || order.ToLower() == "asc" || order.ToLower() == "desc")
                .WithMessage("Sort order must be 'asc' or 'desc'");

            // Search validation (optional)
            RuleFor(x => x.Search)
                .MaximumLength(100)
                .WithMessage("Search term cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Search));
        }
    }
}
