FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo del proyecto
COPY Moneki_api.csproj .
RUN dotnet restore

# Copiar todo el código
COPY . .

# Publicar la aplicación
RUN dotnet publish -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar los archivos publicados
COPY --from=build /app/publish .

# Puerto para Render
EXPOSE 80

# Comando de entrada
ENTRYPOINT ["dotnet", "Moneki_api.dll"]
