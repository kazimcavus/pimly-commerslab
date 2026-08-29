// Package trendyol, Trendyol pazaryeri API istemcilerini içerir
// (.NET Channels.Infrastructure/Trendyol karşılığı). Ortak HTTP desteği:
// Basic auth + zorunlu User-Agent ("{sellerId} - SelfIntegration"), 429/5xx'te
// Retry-After duyarlı üstel backoff (5 deneme, 500ms·2ⁿ) ve — .NET'te olmayan
// bir iyileştirme olarak — istemci tarafı token-bucket rate limiter.
//
// Rate limit varsayılanları Trendyol'un dokümante tavanlarının ~%80'idir ve
// yapılandırılabilir; 429 alınırsa kova Retry-After süresince dondurulur.
package trendyol

import (
	"context"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"strconv"
	"strings"
	"sync"
	"time"

	"golang.org/x/time/rate"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

const (
	maxAttempts = 5
	baseDelay   = 500 * time.Millisecond
)

// EndpointClass, rate limit kovalarının anahtarlandığı uç sınıflarıdır.
type EndpointClass string

// Uç sınıfları.
const (
	ClassTaxonomy       EndpointClass = "taxonomy"
	ClassProductsRead   EndpointClass = "products_read"
	ClassProductsWrite  EndpointClass = "products_write"
	ClassPriceInventory EndpointClass = "price_inventory"
)

// RateLimits, sınıf başına dakikadaki istek limitleridir (0 = sınırsız).
type RateLimits struct {
	TaxonomyPerMinute       int
	ProductsReadPerMinute   int
	ProductsWritePerMinute  int
	PriceInventoryPerMinute int
}

// DefaultRateLimits, dokümante tavanların ~%80'i olan güvenli varsayılanlardır.
func DefaultRateLimits() RateLimits {
	return RateLimits{
		TaxonomyPerMinute:       240, // tavan ~50/10s ⇒ 300/dk
		ProductsReadPerMinute:   40,  // tavan ~50/dk
		ProductsWritePerMinute:  30,  // tavan ~40/dk
		PriceInventoryPerMinute: 80,  // tavan ~100/dk
	}
}

// Client, ortak HTTP altyapısını taşıyan Trendyol istemci tabanıdır; tüm uç
// istemcileri bunun üzerinden konuşur, kovalar süreç içinde paylaşılır.
type Client struct {
	httpClient *http.Client
	baseURL    string
	limits     RateLimits

	mu      sync.Mutex
	buckets map[string]*rate.Limiter
	frozen  map[string]time.Time
}

// NewClient, verilen taban URL ve limitlerle istemci oluşturur.
func NewClient(baseURL string, limits RateLimits) *Client {
	return &Client{
		httpClient: &http.Client{Timeout: 30 * time.Second},
		baseURL:    strings.TrimRight(baseURL, "/"),
		limits:     limits,
		buckets:    map[string]*rate.Limiter{},
		frozen:     map[string]time.Time{},
	}
}

// perMinute, sınıfın limitini döner.
func (c *Client) perMinute(class EndpointClass) int {
	switch class {
	case ClassTaxonomy:
		return c.limits.TaxonomyPerMinute
	case ClassProductsRead:
		return c.limits.ProductsReadPerMinute
	case ClassProductsWrite:
		return c.limits.ProductsWritePerMinute
	case ClassPriceInventory:
		return c.limits.PriceInventoryPerMinute
	default:
		return 0
	}
}

// wait, (satıcı, sınıf) kovasında sıra bekler; kova 429 nedeniyle donmuşsa
// çözülene kadar bekler.
func (c *Client) wait(ctx context.Context, sellerID string, class EndpointClass) error {
	perMinute := c.perMinute(class)
	if perMinute <= 0 {
		return nil
	}
	key := sellerID + "|" + string(class)

	c.mu.Lock()
	limiter, ok := c.buckets[key]
	if !ok {
		limiter = rate.NewLimiter(rate.Limit(float64(perMinute)/60.0), max(1, perMinute/10))
		c.buckets[key] = limiter
	}
	frozenUntil := c.frozen[key]
	c.mu.Unlock()

	if until := time.Until(frozenUntil); until > 0 {
		select {
		case <-time.After(until):
		case <-ctx.Done():
			return ctx.Err()
		}
	}
	return limiter.Wait(ctx)
}

// freeze, 429 sonrası kovayı verilen süre boyunca dondurur.
func (c *Client) freeze(sellerID string, class EndpointClass, duration time.Duration) {
	if duration <= 0 {
		return
	}
	key := sellerID + "|" + string(class)
	c.mu.Lock()
	if until := time.Now().Add(duration); until.After(c.frozen[key]) {
		c.frozen[key] = until
	}
	c.mu.Unlock()
}

// applyHeaders, Basic auth ve zorunlu User-Agent başlıklarını hazırlar
// (.NET TrendyolHttpSupport.ApplyHeaders portu).
func applyHeaders(req *http.Request, credentials *application.MarketplaceCredentials) {
	if credentials == nil || strings.TrimSpace(credentials.ApiKey) == "" {
		return
	}
	secret := ""
	if credentials.ApiSecret != nil {
		secret = *credentials.ApiSecret
	}
	token := base64.StdEncoding.EncodeToString([]byte(credentials.ApiKey + ":" + secret))
	req.Header.Set("Authorization", "Basic "+token)

	agent := "pimly - SelfIntegration"
	if credentials.SellerID != nil && strings.TrimSpace(*credentials.SellerID) != "" {
		agent = strings.TrimSpace(*credentials.SellerID) + " - SelfIntegration"
	}
	req.Header.Set("User-Agent", agent)
}

// sellerKey, rate limit kovası için satıcı anahtarını döner.
func sellerKey(credentials *application.MarketplaceCredentials) string {
	if credentials != nil && credentials.SellerID != nil {
		return strings.TrimSpace(*credentials.SellerID)
	}
	return "-"
}

// retryAfter, 429/5xx yanıtındaki Retry-After başlığını çözer; yoksa 0.
func retryAfter(resp *http.Response) time.Duration {
	header := resp.Header.Get("Retry-After")
	if header == "" {
		return 0
	}
	if seconds, err := strconv.Atoi(header); err == nil && seconds > 0 {
		return time.Duration(seconds) * time.Second
	}
	return 0
}

// computeDelay, deneme için bekleme süresini hesaplar: Retry-After varsa o,
// yoksa 500ms·2ⁿ (.NET ComputeDelay portu).
func computeDelay(attempt int, fromHeader time.Duration) time.Duration {
	if fromHeader > 0 {
		return fromHeader
	}
	return baseDelay * time.Duration(1<<(attempt-1))
}

// isTransient, durumun geçici sayılıp sayılmadığını döner (429 veya 5xx).
func isTransient(status int) bool {
	return status == http.StatusTooManyRequests || status >= 500
}

// GetJSON, GET isteği yapar, geçici hatalarda backoff ile tekrar dener ve JSON
// gövdeyi hedefe çözer (.NET GetJsonAsync portu; rate limiter eklidir).
func (c *Client) GetJSON(ctx context.Context, class EndpointClass, path string, credentials *application.MarketplaceCredentials, target any) *sharedkernel.Error {
	return c.sendJSON(ctx, http.MethodGet, class, path, credentials, nil, target)
}

// SendJSON, JSON gövdeli POST/PUT isteği yapar (.NET Post/PutJsonAsync portu).
func (c *Client) SendJSON(ctx context.Context, method string, class EndpointClass, path string, credentials *application.MarketplaceCredentials, body, target any) *sharedkernel.Error {
	return c.sendJSON(ctx, method, class, path, credentials, body, target)
}

// sendJSON, ortak istek döngüsüdür.
func (c *Client) sendJSON(ctx context.Context, method string, class EndpointClass, path string, credentials *application.MarketplaceCredentials, body, target any) *sharedkernel.Error {
	requestURI := c.baseURL + path
	var payload []byte
	if body != nil {
		var err error
		payload, err = json.Marshal(body)
		if err != nil {
			return sharedkernel.NewFailureError("Trendyol request body could not be serialized: " + err.Error())
		}
	}

	for attempt := 1; attempt <= maxAttempts; attempt++ {
		if err := c.wait(ctx, sellerKey(credentials), class); err != nil {
			return sharedkernel.NewFailureError("Trendyol rate limiter wait failed: " + err.Error())
		}

		var bodyReader io.Reader
		if payload != nil {
			bodyReader = strings.NewReader(string(payload))
		}
		req, err := http.NewRequestWithContext(ctx, method, requestURI, bodyReader)
		if err != nil {
			return sharedkernel.NewFailureError("Trendyol request could not be built: " + err.Error())
		}
		if payload != nil {
			req.Header.Set("Content-Type", "application/json")
		}
		applyHeaders(req, credentials)

		resp, err := c.httpClient.Do(req)
		if err != nil {
			if attempt < maxAttempts {
				select {
				case <-time.After(computeDelay(attempt, 0)):
					continue
				case <-ctx.Done():
					return sharedkernel.NewFailureError("Trendyol request cancelled: " + ctx.Err().Error())
				}
			}
			return sharedkernel.NewFailureError("Trendyol request failed: " + err.Error())
		}

		responseBody, readErr := io.ReadAll(io.LimitReader(resp.Body, 8<<20))
		resp.Body.Close()
		if readErr != nil {
			return sharedkernel.NewFailureError("Trendyol response could not be read: " + readErr.Error())
		}

		if isTransient(resp.StatusCode) && attempt < maxAttempts {
			delay := computeDelay(attempt, retryAfter(resp))
			if resp.StatusCode == http.StatusTooManyRequests {
				c.freeze(sellerKey(credentials), class, delay)
			}
			select {
			case <-time.After(delay):
				continue
			case <-ctx.Done():
				return sharedkernel.NewFailureError("Trendyol request cancelled: " + ctx.Err().Error())
			}
		}

		if resp.StatusCode < 200 || resp.StatusCode >= 300 {
			snippet := string(responseBody)
			if len(snippet) > 300 {
				snippet = snippet[:300]
			}
			return sharedkernel.NewFailureError(fmt.Sprintf(
				"Trendyol responded %d for %s: %s", resp.StatusCode, requestURI, snippet))
		}

		if target == nil {
			return nil
		}
		if len(responseBody) == 0 {
			return sharedkernel.NewFailureError("Trendyol response body was empty.")
		}
		if err := json.Unmarshal(responseBody, target); err != nil {
			return sharedkernel.NewFailureError("Trendyol response could not be parsed: " + err.Error())
		}
		return nil
	}
	return sharedkernel.NewFailureError("Trendyol request exhausted retry attempts.")
}
