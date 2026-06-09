# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar todo el proyecto
COPY . .

# Restaurar dependencias
WORKDIR "/src/Moneki api"
RUN dotnet restore "Moneki_api.csproj"

# Publicar la aplicación
RUN dotnet publish "Moneki_api.csproj" -c Release -o /app/publish

# 🔥 ETAPA RUNTIME CON FUENTES INSTALADAS
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 🔥 INSTALAR FUENTES NECESARIAS PARA PDFSHARPCORE
RUN apt-get update && apt-get install -y \
    fonts-liberation \
    fonts-dejavu-core \
    fonts-freefont-ttf \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Copiar la aplicación publicada
COPY --from=build /app/publish .

# Exponer el puerto
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Ejecutar la aplicación
ENTRYPOINT ["dotnet", "Moneki_api.dll"]
