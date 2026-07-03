FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Pimly.slnx ./
COPY src/SharedKernel/ src/SharedKernel/
COPY src/Pimly.AspNetCore/ src/Pimly.AspNetCore/
COPY src/Pimly.Api/ src/Pimly.Api/
COPY src/Modules/Catalog/Catalog.Domain/ src/Modules/Catalog/Catalog.Domain/
COPY src/Modules/Catalog/Catalog.Application/ src/Modules/Catalog/Catalog.Application/
COPY src/Modules/Catalog/Catalog.Infrastructure/ src/Modules/Catalog/Catalog.Infrastructure/
COPY src/Modules/Catalog/Catalog.Api/ src/Modules/Catalog/Catalog.Api/
COPY src/Modules/Identity/Identity.Domain/ src/Modules/Identity/Identity.Domain/
COPY src/Modules/Identity/Identity.Application/ src/Modules/Identity/Identity.Application/
COPY src/Modules/Identity/Identity.Infrastructure/ src/Modules/Identity/Identity.Infrastructure/
COPY src/Modules/Identity/Identity.Api/ src/Modules/Identity/Identity.Api/
COPY src/Modules/Media/Media.Application/ src/Modules/Media/Media.Application/
COPY src/Modules/Media/Media.Infrastructure/ src/Modules/Media/Media.Infrastructure/
COPY src/Modules/Media/Media.Api/ src/Modules/Media/Media.Api/

RUN dotnet restore src/Pimly.Api/Pimly.Api.csproj
RUN dotnet publish src/Pimly.Api/Pimly.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:7000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 7000

HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:7000/ready || exit 1

ENTRYPOINT ["dotnet", "Pimly.Api.dll"]
