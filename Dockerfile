FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS builder
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

WORKDIR /app

COPY --from=builder /app/out ./

EXPOSE 80

ENTRYPOINT ["dotnet", "ReporterService.dll"]