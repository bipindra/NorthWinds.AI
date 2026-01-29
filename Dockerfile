# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/Northwind.Portal.Domain/Northwind.Portal.Domain.csproj", "src/Northwind.Portal.Domain/"]
COPY ["src/Northwind.Portal.Data/Northwind.Portal.Data.csproj", "src/Northwind.Portal.Data/"]
COPY ["src/Northwind.Portal.Web/Northwind.Portal.Web.csproj", "src/Northwind.Portal.Web/"]

# Restore dependencies
RUN dotnet restore "src/Northwind.Portal.Web/Northwind.Portal.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Northwind.Portal.Web"
RUN dotnet build "Northwind.Portal.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Northwind.Portal.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check (using wget as curl may not be available)
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Northwind.Portal.Web.dll"]
