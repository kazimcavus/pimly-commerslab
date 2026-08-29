package trendyol

import (
	"context"
	"fmt"
	"net/url"
	"strconv"
	"strings"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// CategoryAttributesClient, Trendyol kategori özellik şemasını çeker
// (.NET TrendyolMarketplaceCategoryAttributesClient portu): varianter →
// IsVariant, slicer → IsSlicer olarak eşlenir.
type CategoryAttributesClient struct {
	client *Client
}

// NewCategoryAttributesClient, ortak istemciyle özellik istemcisi oluşturur.
func NewCategoryAttributesClient(client *Client) *CategoryAttributesClient {
	return &CategoryAttributesClient{client: client}
}

// Trendyol yanıt biçimi (kısmi).
type trendyolAttributeRef struct {
	ID   int64   `json:"id"`
	Name *string `json:"name"`
}

type trendyolAttributeValue struct {
	ID   int64   `json:"id"`
	Name *string `json:"name"`
}

type trendyolCategoryAttribute struct {
	Attribute       *trendyolAttributeRef    `json:"attribute"`
	Required        bool                     `json:"required"`
	AllowCustom     bool                     `json:"allowCustom"`
	Varianter       bool                     `json:"varianter"`
	Slicer          bool                     `json:"slicer"`
	AttributeValues []trendyolAttributeValue `json:"attributeValues"`
}

type trendyolCategoryAttributesResponse struct {
	CategoryAttributes []trendyolCategoryAttribute `json:"categoryAttributes"`
}

// FetchCategoryAttributes, kategori özelliklerini pazaryerinden çeker.
func (c *CategoryAttributesClient) FetchCategoryAttributes(ctx context.Context, credentials *application.MarketplaceCredentials, externalCategoryID string) sharedkernel.ResultOf[[]application.MarketplaceCategoryAttributeNode] {
	path := fmt.Sprintf("/integration/product/product-categories/%s/attributes",
		url.PathEscape(strings.TrimSpace(externalCategoryID)))

	var response trendyolCategoryAttributesResponse
	if err := c.client.GetJSON(ctx, ClassTaxonomy, path, credentials, &response); err != nil {
		return sharedkernel.FailOf[[]application.MarketplaceCategoryAttributeNode](err)
	}

	nodes := make([]application.MarketplaceCategoryAttributeNode, 0, len(response.CategoryAttributes))
	for _, attribute := range response.CategoryAttributes {
		if attribute.Attribute == nil || attribute.Attribute.Name == nil ||
			strings.TrimSpace(*attribute.Attribute.Name) == "" {
			continue
		}
		values := make([]application.MarketplaceAttributeValueNode, 0, len(attribute.AttributeValues))
		for _, value := range attribute.AttributeValues {
			if value.Name == nil || strings.TrimSpace(*value.Name) == "" {
				continue
			}
			values = append(values, application.MarketplaceAttributeValueNode{
				ExternalValueID: strconv.FormatInt(value.ID, 10), Name: *value.Name})
		}
		nodes = append(nodes, application.MarketplaceCategoryAttributeNode{
			ExternalAttributeID: strconv.FormatInt(attribute.Attribute.ID, 10),
			Name:                *attribute.Attribute.Name,
			Required:            attribute.Required,
			AllowCustom:         attribute.AllowCustom,
			IsVariant:           attribute.Varianter,
			IsSlicer:            attribute.Slicer,
			Values:              values,
		})
	}
	return sharedkernel.OkOf(nodes)
}

// StubCategoryAttributesClient, testler ve UseStubTaxonomyClient=true modu için
// pazaryerine gitmeden sabit bir şema döner.
type StubCategoryAttributesClient struct{}

// FetchCategoryAttributes, sabit örnek şema döner.
func (StubCategoryAttributesClient) FetchCategoryAttributes(_ context.Context, _ *application.MarketplaceCredentials, _ string) sharedkernel.ResultOf[[]application.MarketplaceCategoryAttributeNode] {
	return sharedkernel.OkOf([]application.MarketplaceCategoryAttributeNode{
		{
			ExternalAttributeID: "338", Name: "Renk", Required: true, AllowCustom: false,
			IsVariant: true, IsSlicer: true,
			Values: []application.MarketplaceAttributeValueNode{
				{ExternalValueID: "633", Name: "Kırmızı"},
				{ExternalValueID: "634", Name: "Mavi"},
			},
		},
		{
			ExternalAttributeID: "47", Name: "Materyal", Required: false, AllowCustom: true,
			Values: []application.MarketplaceAttributeValueNode{
				{ExternalValueID: "100", Name: "Pamuk"},
			},
		},
	})
}
