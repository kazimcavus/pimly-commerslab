// Package config, tüm binary'lerin paylaştığı yapılandırma modelini ve
// yükleyicisini içerir. Öncelik sırası (yüksekten alçağa):
//
//  1. Ortam değişkenleri — .NET'in çift alt çizgi kuralıyla
//     (ör. Identity__Jwt__Secret, ConnectionStrings__Database); böylece mevcut
//     VPS env dosyaları değişmeden çalışır.
//  2. İsteğe bağlı YAML dosyası (PIMLY_CONFIG ile verilen yol, yoksa ./config.yaml).
//  3. appsettings.json ile birebir aynı varsayılanlar.
//
// Bölüm ve alan adları .NET appsettings bölümlerini aynalar; yeni alan eklerken
// hem struct'a hem Defaults'a eklenmelidir.
package config

import (
	"fmt"
	"os"
	"reflect"
	"strconv"
	"strings"

	"gopkg.in/yaml.v3"
)

// Config, bir Pimly binary'sinin tüm çalışma ayarlarını taşır.
type Config struct {
	// Server, HTTP sunucu ayarlarıdır (yalnızca API ve worker metrik uçları kullanır).
	Server ServerConfig `yaml:"Server"`

	// ConnectionStrings, veritabanı bağlantı dizeleridir (.NET biçiminde anahtar=değer).
	ConnectionStrings ConnectionStringsConfig `yaml:"ConnectionStrings"`

	// Observability, log/metrik/trace ayarlarıdır.
	Observability ObservabilityConfig `yaml:"Observability"`

	// Catalog, katalog modülü ayarlarıdır.
	Catalog CatalogConfig `yaml:"Catalog"`

	// Channels, kanallar (pazaryeri) modülü ayarlarıdır.
	Channels ChannelsConfig `yaml:"Channels"`

	// Identity, kimlik modülü ayarlarıdır.
	Identity IdentityConfig `yaml:"Identity"`

	// Media, medya modülü ayarlarıdır.
	Media MediaConfig `yaml:"Media"`

	// ProductImports, ürün import worker'ının ayarlarıdır.
	ProductImports WorkerQueueConfig `yaml:"ProductImports"`

	// ProductPublications, ürün yayın worker'ının ayarlarıdır.
	ProductPublications WorkerQueueConfig `yaml:"ProductPublications"`

	// ListingSync, listeleme senkron worker'ının ayarlarıdır.
	ListingSync ListingSyncConfig `yaml:"ListingSync"`

	// Outbox, outbox dispatcher ayarlarıdır (Go dönemi eklentisi).
	Outbox OutboxConfig `yaml:"Outbox"`
}

// ServerConfig, HTTP dinleme ayarlarını taşır.
type ServerConfig struct {
	// Addr, dinlenecek adrestir (ör. ":7000"). Yan yana geçiş döneminde Go API
	// farklı bir porta alınabilir.
	Addr string `yaml:"Addr"`
}

// ConnectionStringsConfig, .NET biçimindeki bağlantı dizelerini taşır
// (Host=..;Port=..;Database=..;Username=..;Password=..). pg paketi bunları
// pgx URL'sine çevirir.
type ConnectionStringsConfig struct {
	Database string `yaml:"Database"`
	Identity string `yaml:"Identity"`
}

// ObservabilityConfig, log/metrik/trace ayarlarını taşır.
type ObservabilityConfig struct {
	Enabled                        bool          `yaml:"Enabled"`
	ServiceName                    string        `yaml:"ServiceName"`
	ServiceVersion                 string        `yaml:"ServiceVersion"`
	MetricsPath                    string        `yaml:"MetricsPath"`
	ExcludePathsFromRequestLogging []string      `yaml:"ExcludePathsFromRequestLogging"`
	Tracing                        TracingConfig `yaml:"Tracing"`
}

// TracingConfig, OpenTelemetry izleme ayarlarını taşır.
type TracingConfig struct {
	Enabled       bool    `yaml:"Enabled"`
	OtlpEndpoint  string  `yaml:"OtlpEndpoint"`
	SamplingRatio float64 `yaml:"SamplingRatio"`
}

// CatalogConfig, katalog modülü ayarlarını taşır.
type CatalogConfig struct {
	// AutoMigrate, başlangıçta katalog şeması migration'larının uygulanmasını denetler.
	AutoMigrate bool `yaml:"AutoMigrate"`
}

