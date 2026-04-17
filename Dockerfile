FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DndAppName.slnx ./
COPY src/DndApp.Api/DndApp.Api.csproj src/DndApp.Api/
COPY tools/DndApp.ContentIngestion/DndApp.ContentIngestion.csproj tools/DndApp.ContentIngestion/
COPY tests/DndApp.Api.Tests/DndApp.Api.Tests.csproj tests/DndApp.Api.Tests/
RUN dotnet restore DndAppName.slnx

COPY . .
RUN dotnet publish src/DndApp.Api/DndApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DndApp.Api.dll"]
