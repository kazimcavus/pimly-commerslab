# Pimly Go backend geliştirme hedefleri.
# Kullanım: make build | make test | make vet | make run-api | make docker-api

BINARIES := pimly-api pimly-outbox-worker pimly-product-imports-worker \
            pimly-taxonomy-sync-worker pimly-listing-sync-worker \
            pimly-product-publications-worker

# Tüm paketleri derler (binary üretmeden hata denetimi için).
build:
	go build ./...

# Tüm binary'leri bin/ altına derler.
binaries:
	@for b in $(BINARIES); do \
		echo "building $$b"; \
		CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o bin/$$b ./cmd/$$b || exit 1; \
	done

# Birim + entegrasyon testleri (entegrasyon testleri Docker ister; yoksa atlanır).
test:
	go test ./...

# Statik analiz.
vet:
	go vet ./...

# API'yi yerel ayarlarla çalıştırır.
run-api:
	go run ./cmd/pimly-api

# Tek bir binary'nin Docker imajını üretir: make docker-api, make docker-outbox-worker ...
docker-%:
	docker build --build-arg BINARY=pimly-$* -t pimly-go-$* .

.PHONY: build binaries test vet run-api
