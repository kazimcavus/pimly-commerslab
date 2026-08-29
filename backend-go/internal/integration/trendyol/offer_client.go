package trendyol

import (
	"context"
	"fmt"
	"net/url"
	"strings"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/application/listingsync"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// OfferClient, Trendyol fiyat/stok (price-and-inventory) ucuna toplu teklif
// gönderen istemcidir (.NET TrendyolMarketplaceOfferClient portu). Bu istemci
// GERÇEK mağazaya YAZAR — çağrıldığında Trendyol'daki fiyat/stok değişir.
type OfferClient struct {
	client *Client
}

// NewOfferClient, ortak istemciyle teklif istemcisini oluşturur.
func NewOfferClient(client *Client) *OfferClient {
	return &OfferClient{client: client}
}

// MaxBatchSize, tek istekte gönderilebilecek en fazla teklif sayısıdır.
func (c *OfferClient) MaxBatchSize() int { return 1000 }

type trendyolPriceInventoryRequest struct {
	Items []trendyolPriceInventoryItem `json:"items"`
}

type trendyolPriceInventoryItem struct {
	Barcode   string `json:"barcode"`
	Quantity  int    `json:"quantity"`
	SalePrice string `json:"salePrice"`
	ListPrice string `json:"listPrice"`
}

type trendyolBatchResponse struct {
	BatchRequestID *string `json:"batchRequestId"`
}

// UpdateOffers, teklif partisini gönderir (.NET UpdateOffersAsync portu).
// listPrice, karşılaştırma fiyatı yoksa satış fiyatına eşit yazılır (Trendyol
// listPrice >= salePrice şartını korumak için).
func (c *OfferClient) UpdateOffers(ctx context.Context, credentials *application.MarketplaceCredentials, offers []listingsync.MarketplaceOfferUpdate) sharedkernel.ResultOf[listingsync.OfferUpdateReceipt] {
	if credentials == nil || credentials.SellerID == nil || strings.TrimSpace(*credentials.SellerID) == "" {
		return sharedkernel.FailOf[listingsync.OfferUpdateReceipt](
			sharedkernel.NewValidationError("Seller id is required to update Trendyol offers."))
	}
	if len(offers) == 0 {
		return sharedkernel.OkOf(listingsync.OfferUpdateReceipt{})
	}

	items := make([]trendyolPriceInventoryItem, len(offers))
	for i, offer := range offers {
		listPrice := offer.Amount
		if offer.CompareAtAmount != nil {
			listPrice = *offer.CompareAtAmount
		}
		items[i] = trendyolPriceInventoryItem{
			Barcode: offer.ExternalListingID, Quantity: offer.Quantity,
			SalePrice: offer.Amount, ListPrice: listPrice,
		}
	}

	sellerID := url.PathEscape(strings.TrimSpace(*credentials.SellerID))
	path := fmt.Sprintf("/integration/inventory/sellers/%s/products/price-and-inventory", sellerID)

	var response trendyolBatchResponse
	if err := c.client.SendJSON(ctx, "POST", ClassPriceInventory, path, credentials,
		trendyolPriceInventoryRequest{Items: items}, &response); err != nil {
		return sharedkernel.FailOf[listingsync.OfferUpdateReceipt](err)
	}
	return sharedkernel.OkOf(listingsync.OfferUpdateReceipt{SubmissionReference: response.BatchRequestID})
}

// StubOfferClient, Trendyol'a HİÇBİR istek göndermeden teklifleri kabul eder
// (testler ve UseStubTaxonomyClient=true modu için — gerçek mağazaya yazma
// riski olmadan pipeline'ı uçtan uca doğrulamak amacıyla).
type StubOfferClient struct{}

// MaxBatchSize, sabit örnek parti boyutunu döner.
func (StubOfferClient) MaxBatchSize() int { return 1000 }

// UpdateOffers, hiçbir HTTP çağrısı yapmadan başarı döner.
func (StubOfferClient) UpdateOffers(_ context.Context, _ *application.MarketplaceCredentials, _ []listingsync.MarketplaceOfferUpdate) sharedkernel.ResultOf[listingsync.OfferUpdateReceipt] {
	stub := "stub-batch-request-id"
	return sharedkernel.OkOf(listingsync.OfferUpdateReceipt{SubmissionReference: &stub})
}