// ChannelsConfig, kanallar modülü ve Trendyol istemcisi ayarlarını taşır.
type ChannelsConfig struct {
	AutoMigrate               bool                       `yaml:"AutoMigrate"`
	UseStubTaxonomyClient     bool                       `yaml:"UseStubTaxonomyClient"`
	WorkerPollIntervalSeconds int                        `yaml:"WorkerPollIntervalSeconds"`
	TrendyolApiBaseUrl        string                     `yaml:"TrendyolApiBaseUrl"`
	ImportPageSize            int                        `yaml:"ImportPageSize"`
	ImportMaxImagesPerProduct int                        `yaml:"ImportMaxImagesPerProduct"`
	TaxonomySyncSchedule      TaxonomySyncScheduleConfig `yaml:"TaxonomySyncSchedule"`
}

// TaxonomySyncScheduleConfig, zamanlanmış taksonomi senkronunun ayarlarını taşır.
type TaxonomySyncScheduleConfig struct {
	Enabled              bool     `yaml:"Enabled"`
	CheckIntervalSeconds int      `yaml:"CheckIntervalSeconds"`
	TimesUtc             []string `yaml:"TimesUtc"`
}

// IdentityConfig, kimlik modülü ayarlarını taşır.
type IdentityConfig struct {
	AutoMigrate bool      `yaml:"AutoMigrate"`
	Jwt         JwtConfig `yaml:"Jwt"`
}

// JwtConfig, JWT üretim/doğrulama ayarlarını taşır.
type JwtConfig struct {
	// Secret, HS256 imza anahtarıdır; üretimde mutlaka değiştirilmelidir.
	Secret string `yaml:"Secret"`

	// ExpirationHours, token geçerlilik süresidir (saat).
	ExpirationHours int `yaml:"ExpirationHours"`
}

// MediaConfig, medya depolama ayarlarını taşır.
type MediaConfig struct {
	StoragePath      string `yaml:"StoragePath"`
	PublicBaseUrl    string `yaml:"PublicBaseUrl"`
	AllowedUrlPrefix string `yaml:"AllowedUrlPrefix"`
}

// WorkerQueueConfig, kuyruk tabanlı worker'ların ortak ayarlarını taşır.
// TenantIds boş liste = tüm tenant'lar (mevcut .NET davranışıyla aynı).
type WorkerQueueConfig struct {
	PollIntervalSeconds int      `yaml:"PollIntervalSeconds"`
	TenantIds           []string `yaml:"TenantIds"`
}

// ListingSyncConfig, listeleme senkron worker'ının ayarlarını taşır;
// PollIntervalSeconds aynı zamanda debounce penceresidir.
type ListingSyncConfig struct {
	PollIntervalSeconds int      `yaml:"PollIntervalSeconds"`
	TenantIds           []string `yaml:"TenantIds"`
}

// OutboxConfig, outbox dispatcher'ının Go dönemi ayarlarını taşır.
type OutboxConfig struct {
	// PollIntervalSeconds, kuyruk boşken iki tarama arasındaki bekleme süresidir.
	PollIntervalSeconds int `yaml:"PollIntervalSeconds"`

	// BatchSize, tek taramada işlenecek azami mesaj sayısıdır.
	BatchSize int `yaml:"BatchSize"`

	// MaxAttempts, bir mesajın dead-letter sayılmadan önceki azami deneme sayısıdır.
	MaxAttempts int `yaml:"MaxAttempts"`
}

// Defaults, appsettings.json ile birebir aynı varsayılanları döner.
// serviceName, Observability.ServiceName için binary'ye özgü değerdir
// (ör. "pimly-api", "pimly-outbox-worker").
func Defaults(serviceName string) Config {
	return Config{
		Server: ServerConfig{Addr: ":7000"},
		ConnectionStrings: ConnectionStringsConfig{
			Database: "Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly",
			Identity: "Host=localhost;Port=5432;Database=pimly;Username=pimly;Password=pimly",
		},
		Observability: ObservabilityConfig{
			Enabled:        true,
			ServiceName:    serviceName,
			ServiceVersion: "1.0.0",
			MetricsPath:    "/metrics",
			ExcludePathsFromRequestLogging: []string{
				"/healthz", "/ready", "/metrics", "/media",
			},
			Tracing: TracingConfig{
				Enabled:       true,
				OtlpEndpoint:  "http://localhost:4317",
				SamplingRatio: 1.0,
			},
		},
		Catalog: CatalogConfig{AutoMigrate: true},
		Channels: ChannelsConfig{
			AutoMigrate:               true,
			UseStubTaxonomyClient:     false,
			WorkerPollIntervalSeconds: 5,
			TrendyolApiBaseUrl:        "https://apigw.trendyol.com",
			ImportPageSize:            200,
			ImportMaxImagesPerProduct: 8,
			TaxonomySyncSchedule: TaxonomySyncScheduleConfig{
				Enabled:              true,
				CheckIntervalSeconds: 60,
				TimesUtc:             []string{"00:00", "08:00", "16:00"},
			},
		},
		Identity: IdentityConfig{
			AutoMigrate: true,
			Jwt:         JwtConfig{Secret: "change-me-in-production", ExpirationHours: 24},
		},
		Media: MediaConfig{
			StoragePath:      "./storage/media",
			PublicBaseUrl:    "",
			AllowedUrlPrefix: "/media/",
		},
		ProductImports:      WorkerQueueConfig{PollIntervalSeconds: 5},
		ProductPublications: WorkerQueueConfig{PollIntervalSeconds: 5},
		ListingSync:         ListingSyncConfig{PollIntervalSeconds: 30},
		Outbox:              OutboxConfig{PollIntervalSeconds: 5, BatchSize: 50, MaxAttempts: 10},
	}
}

