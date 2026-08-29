package trendyol

import (
	"context"
	"encoding/json"
	"fmt"
	"net/url"
	"strconv"
	"strings"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/application/productimports"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// ProductsClient, Trendyol satıcı ürünleri istemcisidir
// (.NET TrendyolMarketplaceProductsClient portu). Sayfalı ürün listesini çeker;
// varyantlar productMainId ile gruplu, attribute'lar düz listedir. Fiyatlar
// ham JSON sayısı olarak taşınır (449.90 hiçbir katmanda 449.9'a çökmez).
type ProductsClient struct {
	client *Client
}

// NewProductsClient, ortak istemciyle ürünler istemcisini oluşturur.
func NewProductsClient(client *Client) *ProductsClient {
	return &ProductsClient{client: client}
}

// Trendyol ürünler yanıt biçimi (kısmi).
type trendyolProductsResponse struct {
	TotalElements int64             `json:"totalElements"`
	TotalPages    int               `json:"totalPages"`
	Page          int               `json:"page"`
	Size          int               `json:"size"`
	Content       []trendyolProduct `json:"content"`
}

type trendyolProduct struct {
	Barcode       *string                    `json:"barcode"`
	Title         *string                    `json:"title"`
	ProductMainID *string                    `json:"productMainId"`
	Brand         *string                    `json:"brand"`
	BrandID       *int64                     `json:"brandId"`
	StockCode     *string                    `json:"stockCode"`
	Quantity      int                        `json:"quantity"`
	ListPrice     json.Number                `json:"listPrice"`
	SalePrice     json.Number                `json:"salePrice"`
	CurrencyType  *string                    `json:"currencyType"`
	PimCategoryID *int64                     `json:"pimCategoryId"`
	CategoryName  *string                    `json:"categoryName"`
	Description   *string                    `json:"description"`
	Approved      bool                       `json:"approved"`
	Images        []trendyolProductImage     `json:"images"`
	Attributes    []trendyolProductAttribute `json:"attributes"`
}

type trendyolProductImage struct {
	URL *string `json:"url"`
}

type trendyolProductAttribute struct {
	AttributeID          *int64  `json:"attributeId"`
	AttributeName        *string `json:"attributeName"`
	AttributeValueID     *int64  `json:"attributeValueId"`
	AttributeValue       *string `json:"attributeValue"`
	CustomAttributeValue *string `json:"customAttributeValue"`
}

// FetchProductsPage, onaylı ürünlerin verilen sayfasını çeker; barkodu ya da
// başlığı boş satırlar elenir, ProductMainID boşsa barkoda düşülür.
func (c *ProductsClient) FetchProductsPage(ctx context.Context, credentials *application.MarketplaceCredentials, page, size int) sharedkernel.ResultOf[productimports.MarketplaceProductPage] {
	if credentials == nil || credentials.SellerID == nil || strings.TrimSpace(*credentials.SellerID) == "" {
		return sharedkernel.FailOf[productimports.MarketplaceProductPage](
			sharedkernel.NewValidationError("Seller id is required to fetch Trendyol products."))
	}
	sellerID := url.PathEscape(strings.TrimSpace(*credentials.SellerID))
	path := fmt.Sprintf("/integration/product/sellers/%s/products?page=%d&size=%d&approved=true",
		sellerID, page, size)

	var response trendyolProductsResponse
	if err := c.client.GetJSON(ctx, ClassProductsRead, path, credentials, &response); err != nil {
		return sharedkernel.FailOf[productimports.MarketplaceProductPage](err)
	}

	items := []productimports.MarketplaceProductNode{}
	for _, product := range response.Content {
		if product.Barcode == nil || strings.TrimSpace(*product.Barcode) == "" ||
			product.Title == nil || strings.TrimSpace(*product.Title) == "" {
			continue
		}
		barcode := strings.TrimSpace(*product.Barcode)
		mainID := barcode
		if product.ProductMainID != nil && strings.TrimSpace(*product.ProductMainID) != "" {
			mainID = strings.TrimSpace(*product.ProductMainID)
		}
		externalCategoryID := ""
		if product.PimCategoryID != nil {
			externalCategoryID = strconv.FormatInt(*product.PimCategoryID, 10)
		}

		imageURLs := []string{}
		for _, image := range product.Images {
			if image.URL != nil && strings.TrimSpace(*image.URL) != "" {
				imageURLs = append(imageURLs, strings.TrimSpace(*image.URL))
			}
		}

		attributes := []productimports.MarketplaceProductAttributeNode{}
		for _, attribute := range product.Attributes {
			if attribute.AttributeID == nil {
				continue
			}
			name := ""
			if attribute.AttributeName != nil {
				name = *attribute.AttributeName
			}
			var externalValueID *string
			if attribute.AttributeValueID != nil {
				formatted := strconv.FormatInt(*attribute.AttributeValueID, 10)
				externalValueID = &formatted
			}
			attributes = append(attributes, productimports.MarketplaceProductAttributeNode{
				ExternalAttributeID: strconv.FormatInt(*attribute.AttributeID, 10),
				Name:                name,
				ExternalValueID:     externalValueID,
				Value:               attribute.AttributeValue,
				CustomValue:         attribute.CustomAttributeValue,
			})
		}

		var brandExternalID *string
		if product.BrandID != nil {
			formatted := strconv.FormatInt(*product.BrandID, 10)
			brandExternalID = &formatted
		}
		items = append(items, productimports.MarketplaceProductNode{
			Barcode:            barcode,
			Title:              strings.TrimSpace(*product.Title),
			ProductMainID:      mainID,
			Brand:              product.Brand,
			StockCode:          product.StockCode,
			Quantity:           product.Quantity,
			ListPrice:          product.ListPrice.String(),
			SalePrice:          product.SalePrice.String(),
			CurrencyType:       product.CurrencyType,
			ExternalCategoryID: externalCategoryID,
			CategoryName:       product.CategoryName,
			Description:        product.Description,
			Approved:           product.Approved,
			ImageURLs:          imageURLs,
			Attributes:         attributes,
			BrandExternalID:    brandExternalID,
		})
	}

	return sharedkernel.OkOf(productimports.MarketplaceProductPage{
		TotalElements: response.TotalElements,
		TotalPages:    response.TotalPages,
		Page:          response.Page,
		Size:          response.Size,
		Items:         items,
	})
}

// StubProductsClient, testler ve stub modu için tek sayfalık sabit ürün
// listesi döner.
type StubProductsClient struct{}

// FetchProductsPage, sabit örnek sayfayı döner.
func (StubProductsClient) FetchProductsPage(_ context.Context, _ *application.MarketplaceCredentials, page, size int) sharedkernel.ResultOf[productimports.MarketplaceProductPage] {
	if page > 0 {
		return sharedkernel.OkOf(productimports.MarketplaceProductPage{
			TotalElements: 1, TotalPages: 1, Page: page, Size: size})
	}
	value := "Mavi"
	valueID := "686"
	return sharedkernel.OkOf(productimports.MarketplaceProductPage{
		TotalElements: 1, TotalPages: 1, Page: 0, Size: size,
		Items: []productimports.MarketplaceProductNode{{
			Barcode: "8690000000001", Title: "Stub Tişört", ProductMainID: "STUB-MODEL-1",
			Quantity: 5, ListPrice: "199.90", SalePrice: "149.90",
			ExternalCategoryID: "3", Approved: true,
			Attributes: []productimports.MarketplaceProductAttributeNode{{
				ExternalAttributeID: "47", Name: "Renk", ExternalValueID: &valueID, Value: &value,
			}},
		}},
	})
}
