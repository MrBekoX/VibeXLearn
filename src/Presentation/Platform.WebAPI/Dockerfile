# ── Build stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Bağımlılıkları önce kopyala — layer cache'i optimize eder
COPY VibeXLearnPlatform.sln .
COPY src/Core/Platform.Domain/Platform.Domain.csproj src/Core/Platform.Domain/
COPY src/Core/Platform.Application/Platform.Application.csproj src/Core/Platform.Application/
COPY src/Infrastructure/Platform.Persistence/Platform.Persistence.csproj src/Infrastructure/Platform.Persistence/
COPY src/Infrastructure/Platform.Infrastructure/Platform.Infrastructure.csproj src/Infrastructure/Platform.Infrastructure/
COPY src/Infrastructure/Platform.Integration/Platform.Integration.csproj src/Infrastructure/Platform.Integration/
COPY src/Presentation/Platform.WebAPI/Platform.WebAPI.csproj src/Presentation/Platform.WebAPI/

RUN dotnet restore

# Kaynak kodun tamamını kopyala ve publish
COPY . .
RUN dotnet publish src/Presentation/Platform.WebAPI/Platform.WebAPI.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Güvenlik: root olmayan kullanıcı
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget -qO- http://localhost:8080/health/ready || exit 1

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Platform.WebAPI.dll"]
