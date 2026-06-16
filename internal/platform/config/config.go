// Package config loads pimly configuration from environment variables.
// A .env file in the working directory is loaded automatically (without
// overriding values already present in the environment) for local dev.
package config

import (
	"bufio"
	"fmt"
	"log/slog"
	"os"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	HTTPAddr string

	DatabaseURL string
	DBMaxConns  int32
	DBMinConns  int32

	JWTSecret string
	JWTTTL    time.Duration

	AdminToken string

	S3Endpoint      string
	S3AccessKey     string
	S3SecretKey     string
	S3Bucket        string
	S3UseSSL        bool
	S3PublicBaseURL string

	LogLevel  string
	LogFormat string
}

// Load reads configuration from the environment (after loading .env if present).
func Load() (*Config, error) {
	loadDotEnv(".env")

	c := &Config{
		HTTPAddr:        getEnv("PIMLY_HTTP_ADDR", ":8080"),
		DatabaseURL:     getEnv("PIMLY_DATABASE_URL", "postgres://pimly:pimly@localhost:5432/pimly?sslmode=disable"),
		DBMaxConns:      int32(getEnvInt("PIMLY_DB_MAX_CONNS", 8)),
		DBMinConns:      int32(getEnvInt("PIMLY_DB_MIN_CONNS", 1)),
		JWTSecret:       getEnv("PIMLY_JWT_SECRET", ""),
		JWTTTL:          getEnvDuration("PIMLY_JWT_TTL", 24*time.Hour),
		AdminToken:      getEnv("PIMLY_ADMIN_TOKEN", ""),
		S3Endpoint:      getEnv("PIMLY_S3_ENDPOINT", "localhost:9000"),
		S3AccessKey:     getEnv("PIMLY_S3_ACCESS_KEY", "pimly"),
		S3SecretKey:     getEnv("PIMLY_S3_SECRET_KEY", "pimly-secret"),
		S3Bucket:        getEnv("PIMLY_S3_BUCKET", "pimly-media"),
		S3UseSSL:        getEnvBool("PIMLY_S3_USE_SSL", false),
		S3PublicBaseURL: getEnv("PIMLY_S3_PUBLIC_BASE_URL", "http://localhost:9000/pimly-media"),
		LogLevel:        getEnv("PIMLY_LOG_LEVEL", "info"),
		LogFormat:       getEnv("PIMLY_LOG_FORMAT", "text"),
	}
	if c.DatabaseURL == "" {
		return nil, fmt.Errorf("PIMLY_DATABASE_URL is required")
	}
	return c, nil
}

// NewLogger builds an slog.Logger per the configured level/format.
func (c *Config) NewLogger() *slog.Logger {
	var level slog.Level
	switch strings.ToLower(c.LogLevel) {
	case "debug":
		level = slog.LevelDebug
	case "warn":
		level = slog.LevelWarn
	case "error":
		level = slog.LevelError
	default:
		level = slog.LevelInfo
	}
	opts := &slog.HandlerOptions{Level: level}
	var h slog.Handler
	if strings.ToLower(c.LogFormat) == "json" {
		h = slog.NewJSONHandler(os.Stdout, opts)
	} else {
		h = slog.NewTextHandler(os.Stdout, opts)
	}
	return slog.New(h)
}

func getEnv(key, def string) string {
	if v, ok := os.LookupEnv(key); ok && v != "" {
		return v
	}
	return def
}

func getEnvInt(key string, def int) int {
	if v, ok := os.LookupEnv(key); ok && v != "" {
		if n, err := strconv.Atoi(v); err == nil {
			return n
		}
	}
	return def
}

func getEnvBool(key string, def bool) bool {
	if v, ok := os.LookupEnv(key); ok && v != "" {
		if b, err := strconv.ParseBool(v); err == nil {
			return b
		}
	}
	return def
}

func getEnvDuration(key string, def time.Duration) time.Duration {
	if v, ok := os.LookupEnv(key); ok && v != "" {
		if d, err := time.ParseDuration(v); err == nil {
			return d
		}
	}
	return def
}

// loadDotEnv parses KEY=VALUE lines from path and sets any vars not already in
// the environment. Missing file is a no-op. Minimal parser (no quoting rules
// beyond stripping surrounding quotes).
func loadDotEnv(path string) {
	f, err := os.Open(path)
	if err != nil {
		return
	}
	defer f.Close()
	sc := bufio.NewScanner(f)
	for sc.Scan() {
		line := strings.TrimSpace(sc.Text())
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		key, val, ok := strings.Cut(line, "=")
		if !ok {
			continue
		}
		key = strings.TrimSpace(key)
		val = strings.Trim(strings.TrimSpace(val), `"'`)
		if _, exists := os.LookupEnv(key); !exists {
			_ = os.Setenv(key, val)
		}
	}
}
