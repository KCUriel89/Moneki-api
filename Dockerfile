FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar TODO el contenido (esto incluye la carpeta Moneki api)
COPY . .

# Cambiar al directorio correcto donde está el .csproj
WORKDIR /src/Moneki\ api

# Restaurar dependencias
RUN dotnet restore "Moneki_api.csproj"

# Publicar la aplicación
RUN dotnet publish "Moneki_api.csproj" -c Release -o /app/publish

# Etapa runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar la aplicación publicada
COPY --from=build /app/publish .

EXPOSE 80

# Comando de entrada
ENTRYPOINT ["dotnet", "Moneki_api.dll"]
