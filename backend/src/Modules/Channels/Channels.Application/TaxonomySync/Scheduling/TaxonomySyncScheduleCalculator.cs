using System.Globalization;

namespace Channels.Application.TaxonomySync.Scheduling;

/// <summary>UTC taxonomy sync zaman dilimi hesapları.</summary>
internal static class TaxonomySyncScheduleCalculator
{
    internal static IReadOnlyList<TimeOnly> ParseTimesUtc(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException("At least one taxonomy sync time must be configured.");
        }

        return values
            .Select(value => TimeOnly.Parse(value.Trim(), CultureInfo.InvariantCulture))
            .OrderBy(time => time)
            .ToList();
    }

    /// <summary>Verilen ana için geçerli UTC zaman diliminin başlangıcını döndürür.</summary>
    internal static DateTimeOffset GetCurrentSlotStartUtc(DateTimeOffset now, IReadOnlyList<TimeOnly> scheduleTimesUtc)
    {
        var utcNow = now.ToUniversalTime();
        var today = DateOnly.FromDateTime(utcNow.UtcDateTime);
        var ordered = scheduleTimesUtc.OrderBy(time => time).ToList();

        TimeOnly? matched = null;
        foreach (var time in ordered)
        {
            var slotStart = today.ToDateTime(time, DateTimeKind.Utc);
            if (utcNow.UtcDateTime >= slotStart)
            {
                matched = time;
                continue;
            }

            break;
        }

        if (matched is null)
        {
            var previousDay = today.AddDays(-1);
            var lastTime = ordered[^1];
            return new DateTimeOffset(previousDay.ToDateTime(lastTime, DateTimeKind.Utc), TimeSpan.Zero);
        }

        return new DateTimeOffset(today.ToDateTime(matched.Value, DateTimeKind.Utc), TimeSpan.Zero);
    }
}
