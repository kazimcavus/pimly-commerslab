//go:build integration

package server_test

import (
	"bytes"
	"crypto/rand"
	"encoding/hex"
	"mime/multipart"
	"net/http"
	"net/http/httptest"
	"os"
	"testing"
	"time"

	"github.com/kazimcavus/pimly/internal/platform/auth"
	"github.com/kazimcavus/pimly/internal/platform/db/dbtest"
	"github.com/kazimcavus/pimly/internal/platform/flags"
	"github.com/kazimcavus/pimly/internal/platform/provision"
	"github.com/kazimcavus/pimly/internal/platform/storage"
	"github.com/kazimcavus/pimly/internal/server"
)

func storageOrSkip(t *testing.T) *storage.Client {
	t.Helper()
	endpoint := os.Getenv("PIMLY_TEST_S3_ENDPOINT")
	if endpoint == "" {
		endpoint = "localhost:9000"
	}
	buf := make([]byte, 6)
	_, _ = rand.Read(buf)
	bucket := "pimly-test-" + hex.EncodeToString(buf)
	c, err := storage.New(storage.Config{
		Endpoint:      endpoint,
		AccessKey:     "pimly",
		SecretKey:     "pimly-secret",
		Bucket:        bucket,
		UseSSL:        false,
		PublicBaseURL: "http://" + endpoint + "/" + bucket,
	})
	if err != nil {
		t.Skipf("skipping media test: %v", err)
	}
	if err := c.EnsureBucket(t.Context()); err != nil {
		t.Skipf("skipping media test: minio unreachable: %v", err)
	}
	return c
}

func TestMediaUploadAndBulk(t *testing.T) {
	database := dbtest.New(t)
	store := storageOrSkip(t)
	authSvc := auth.NewService(database, "test-secret", time.Hour)
	h := server.New(server.Deps{DB: database, Auth: authSvc, Flags: flags.AlwaysOn{}, Storage: store})

	if _, err := provision.CreateTenant(t.Context(), database, provision.Input{
		Name: "Media Co", OwnerEmail: "m@x.test", OwnerPassword: "pw",
	}); err != nil {
		t.Fatalf("provision: %v", err)
	}
	token, _ := login(t, h, "m@x.test", "pw", "")

	// Enable barcode generation (no silent fallback anymore).
	request(t, h, "PUT", "/settings/barcode", token, map[string]any{"enabled": true, "start": 8440491})

	// Create a product with a known SKU and one variant.
	rec := request(t, h, "POST", "/products:batch", token, map[string]any{
		"group": map[string]any{"group_code": "MED1"},
		"products": []map[string]any{
			{"product_sku": "SKU100", "variants": []map[string]any{{"price": 10, "stock": 1}}},
		},
	})
	var batch batchResp
	mustJSON(t, rec.Body.Bytes(), &batch)
	productID := batch.Products[0].ID

	// --- single upload ---
	rec = uploadFiles(t, h, "POST", "/products/"+productID+"/media", token, "file",
		map[string][]byte{"front.jpg": []byte("img-bytes")}, map[string]string{"alt_text": "Front"})
	if rec.Code != http.StatusCreated {
		t.Fatalf("single upload: code=%d body=%s", rec.Code, rec.Body)
	}
	var media struct {
		URL string `json:"url"`
	}
	mustJSON(t, rec.Body.Bytes(), &media)
	if media.URL == "" {
		t.Fatal("expected media url")
	}

	if n := arrLen(t, h, token, "/products/"+productID+"/media"); n != 1 {
		t.Fatalf("product media = %d, want 1", n)
	}

	// --- bulk upload: filename (sans ext) = product_sku ---
	rec = uploadFiles(t, h, "POST", "/media:bulk", token, "files", map[string][]byte{
		"SKU100.jpg":  []byte("matched"),
		"UNKNOWN.png": []byte("orphan"),
	}, nil)
	if rec.Code != http.StatusOK {
		t.Fatalf("bulk upload: code=%d body=%s", rec.Code, rec.Body)
	}
	var bulk struct {
		Attached []struct {
			Sku string `json:"product_sku"`
		} `json:"attached"`
		Skipped []struct {
			Filename string `json:"filename"`
		} `json:"skipped"`
	}
	mustJSON(t, rec.Body.Bytes(), &bulk)
	if len(bulk.Attached) != 1 || bulk.Attached[0].Sku != "SKU100" {
		t.Fatalf("attached = %+v, want 1 (SKU100)", bulk.Attached)
	}
	if len(bulk.Skipped) != 1 || bulk.Skipped[0].Filename != "UNKNOWN.png" {
		t.Fatalf("skipped = %+v, want 1 (UNKNOWN.png)", bulk.Skipped)
	}

	// Product now has 2 media (single + bulk-matched).
	if n := arrLen(t, h, token, "/products/"+productID+"/media"); n != 2 {
		t.Fatalf("product media after bulk = %d, want 2", n)
	}
}

func uploadFiles(t *testing.T, h http.Handler, method, path, token, field string, files map[string][]byte, formFields map[string]string) *httptest.ResponseRecorder {
	t.Helper()
	var body bytes.Buffer
	mw := multipart.NewWriter(&body)
	for name, content := range files {
		fw, err := mw.CreateFormFile(field, name)
		if err != nil {
			t.Fatal(err)
		}
		if _, err := fw.Write(content); err != nil {
			t.Fatal(err)
		}
	}
	for k, v := range formFields {
		_ = mw.WriteField(k, v)
	}
	if err := mw.Close(); err != nil {
		t.Fatal(err)
	}
	req := httptest.NewRequest(method, path, &body)
	req.Header.Set("Content-Type", mw.FormDataContentType())
	if token != "" {
		req.Header.Set("Authorization", "Bearer "+token)
	}
	rec := httptest.NewRecorder()
	h.ServeHTTP(rec, req)
	return rec
}
