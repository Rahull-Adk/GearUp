# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["GearUp.Presentation/GearUp.Presentation.csproj", "GearUp.Presentation/"]
COPY ["GearUp.Application/GearUp.Application.csproj", "GearUp.Application/"]
COPY ["GearUp.Domain/GearUp.Domain.csproj", "GearUp.Domain/"]
COPY ["GearUp.Infrastructure/GearUp.Infrastructure.csproj", "GearUp.Infrastructure/"]

RUN dotnet restore "GearUp.Presentation/GearUp.Presentation.csproj"

# Copy everything else and build
COPY . .

WORKDIR "/src/GearUp.Presentation"
RUN dotnet publish "GearUp.Presentation.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Add non-root user for security
RUN addgroup --system --gid 1001 appuser && \
    adduser --system --uid 1001 --ingroup appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Change ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "GearUp.Presentation.dll"]
