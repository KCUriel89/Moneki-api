FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo .csproj con el nombre CORRECTO
COPY "Moneki api.csproj" .
RUN dotnet restore "Moneki api.csproj"

# Copiar todo el código
COPY . .

# Publicar la aplicación
RUN dotnet publish "Moneki api.csproj" -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar los archivos publicados
COPY --from=build /app/publish .

# Puerto para Render
EXPOSE 80

# Comando de entrada (con el nombre CORRECTO del DLL)
ENTRYPOINT ["dotnet", "Moneki api.dll"]
