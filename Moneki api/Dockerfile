# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo del proyecto (nota: el nombre tiene espacios)
COPY "Moneki api.csproj" .
RUN dotnet restore "Moneki api.csproj"

# Copiar todo el código
COPY . .

# Publicar la aplicación
RUN dotnet publish "Moneki api.csproj" -c Release -o /app/publish

# Etapa 2: Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Exponer puerto
EXPOSE 80
EXPOSE 443

# Copiar la aplicación compilada
COPY --from=build /app/publish .

# Variables de entorno
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "Moneki api.dll"]
