using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>API E2E testleri için ortak HTTP assertion yardımcıları.</summary>
internal static class CatalogHttpAssertions
{
    internal static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string? expectedCode = null)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        response.StatusCode.Should().Be(expectedStatus);
        problem.Should().NotBeNull();
        if (expectedCode is not null)
        {
            problem!.Title.Should().Be(expectedCode);
        }
    }
}

internal sealed record ProblemDetailsResponse(
    string? Title,
    int? Status,
    string? Detail);

internal sealed record PagedResultResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
