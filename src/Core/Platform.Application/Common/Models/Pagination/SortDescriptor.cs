namespace Platform.Application.Common.Models.Pagination;

public sealed record SortDescriptor(string Field, SortDirection Direction = SortDirection.Asc);

public enum SortDirection { Asc, Desc }
