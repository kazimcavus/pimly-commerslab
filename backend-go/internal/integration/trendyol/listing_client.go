package trendyol

import (
	"context"
	"fmt"
	"net/url"
	"strconv"
	"strings"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/application/listingsync"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// ListingClient, Trendyol ürün kartı ucuna toplu içerik gönderen istemcidir
// (.NET TrendyolMarketplaceListingClient portu). Bu istemci GERÇEK mağazaya
// YAZAR — çağrıldığında Trendyol'da yeni ürün kartı açılır ya da mevcut kart
// güncellenir (yeniden onaya girer).
type ListingClient struct {
	client *Client
}

// NewListingClient, ortak istemciyle içerik istemcisini oluşturur.
func NewListingClient(client *Client) *ListingClient {
	return &ListingClient{client: client}
}

// MaxBatchSize, tek istekte gönderilebilecek en fazla kart sayısıdır.
func (c *ListingClient) MaxBatchSize() int { return 1000 }

type trendyolProductsRequest struct {
	Items []trendyolProductItemRequest `json:"items"`
}

type trendyolProductItemRequest struct {
	Barcode       string                      `json:"barcode"`
	Title         string                      `json:"title"`
	ProductMainID string                      `json:"productMainId"`
	Brand         *string                     `json:"brand"`
	BrandID       *int64                      `json:"brandId"`
	CategoryID    int64                       `json:"categoryId"`
	Quantity      int                         `json:"quantity"`
	StockCode     string                      `json:"stockCode"`
	Description   *string                     `json:"description"`
	ListPrice     string                      `json:"listPrice"`
	SalePrice     string                      `json:"salePrice"`
	CurrencyType  string                      `json:"currencyType"`
	Images        []trendyolProductImageWrite `json:"images"`
	Attributes    []trendyolProductAttrWrite  `json:"attributes"`
}

type trendyolProductImageWrite struct {
	URL string `json:"url"`
}

type trendyolProductAttrWrite struct {
	AttributeID          int64   `json:"attributeId"`
	AttributeValueID     *int64  `json:"attributeValueId"`
	CustomAttributeValue *string `json:"customAttributeValue"`
}

// parseExternalID, harici id dizgisini sayısal Trendyol kimliğine çevirir;
// parse edilemezse nil döner (.NET ParseExternalId portu).
func parseExternalID(value *string) *int64 {
	if value == nil {
		return nil
	}
	parsed, err := strconv.ParseInt(strings.TrimSpace(*value), 10, 64)
	if err != nil {
		return nil
	}
	return &parsed
}

// toTrendyolItem, listeleme isteğini Trendyol ürün kartı gövdesine çevirir
// (.NET ToTrendyolItem portu).
func toTrendyolItem(listing listingsync.MarketplaceListingRequest) trendyolProductItemRequest {
	stockCode := listing.Barcode
	if listing.Sku != nil && strings.TrimSpace(*listing.Sku) != "" {
		stockCode = *listing.Sku
	}
	listPrice := listing.Amount
	if listing.CompareAtAmount != nil {
		listPrice = *listing.CompareAtAmount
	}
	categoryID := int64(0)
	if parsed := parseExternalID(&listing.ExternalCategoryID); parsed != nil {
		categoryID = *parsed
	}

	images := make([]trendyolProductImageWrite, len(listing.ImageURLs))
	for i, url := range listing.ImageURLs {
		images[i] = trendyolProductImageWrite{URL: url}
	}
	attributes := make([]trendyolProductAttrWrite, len(listing.Attributes))
	for i, attribute := range listing.Attributes {
		attributeID := int64(0)
		if parsed := parseExternalID(&attribute.ExternalAttributeID); parsed != nil {
			attributeID = *parsed
		}
		attributes[i] = trendyolProductAttrWrite{
			AttributeID: attributeID, AttributeValueID: parseExternalID(attribute.ExternalValueID),
			CustomAttributeValue: attribute.CustomValue,
		}
	}

	return trendyolProductItemRequest{
		Barcode: listing.Barcode, Title: listing.Title, ProductMainID: listing.ModelCode,
		Brand: listing.BrandName, BrandID: parseExternalID(listing.BrandExternalID),
		CategoryID: categoryID, Quantity: listing.Quantity, StockCode: stockCode,
		Description: listing.Description, ListPrice: listPrice, SalePrice: listing.Amount,
		CurrencyType: listing.Currency, Images: images, Attributes: attributes,
	}
}

// Submit, kart partisini gönderir; isUpdate true ise PUT (güncelleme), false
// ise POST (yeni kart) kullanılır (.NET SubmitAsync portu).
func (c *ListingClient) Submit(ctx context.Context, credentials *application.MarketplaceCredentials, listings []listingsync.MarketplaceListingRequest, isUpdate bool) sharedkernel.ResultOf[listingsync.ListingSubmissionReceipt] {
	if credentials == nil || credentials.SellerID == nil || strings.TrimSpace(*credentials.SellerID) == "" {
		return sharedkernel.FailOf[listingsync.ListingSubmissionReceipt](
			sharedkernel.NewValidationError("Seller id is required to submit Trendyol listings."))
	}
	if len(listings) == 0 {
		return sharedkernel.OkOf(listingsync.ListingSubmissionReceipt{})
	}

	items := make([]trendyolProductItemRequest, len(listings))
	for i, listing := range listings {
		items[i] = toTrendyolItem(listing)
	}

	sellerID := url.PathEscape(strings.TrimSpace(*credentials.SellerID))
	path := fmt.Sprintf("/integration/product/sellers/%s/products", sellerID)
	method := "POST"
	if isUpdate {
		method = "PUT"
	}

	var response trendyolBatchResponse
	if err := c.client.SendJSON(ctx, method, ClassProductsWrite, path, credentials,
		trendyolProductsRequest{Items: items}, &response); err != nil {
		return sharedkernel.FailOf[listingsync.ListingSubmissionReceipt](err)
	}
	return sharedkernel.OkOf(listingsync.ListingSubmissionReceipt{SubmissionReference: response.BatchRequestID})
}

// StubListingClient, Trendyol'a HİÇBİR istek göndermeden kartları kabul eder
// (testler ve UseStubTaxonomyClient=true modu için — gerçek mağazaya yazma
// riski olmadan pipeline'ı uçtan uca doğrulamak amacıyla).
type StubListingClient struct{}

// MaxBatchSize, sabit örnek parti boyutunu döner.
func (StubListingClient) MaxBatchSize() int { return 1000 }

// Submit, hiçbir HTTP çağrısı yapmadan başarı döner.
func (StubListingClient) Submit(_ context.Context, _ *application.MarketplaceCredentials, _ []listingsync.MarketplaceListingRequest, _ bool) sharedkernel.ResultOf[listingsync.ListingSubmissionReceipt] {
	stub := "stub-batch-request-id"
	return sharedkernel.OkOf(listingsync.ListingSubmissionReceipt{SubmissionReference: &stub})
}
