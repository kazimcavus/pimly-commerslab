using System.Globalization;
using Catalog.Domain.Variants;
using SharedKernel;

namespace Catalog.Domain.SkuGenerator;

/// <summary>Segment şablonundan ürün kodu ve varyant SKU üretir.</summary>
public static class SkuCodeAssembler
{
    private const string KeySource = "key";
    private const string LegacyCodeSource = "code";

    /// <summary>Ürün seviyesi kod üretir (color/size segmentleri hariç).</summary>
    public static Result<(string Code, long NextCounter)> AssembleProductCode(
        IReadOnlyList<SkuSegment> segments,
        IReadOnlyList<string>? codeInputs,
        long counterValue,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var year = now.Year;
        var builder = new System.Text.StringBuilder();

        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            if (segment.IsVariantSegment)
            {
                continue;
            }

            var tokenResult = BuildNonVariantToken(segment, codeInputs, index, ref counterValue, year);
            if (tokenResult.IsFailure)
            {
                return Result.Failure<(string, long)>(tokenResult.Error);
            }

            builder.Append(tokenResult.Value);
        }

        return Result.Success((builder.ToString(), counterValue));
    }

    /// <summary>Varyant SKU üretir — ürün kodu + color/size tokenları.</summary>
    public static string AssembleVariantSku(
        string productCode,
        IReadOnlyList<SkuSegment> segments,
        IReadOnlyList<SkuVariantSelection> variantSelections)
    {
        var builder = new System.Text.StringBuilder(productCode);

        foreach (var segment in segments)
        {
            if (string.Equals(segment.Type, SkuSegmentTypes.Color, StringComparison.OrdinalIgnoreCase))
            {
                var color = variantSelections.FirstOrDefault(selection =>
                    selection.SelectionStyle == SelectionStyle.Color);
                if (color is not null)
                {
                    builder.Append(VariantToken(color, segment.Source));
                }
            }
            else if (string.Equals(segment.Type, SkuSegmentTypes.Size, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var selection in variantSelections.Where(v => v.SelectionStyle != SelectionStyle.Color))
                {
                    builder.Append(VariantToken(selection, segment.Source));
                }
            }
        }

        return builder.ToString();
    }

    /// <summary>Varyant değerinden token üretir.</summary>
    public static string VariantToken(SkuVariantSelection selection, string? source)
    {
        var useName = string.Equals(source, "name", StringComparison.OrdinalIgnoreCase);
        var raw = useName
            ? selection.Name
            : selection.Key ?? selection.Name;

        return (raw ?? string.Empty).Trim().ToUpperInvariant();
    }

    /// <summary>Manual segment girdilerinin eksiksiz olup olmadığını doğrular.</summary>
    /// <param name="segments">Segment şablonu.</param>
    /// <param name="codeInputs">Manual segment değerleri; segment sırasına göre.</param>
    public static Result ValidateManualInputs(
        IReadOnlyList<SkuSegment> segments,
        IReadOnlyList<string>? codeInputs)
    {
        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            if (!string.Equals(segment.Type, SkuSegmentTypes.Manual, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = codeInputs is not null && index < codeInputs.Count
                ? codeInputs[index]
                : null;

            if (string.IsNullOrWhiteSpace(value))
            {
                var label = string.IsNullOrWhiteSpace(segment.Label) ? "manual segment" : segment.Label;
                return Result.Failure(Error.Validation($"Manual segment '{label}' is required."));
            }
        }

        return Result.Success();
    }

    /// <summary>Key kaynağı kullanan varyant segmentleri için değer anahtarlarının tanımlı olup olmadığını doğrular.</summary>
    /// <param name="segments">Segment şablonu.</param>
    /// <param name="variantSelections">Ürün kalemindeki varyant seçimleri.</param>
    public static Result ValidateVariantCodes(
        IReadOnlyList<SkuSegment> segments,
        IReadOnlyList<SkuVariantSelection> variantSelections)
    {
        foreach (var segment in segments)
        {
            if (segment.IsVariantSegment &&
                UsesKeySource(segment.Source))
            {
                var selections = string.Equals(segment.Type, SkuSegmentTypes.Color, StringComparison.OrdinalIgnoreCase)
                    ? variantSelections.Where(v => v.SelectionStyle == SelectionStyle.Color)
                    : variantSelections.Where(v => v.SelectionStyle != SelectionStyle.Color);

                foreach (var selection in selections)
                {
                    if (string.IsNullOrWhiteSpace(selection.Key))
                    {
                        return Result.Failure(Error.Validation(
                            $"Variant value '{selection.Name}' requires a key for SKU segment '{segment.Type}'."));
                    }
                }
            }
        }

        return Result.Success();
    }

    private static bool UsesKeySource(string? source) =>
        string.Equals(source, KeySource, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(source, LegacyCodeSource, StringComparison.OrdinalIgnoreCase);

    private static Result<string> BuildNonVariantToken(
        SkuSegment segment,
        IReadOnlyList<string>? codeInputs,
        int index,
        ref long counterValue,
        int year) =>
        segment.Type.ToLowerInvariant() switch
        {
            SkuSegmentTypes.Fixed => Result.Success((segment.Value ?? string.Empty).Trim().ToUpperInvariant()),
            SkuSegmentTypes.Manual => BuildManualToken(segment, codeInputs, index),
            SkuSegmentTypes.Counter => BuildCounterToken(segment, ref counterValue),
            SkuSegmentTypes.Year => Result.Success(BuildYearToken(segment, year)),
            _ => Result.Success(string.Empty),
        };

    private static Result<string> BuildManualToken(
        SkuSegment segment,
        IReadOnlyList<string>? codeInputs,
        int index)
    {
        var value = codeInputs is not null && index < codeInputs.Count
            ? codeInputs[index]
            : null;

        if (string.IsNullOrWhiteSpace(value))
        {
            var label = string.IsNullOrWhiteSpace(segment.Label) ? "manual segment" : segment.Label;
            return Result.Failure<string>(Error.Validation($"Manual segment '{label}' is required."));
        }

        return Result.Success(value.Trim().ToUpperInvariant());
    }

    private static Result<string> BuildCounterToken(SkuSegment segment, ref long counterValue)
    {
        var width = segment.Width is > 0 ? segment.Width.Value : 4;
        var token = counterValue.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
        counterValue++;
        return Result.Success(token);
    }

    private static string BuildYearToken(SkuSegment segment, int year) =>
        segment.Digits == 4
            ? year.ToString("D4", CultureInfo.InvariantCulture)
            : (year % 100).ToString("D2", CultureInfo.InvariantCulture);
}

/// <summary>SKU üretiminde kullanılan varyant değeri anlık görüntüsü.</summary>
public sealed record SkuVariantSelection(
    SelectionStyle SelectionStyle,
    string Name,
    string? Key);
