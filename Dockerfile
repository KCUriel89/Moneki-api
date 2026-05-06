FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar TODO el contenido del repositorio
COPY . .

# Cambiar al directorio del proyecto
WORKDIR /src/Moneki_api

# Restaurar y publicar
RUN dotnet restore "Moneki_api.csproj"
RUN dotnet publish "Moneki_api.csproj" -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar los archivos publicados (desde la subcarpeta)
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Moneki_api.dll"]
