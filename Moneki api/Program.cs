using Microsoft.EntityFrameworkCore;
using Moneki_api.Services;
using Proyecto_servicio.Helpers;  // ← IMPORTANTE: Agregar este using para EmailService
using Supabase;
using System;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// CONFIGURACIÓN DE SUPABASE
// ========================================

// Obtener las credenciales de Supabase (desde variables de entorno en Render)
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? 
                  builder.Configuration["Supabase:Url"] ??
                  "https://tu-proyecto.supabase.co";

var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? 
                  builder.Configuration["Supabase:Key"] ??
                  "tu-clave-anon-publica";

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

// Tu DatabaseService
builder.Services.AddScoped<DatabaseService>();

// Agregar EmailService
builder.Services.AddScoped<EmailService>();

// Configurar CORS
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

app.UseAuthorization();
app.MapControllers();

// Endpoint de health check para Render
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
