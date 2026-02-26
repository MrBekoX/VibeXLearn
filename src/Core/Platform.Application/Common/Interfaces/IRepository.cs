using Platform.Domain.Common;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Her ikisine birden ihtiyaç duyan (nadir) servisler için composite arayüz.
/// </summary>
public interface IRepository<T> : IReadRepository<T>, IWriteRepository<T>
    where T : BaseEntity { }
