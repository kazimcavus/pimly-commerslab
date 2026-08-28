// Package infrastructure, Media modülünün yerel disk depolamasını içerir
// (.NET LocalBlobStorage karşılığı). Disk düzeni .NET ile birebir aynıdır:
// {tenant:N}/{2hex}/{2hex}/{guid:N}.{ext} — parçalama rastgele GUID'in ilk
// 4 hex karakterinden türetilir; dosya O_EXCL ile açılır (çakışmada üzerine
// yazmak yerine hata verir — FileMode.CreateNew karşılığı).
package infrastructure

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/media/application"
)

// LocalBlobStorage, yerel dosya sistemi üzerinde blob depolamadır.
type LocalBlobStorage struct {
	storagePath string
}

// NewLocalBlobStorage, verilen kök dizinle depoyu oluşturur.
func NewLocalBlobStorage(storagePath string) *LocalBlobStorage {
	return &LocalBlobStorage{storagePath: storagePath}
}

// extensionFor, MIME türünün dosya uzantısını döner.
func extensionFor(contentType string) (string, error) {
	switch contentType {
	case "image/jpeg":
		return ".jpg", nil
	case "image/png":
		return ".png", nil
	case "image/webp":
		return ".webp", nil
	default:
		return "", fmt.Errorf("media: desteklenmeyen içerik türü: %s", contentType)
	}
}

// hexN, UUID'nin tiresiz (N biçimi) gösterimini döner.
func hexN(id uuid.UUID) string { return strings.ReplaceAll(id.String(), "-", "") }

// Save, içeriği tenant'a özel parçalı yola yazar.
func (s *LocalBlobStorage) Save(_ context.Context, content []byte, contentType string, tenantID uuid.UUID) (application.StoredBlob, error) {
	extension, err := extensionFor(contentType)
	if err != nil {
		return application.StoredBlob{}, err
	}
	id := hexN(uuid.New())
	storageKey := fmt.Sprintf("%s/%s/%s/%s%s", hexN(tenantID), id[:2], id[2:4], id, extension)

	absolutePath := filepath.Join(s.storagePath, filepath.FromSlash(storageKey))
	if err := os.MkdirAll(filepath.Dir(absolutePath), 0o755); err != nil {
		return application.StoredBlob{}, fmt.Errorf("media: depolama dizini oluşturulamadı: %w", err)
	}

	file, err := os.OpenFile(absolutePath, os.O_WRONLY|os.O_CREATE|os.O_EXCL, 0o644)
	if err != nil {
		return application.StoredBlob{}, fmt.Errorf("media: dosya oluşturulamadı: %w", err)
	}
	defer file.Close()
	if _, err := file.Write(content); err != nil {
		return application.StoredBlob{}, fmt.Errorf("media: dosya yazılamadı: %w", err)
	}
	return application.StoredBlob{
		StorageKey: storageKey, ContentType: contentType, SizeBytes: int64(len(content))}, nil
}

// Delete, depolama anahtarındaki dosyayı siler; dosya yoksa sessizce geçer.
func (s *LocalBlobStorage) Delete(_ context.Context, storageKey string) error {
	absolutePath := filepath.Join(s.storagePath, filepath.FromSlash(storageKey))
	err := os.Remove(absolutePath)
	if err != nil && !os.IsNotExist(err) {
		return fmt.Errorf("media: dosya silinemedi: %w", err)
	}
	return nil
}
