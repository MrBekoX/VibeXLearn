using Platform.Application.Common.Models.Pagination;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Marker interface for paginated queries.
/// Enables PaginationValidationBehaviour to automatically validate pagination parameters.
/// </summary>
public interface IPagedQuery
{
    PageRequest PageRequest { get; }
}
