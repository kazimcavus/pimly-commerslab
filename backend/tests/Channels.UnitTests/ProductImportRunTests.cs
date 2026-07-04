using Channels.Domain.Marketplaces;
using Channels.Domain.ProductImports;
using FluentAssertions;

namespace Channels.UnitTests;

/// <summary>ProductImportRun durum makinesi ve hata sınırı için birim testleri.</summary>
public class ProductImportRunTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);

    private static ProductImportRun CreateRun() =>
        ProductImportRun.Create(Guid.NewGuid(), Marketplace.Trendyol, Now).Value;

    [Fact]
    public void Create_EmptyTenant_Fails()
    {
        var result = ProductImportRun.Create(Guid.Empty, Marketplace.Trendyol, Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Valid_StartsPending()
    {
        var run = CreateRun();

        run.Status.Should().Be(ProductImportStatus.Pending);
        run.IsActive().Should().BeTrue();
        run.Marketplace.Should().Be(Marketplace.Trendyol);
    }

    [Fact]
    public void MarkRunning_FromPending_Succeeds()
    {
        var run = CreateRun();

        run.MarkRunning(Now.AddSeconds(5)).IsSuccess.Should().BeTrue();
        run.Status.Should().Be(ProductImportStatus.Running);
        run.StartedAt.Should().Be(Now.AddSeconds(5));
    }

    [Fact]
    public void MarkRunning_Twice_Conflicts()
    {
        var run = CreateRun();
        run.MarkRunning(Now);

        run.MarkRunning(Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkCompleted_WithoutFailures_IsCompleted()
    {
        var run = CreateRun();
        run.MarkRunning(Now);
        run.UpdateProgress(5, 4, 1, 0, 5);

        run.MarkCompleted(Now.AddMinutes(1)).IsSuccess.Should().BeTrue();
        run.Status.Should().Be(ProductImportStatus.Completed);
        run.IsActive().Should().BeFalse();
    }

    [Fact]
    public void MarkCompleted_WithFailures_IsCompletedWithErrors()
    {
        var run = CreateRun();
        run.MarkRunning(Now);
        run.UpdateProgress(5, 3, 1, 1, 5);

        run.MarkCompleted(Now.AddMinutes(1)).IsSuccess.Should().BeTrue();
        run.Status.Should().Be(ProductImportStatus.CompletedWithErrors);
    }

    [Fact]
    public void MarkCompleted_FromPending_Conflicts()
    {
        var run = CreateRun();

        run.MarkCompleted(Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkFailed_FromRunning_SetsMessage()
    {
        var run = CreateRun();
        run.MarkRunning(Now);

        run.MarkFailed(Now.AddMinutes(1), "connection lost").IsSuccess.Should().BeTrue();
        run.Status.Should().Be(ProductImportStatus.Failed);
        run.ErrorMessage.Should().Be("connection lost");
    }

    [Fact]
    public void AddError_CapsAtMaxErrors()
    {
        var run = CreateRun();

        for (var i = 0; i < ProductImportRun.MaxErrors; i++)
        {
            run.AddError($"MAIN-{i}", null, "hata").Should().BeTrue();
        }

        run.AddError("MAIN-EXTRA", null, "hata").Should().BeFalse();
        run.Errors.Should().HaveCount(ProductImportRun.MaxErrors);
    }

    [Fact]
    public void AddError_TruncatesLongMessages()
    {
        var run = CreateRun();
        var longMessage = new string('x', ProductImportError.MessageMaxLength + 50);

        run.AddError("MAIN-1", "868000", longMessage).Should().BeTrue();
        run.Errors.Single().Message.Length.Should().Be(ProductImportError.MessageMaxLength);
        run.Errors.Single().Barcode.Should().Be("868000");
    }
}
