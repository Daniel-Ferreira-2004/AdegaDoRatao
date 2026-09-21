# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AdegaDoRatao.sln ./
COPY src/AdegaDoRatao.Domain/AdegaDoRatao.Domain.csproj src/AdegaDoRatao.Domain/
COPY src/AdegaDoRatao.Application/AdegaDoRatao.Application.csproj src/AdegaDoRatao.Application/
COPY src/AdegaDoRatao.Infrastructure/AdegaDoRatao.Infrastructure.csproj src/AdegaDoRatao.Infrastructure/
COPY src/AdegaDoRatao.API/AdegaDoRatao.API.csproj src/AdegaDoRatao.API/
RUN dotnet restore src/AdegaDoRatao.API/AdegaDoRatao.API.csproj

COPY src/ src/
RUN dotnet publish src/AdegaDoRatao.API/AdegaDoRatao.API.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AdegaDoRatao.API.dll"]
