using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using MiWebAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuración propia de cada computadora
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// EVITA QUE KESTREL COLapse AL RECIBIR ARCHIVOS ADJUNTOS O FORMDATA
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB límite seguro
    options.MemoryBufferThreshold = int.MaxValue;
});

// Sesión con cookie (segura, sin JavaScript): el navegador la envía sola en cada petición.
// Si falta la sesión la API responde 401, y si el rol no alcanza, 403.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.Cookie.Name = "deportes_california_sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Strict;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
        opciones.Events.OnRedirectToLogin = contexto => { contexto.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        opciones.Events.OnRedirectToAccessDenied = contexto => { contexto.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<UsuarioData>();
builder.Services.AddSingleton<InventarioData>();
builder.Services.AddSingleton<ImagenData>();
builder.Services.AddSingleton<DbInicializador>();

var app = builder.Build();

// Prepara la base de datos si algo falta.
await app.Services.GetRequiredService<DbInicializador>().Ejecutar();

// Atrapa cualquier error global y lo imprime en la consola del Output para que lo veas
app.UseExceptionHandler(errorApp => errorApp.Run(async contexto =>
{
    var error = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
    contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;

    // Imprime el error exacto en la consola de Visual Studio para diagnóstico
    Console.WriteLine("=== ERROR CRÍTICO CAPTURADO ===");
    Console.WriteLine(error?.ToString());

    await contexto.Response.WriteAsJsonAsync(new
    {
        isSuccess = false,
        numeroError = 500,
        respuesta = "Error crítico del servidor: " + (error?.Message ?? "desconocido")
    });
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new List<string> { "bienvenida.html" } });
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();