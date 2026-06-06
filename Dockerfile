# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY GLMS.Shared/GLMS.Shared.csproj GLMS.Shared/
COPY EAPD7111_PART2.csproj ./
RUN dotnet restore EAPD7111_PART2.csproj

COPY GLMS.Shared/ GLMS.Shared/
COPY . ./
RUN dotnet publish EAPD7111_PART2.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
RUN mkdir -p wwwroot/uploads/contracts

ENTRYPOINT ["dotnet", "EAPD7111_PART2.dll"]
