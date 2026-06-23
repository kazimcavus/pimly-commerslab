using SharedKernel;

namespace Catalog.Domain.SkuGenerator;

/// <summary>SKU oluşturucu yapılandırması — tenant başına tek satır.</summary>
public sealed class SkuGeneratorConfig : Entity<int>
{
    public const int SingletonId = 1;

    private SkuGeneratorConfig()
    {
    }

    private List<SkuSegment> _segments = [];

    private SkuGeneratorConfig(int id, bool enabled, IReadOnlyList<SkuSegment> segments, long counterNextValue)
        : base(id)
    {
        Enabled = enabled;
        _segments = segments.ToList();
        CounterNextValue = counterNextValue;
    }

    /// <summary>Gets a value indicating whether generator açık.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Gets sıralı segment şablonu.</summary>
    public IReadOnlyList<SkuSegment> Segments => _segments;

    /// <summary>Gets bir sonraki counter token değeri.</summary>
    public long CounterNextValue { get; private set; }

    public static SkuGeneratorConfig CreateInitial() =>
        new(SingletonId, false, [], 1);

    public static long DefaultCounterStart(IReadOnlyList<SkuSegment> segments)
    {
        foreach (var segment in segments)
        {
            if (segment.IsCounterSegment && segment.Start is > 0)
            {
                return segment.Start.Value;
            }
        }

        return 1;
    }

    public int CounterSegmentCount =>
        Segments.Count(segment => segment.IsCounterSegment);

    public Result UpdateSettings(bool enabled, IReadOnlyList<SkuSegment> segments, long? counterNextValue)
    {
        if (segments.Count == 0 && enabled)
        {
            return Result.Failure(Error.Validation("At least one segment is required when the SKU generator is enabled."));
        }

        Enabled = enabled;
        _segments = segments.ToList();

        if (counterNextValue.HasValue)
        {
            if (counterNextValue.Value < 1)
            {
                return Result.Failure(Error.Validation("Counter next value must be at least 1."));
            }

            if (counterNextValue.Value < CounterNextValue)
            {
                return Result.Failure(Error.Conflict(
                    $"Counter next value must be at least the current value ({CounterNextValue})."));
            }

            CounterNextValue = counterNextValue.Value;
        }
        else if (CounterNextValue < 1)
        {
            CounterNextValue = DefaultCounterStart(segments);
        }

        return Result.Success();
    }

    public void EnsureCounterInitialized()
    {
        if (CounterNextValue < 1)
        {
            CounterNextValue = DefaultCounterStart(Segments);
        }
    }

    public void SetCounterNextValue(long value) => CounterNextValue = value;
}
