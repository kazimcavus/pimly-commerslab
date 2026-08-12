using Microsoft.EntityFrameworkCore;

namespace Pimly.Outbox;

/// <summary>
/// Outbox tablosunu barındıran modül DbContext'i. Ortak processor bu arabirim üzerinden çalışır;
/// böylece her modül kendi şemasındaki kendi tablosunu kullanırken mekanizma tek yerde durur.
/// </summary>
public interface IOutboxDbContext
{
    /// <summary>Gets modülün outbox tablosu.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }
}