// Load, varsayılanlar → YAML → ortam değişkenleri sırasıyla katmanlanmış
// yapılandırmayı döner. YAML dosyası yoksa sessizce atlanır; bozuksa hata döner.
func Load(serviceName string) (Config, error) {
	cfg := Defaults(serviceName)

	path := os.Getenv("PIMLY_CONFIG")
	if path == "" {
		path = "config.yaml"
	}
	if data, err := os.ReadFile(path); err == nil {
		if err := yaml.Unmarshal(data, &cfg); err != nil {
			return cfg, fmt.Errorf("config: %s çözümlenemedi: %w", path, err)
		}
	}

	if err := applyEnv(&cfg); err != nil {
		return cfg, err
	}
	return cfg, nil
}

// applyEnv, Section__Sub__Key biçimindeki ortam değişkenlerini yansıma ile
// Config alanlarına uygular. Alan adı eşleşmesi büyük/küçük harf duyarsızdır;
// dilim alanları için virgülle ayrılmış değer kabul edilir.
func applyEnv(cfg *Config) error {
	for _, kv := range os.Environ() {
		name, value, _ := strings.Cut(kv, "=")
		if !strings.Contains(name, "__") {
			continue
		}
		segments := strings.Split(name, "__")
		if err := setField(reflect.ValueOf(cfg).Elem(), segments, value); err != nil {
			return fmt.Errorf("config: %s ortam değişkeni uygulanamadı: %w", name, err)
		}
	}
	return nil
}

// setField, segment zincirini izleyerek hedef alanı bulur ve değeri dönüştürüp
// yazar. Zincirdeki bir segment Config'te karşılık bulamazsa değişken Pimly'ye
// ait olmayabilir; sessizce yok sayılır.
func setField(v reflect.Value, segments []string, value string) error {
	if len(segments) == 0 {
		return nil
	}
	t := v.Type()
	for i := 0; i < t.NumField(); i++ {
		if !strings.EqualFold(t.Field(i).Name, segments[0]) {
			continue
		}
		field := v.Field(i)
		if len(segments) > 1 {
			if field.Kind() != reflect.Struct {
				return nil
			}
			return setField(field, segments[1:], value)
		}
		return assign(field, value)
	}
	return nil
}

// assign, dizgi değeri alanın türüne dönüştürüp yazar.
func assign(field reflect.Value, value string) error {
	switch field.Kind() {
	case reflect.String:
		field.SetString(value)
	case reflect.Bool:
		b, err := strconv.ParseBool(value)
		if err != nil {
			return err
		}
		field.SetBool(b)
	case reflect.Int:
		n, err := strconv.Atoi(value)
		if err != nil {
			return err
		}
		field.SetInt(int64(n))
	case reflect.Float64:
		f, err := strconv.ParseFloat(value, 64)
		if err != nil {
			return err
		}
		field.SetFloat(f)
	case reflect.Slice:
		if field.Type().Elem().Kind() != reflect.String {
			return fmt.Errorf("desteklenmeyen dilim türü: %s", field.Type())
		}
		parts := strings.Split(value, ",")
		out := make([]string, 0, len(parts))
		for _, p := range parts {
			if p = strings.TrimSpace(p); p != "" {
				out = append(out, p)
			}
		}
		field.Set(reflect.ValueOf(out))
	default:
		return fmt.Errorf("desteklenmeyen alan türü: %s", field.Kind())
	}
	return nil
}
