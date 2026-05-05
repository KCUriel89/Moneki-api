using Microsoft.EntityFrameworkCore;
using Moneki_api.Services;
using Supabase;
using System;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// CONFIGURACIÓN DE SUPABASE
// ========================================

// Obtener las credenciales de Supabase (desde variables de entorno en Render)
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? 
                  builder.Configuration["Supabase:Url"] ??
                  "https://tu-proyecto.supabase.co"; // Reemplaza si no usas variables

var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? 
                  builder.Configuration["Supabase:Key"] ??
                  "tu-clave-anon-publica"; // Reemplaza si no usas variables

// Crear el cliente de Supabase
var supabaseClient = new Client(supabaseUrl, supabaseKey);

// Inicializar Supabase
await supabaseClient.InitializeAsync();

// Registrar Supabase como servicio (Singleton para toda la app)
builder.Services.AddSingleton(supabaseClient);

// ========================================
// TUS SERVICIOS EXISTENTES
// ========================================

// Servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Tu DatabaseService (necesita ser modificado para usar Supabase)
builder.Services.AddScoped<DatabaseService>();

// Agregar EmailService si lo usas
builder.Services.AddScoped<EmailService>();

// Configurar CORS (importante para que tu frontend pueda llamar a la API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========================================
// CONSTRUCCIÓN DE LA APP
// ========================================

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Habilitar CORS (debe ir ANTES de los controladores)
app.UseCors("AllowAll");

// Redirección HTTPS (Render ya maneja SSL, esto es opcional)
// app.UseHttpsRedirection(); // Comenta si da problemas en Render

app.UseAuthorization();
app.MapControllers();

// Endpoint de health check para Render (¡importante!)
app.MapGet("/", () => Results.Ok(new { 
    message = "API Moneki funcionando", 
    status = "ok", 
    timestamp = DateTime.UtcNow 
}));

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));

app.MapGet("/ping", () => Results.Ok(new { 
    message = "pong", 
    timestamp = DateTime.UtcNow 
}));

app.Run();
