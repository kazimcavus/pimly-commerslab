# Tek çok aşamalı Dockerfile; BINARY build-arg'ı hangi cmd/ binary'sinin
# derleneceğini seçer (pimly-api, pimly-outbox-worker, ...).
# Örnek: docker build --build-arg BINARY=pimly-api -t pimly-go-api backend-go/
#
# Çalışma imajı distroless/static'tir: kabuk yok, paket yöneticisi yok,
# ~2 MB taban; CGO kapalı statik Go binary'si doğrudan çalışır.
FROM golang:1.26-alpine AS build
ARG BINARY=pimly-api
WORKDIR /src

# Bağımlılık katmanı ayrı önbelleklenir: go.mod değişmedikçe yeniden inmez.
COPY go.mod go.sum ./
RUN go mod download

COPY . .
RUN CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o /out/app ./cmd/${BINARY}

FROM gcr.io/distroless/static-debian12:nonroot
COPY --from=build /out/app /app
ENTRYPOINT ["/app"]
