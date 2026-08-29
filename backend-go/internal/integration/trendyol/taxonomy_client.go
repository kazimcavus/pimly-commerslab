package trendyol

import (
	"context"
	"strconv"
	"strings"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// CategoryNode, düzleştirilmiş pazaryeri kategori düğümüdür
// (.NET MarketplaceCategoryNode karşılığı).
type CategoryNode struct {
	ExternalID       string
	Name             string
	ParentExternalID *string
	Path             string
	IsLeaf           bool
}

// TaxonomyClient, pazaryeri kategori ağacını çeken porttur.
type TaxonomyClient interface {
	FetchAllCategories(ctx context.Context, credentials *application.MarketplaceCredentials) sharedkernel.ResultOf[[]CategoryNode]
}

// TrendyolTaxonomyClient, Trendyol getCategoryTree ucundan tüm ağacı çekip
// düzleştirir (.NET TrendyolMarketplaceTaxonomyClient portu): yol "A > B > C"
// biçiminde kurulur, alt kategorisi olmayan düğümler yapraktır.
type TrendyolTaxonomyClient struct {
	client *Client
}

// NewTaxonomyClient, ortak istemciyle taksonomi istemcisi oluşturur.
func NewTaxonomyClient(client *Client) *TrendyolTaxonomyClient {
	return &TrendyolTaxonomyClient{client: client}
}

// Trendyol kategori ağacı yanıt biçimi (kısmi).
type trendyolCategoryTreeNode struct {
	ID            int64                      `json:"id"`
	Name          *string                    `json:"name"`
	SubCategories []trendyolCategoryTreeNode `json:"subCategories"`
}

type trendyolCategoryTreeResponse struct {
	Categories []trendyolCategoryTreeNode `json:"categories"`
}

// FetchAllCategories, tüm kategori ağacını çekip düzleştirir.
func (c *TrendyolTaxonomyClient) FetchAllCategories(ctx context.Context, credentials *application.MarketplaceCredentials) sharedkernel.ResultOf[[]CategoryNode] {
	var response trendyolCategoryTreeResponse
	if err := c.client.GetJSON(ctx, ClassTaxonomy, "/integration/product/product-categories", credentials, &response); err != nil {
		return sharedkernel.FailOf[[]CategoryNode](err)
	}
	nodes := []CategoryNode{}
	flattenCategories(response.Categories, nil, nil, &nodes)
	return sharedkernel.OkOf(nodes)
}

// flattenCategories, ağacı derinlik öncelikli dolaşıp düz listeye çevirir.
func flattenCategories(categories []trendyolCategoryTreeNode, parentExternalID, parentPath *string, target *[]CategoryNode) {
	for _, category := range categories {
		if category.Name == nil || strings.TrimSpace(*category.Name) == "" {
			continue
		}
		externalID := strconv.FormatInt(category.ID, 10)
		path := *category.Name
		if parentPath != nil {
			path = *parentPath + " > " + *category.Name
		}
		*target = append(*target, CategoryNode{
			ExternalID: externalID, Name: *category.Name,
			ParentExternalID: parentExternalID, Path: path,
			IsLeaf: len(category.SubCategories) == 0,
		})
		flattenCategories(category.SubCategories, &externalID, &path, target)
	}
}

// StubTaxonomyClient, testler ve UseStubTaxonomyClient=true modu için sabit,
// küçük bir kategori ağacı döner.
type StubTaxonomyClient struct{}

// FetchAllCategories, sabit örnek ağacı döner.
func (StubTaxonomyClient) FetchAllCategories(_ context.Context, _ *application.MarketplaceCredentials) sharedkernel.ResultOf[[]CategoryNode] {
	giyim := "1"
	giyimPath := "Giyim"
	return sharedkernel.OkOf([]CategoryNode{
		{ExternalID: "1", Name: "Giyim", Path: "Giyim", IsLeaf: false},
		{ExternalID: "2", Name: "Gömlek", ParentExternalID: &giyim, Path: giyimPath + " > Gömlek", IsLeaf: true},
		{ExternalID: "3", Name: "Tişört", ParentExternalID: &giyim, Path: giyimPath + " > Tişört", IsLeaf: true},
	})
}
