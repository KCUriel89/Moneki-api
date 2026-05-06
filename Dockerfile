# Usa la imagen SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el archivo del proyecto (que está dentro de la carpeta "Moneki api")
COPY "Moneki api/Moneki_api.csproj" "Moneki api/"
RUN dotnet restore "Moneki api/Moneki_api.csproj"

# Copia todo el resto del código fuente a la carpeta correspondiente
COPY . .

# Compila y publica la aplicación
RUN dotnet publish "Moneki api/Moneki_api.csproj" -c Release -o /app/publish

# Usa la imagen runtime más ligera para ejecutar la app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copia los archivos publicados desde la etapa de build
COPY --from=build /app/publish .

# Expone el puerto 80 (Render usará este)
EXPOSE 80

# Comando para iniciar la aplicación
ENTRYPOINT ["dotnet", "Moneki_api.dll"]
