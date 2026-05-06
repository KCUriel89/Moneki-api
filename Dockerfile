FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar TODO el repositorio
COPY . .

# Verificar que el archivo existe (debug - opcional)
RUN ls -la /src/Moneki_api/

# Restaurar y publicar especificando la ruta completa
RUN dotnet restore "/src/Moneki_api/Moneki_api.csproj"
RUN dotnet publish "/src/Moneki_api/Moneki_api.csproj" -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar los archivos publicados
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Moneki_api.dll"]
