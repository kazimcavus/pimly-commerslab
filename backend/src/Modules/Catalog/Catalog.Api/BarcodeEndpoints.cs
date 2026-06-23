using Catalog.Api.Requests;
using Catalog.Application.Barcodes.AllocateBarcodes;
using Catalog.Application.Barcodes.GetBarcodeSequence;
using Catalog.Application.Barcodes.ListBarcodeAllocations;
using Catalog.Application.Barcodes.UpdateBarcodeSequence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Barkod serisi endpoint'lerini tanımlar.</summary>
internal static class BarcodeEndpoints
{
    internal static void MapBarcodeEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/barcode-sequence", async (IGetBarcodeSequenceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetBarcodeSequenceQuery());
            return result.ToHttpResult();
        });

        group.MapPut("/barcode-sequence", async (
            UpdateBarcodeSequenceRequest request,
            IUpdateBarcodeSequenceHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateBarcodeSequenceCommand(
                request.NextValue,
                request.ClientAllocationRequired));

            return result.ToHttpResult();
        });

        group.MapPost("/barcodes:allocate", async (
            AllocateBarcodesRequest request,
            IAllocateBarcodesHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new AllocateBarcodesCommand(request.Count));
            return result.ToHttpResult();
        });

        group.MapGet("/barcode-allocations", async (
            IListBarcodeAllocationsHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListBarcodeAllocationsQuery(page, page_size));
            return result.ToHttpResult();
        });
    }
}
