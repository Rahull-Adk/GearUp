FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["GearUp.Presentation/GearUp.Presentation.csproj", "GearUp.Presentation/"]
COPY ["GearUp.Application/GearUp.Application.csproj", "GearUp.Application/"]
COPY ["GearUp.Domain/GearUp.Domain.csproj", "GearUp.Domain/"]
COPY ["GearUp.Infrastructure/GearUp.Infrastructure.csproj", "GearUp.Infrastructure/"]

RUN dotnet restore "GearUp.Presentation/GearUp.Presentation.csproj"
COPY . .

WORKDIR "/src/GearUp.Presentation"
RUN dotnet publish "GearUp.Presentation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GearUp.Presentation.dll"]