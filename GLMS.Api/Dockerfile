# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY GLMS.Shared/GLMS.Shared.csproj GLMS.Shared/
COPY GLMS.Api/GLMS.Api.csproj GLMS.Api/
RUN dotnet restore GLMS.Api/GLMS.Api.csproj

COPY GLMS.Shared/ GLMS.Shared/
COPY GLMS.Api/ GLMS.Api/
RUN dotnet publish GLMS.Api/GLMS.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
RUN mkdir -p wwwroot/uploads/contracts

ENTRYPOINT ["dotnet", "GLMS.Api.dll"]
