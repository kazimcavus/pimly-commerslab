SQLC ?= sqlc
PIMLY_DATABASE_URL ?= postgres://pimly:pimly@localhost:5432/pimly?sslmode=disable

.PHONY: build sqlc vet test test-integration up down migrate tidy run help

help:
	@echo "build            Build the pimly binary into bin/"
	@echo "sqlc             Regenerate type-safe query code"
	@echo "vet              go vet ./..."
	@echo "test             Run unit tests (no Docker)"
	@echo "test-integration Run integration tests (-tags=integration; needs Postgres)"
	@echo "up / down        Start / stop docker-compose dependencies"
	@echo "migrate          Apply global migrations"
	@echo "run              Run the HTTP server"

build:
	go build -o bin/pimly ./cmd/pimly

sqlc:
	$(SQLC) generate

vet:
	go vet ./...

test:
	go test ./...

test-integration:
	go test -tags=integration ./...

up:
	docker compose up -d

down:
	docker compose down

migrate: build
	./bin/pimly migrate

run: build
	./bin/pimly serve

tidy:
	go mod tidy
