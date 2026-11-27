using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using MiWebAPI.Data;
using MiWebAPI.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// Configuración propia de cada computadora
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// Límite de tamaño para lo que se envía al servidor (la foto ya llega reducida a menos de 2 MB).
// Evita que alguien sature el servidor mandando archivos enormes.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // 5 MB
    options.MemoryBufferThreshold = 5 * 1024 * 1024;    // se procesa en memoria (sin archivos temporales en disco)
});

// Sesión con cookie (segura, sin JavaScript): el navegador la envía sola en cada petición.
// Si falta la sesión la API responde 401, y si el rol no alcanza, 403.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.Cookie.Name = "deportes_california_sesion";
        opciones.Cookie.HttpOnly = true;                 // JavaScript no puede leerla (protege contra robo de sesión)
        opciones.Cookie.SameSite = SameSiteMode.Strict;  // otras páginas no pueden usarla (protege contra CSRF)
        // En Azure la cookie solo viaja por HTTPS; en la computadora local también funciona con http.
        opciones.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
        opciones.Events.OnRedirectToLogin = contexto => { contexto.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        opciones.Events.OnRedirectToAccessDenied = contexto => { contexto.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

// Límite de intentos de inicio de sesión: 10 por minuto desde la misma conexión.
// Junto con el bloqueo de cuenta (ControlIntentos) frena a quien intenta adivinar contraseñas.
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.OnRejected = async (contexto, cancelar) =>
        await contexto.HttpContext.Response.WriteAsJsonAsync(new
        {
            isSuccess = false,
            numeroError = 429,
            respuesta = "Demasiados intentos. Espera un minuto e inténtalo de nuevo."
        }, cancelar);
    opciones.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<UsuarioData>();
builder.Services.AddSingleton<InventarioData>();
builder.Services.AddSingleton<ImagenData>();
builder.Services.AddSingleton<DbInicializador>();
builder.Services.AddSingleton<ControlIntentos>();

var app = builder.Build();

// Prepara la base de datos si algo falta.
await app.Services.GetRequiredService<DbInicializador>().Ejecutar();

// Atrapa cualquier error no controlado. El detalle técnico se guarda en el registro del servidor
// (consola de Visual Studio o "Secuencia de registro" en Azure); al navegador solo se le manda un
// mensaje general, para no revelar nombres de tablas, rutas ni datos internos.
app.UseExceptionHandler(errorApp => errorApp.Run(async contexto =>
{
    var error = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(error, "Error no controlado en {Ruta}", contexto.Request.Path);
    contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;

    await contexto.Response.WriteAsJsonAsync(new
    {
        isSuccess = false,
        numeroError = 500,
        respuesta = app.Environment.IsDevelopment()
            ? "Error del servidor: " + (error?.Message ?? "desconocido")
            : "Ocurrió un error en el servidor. Inténtalo de nuevo en unos minutos."
    });
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // En Azure: obliga a usar HTTPS (todo viaja cifrado).
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Cabeceras de seguridad para todas las respuestas.
app.Use(async (contexto, siguiente) =>
{
    var h = contexto.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";                // el navegador no "adivina" el tipo de archivo
    h["X-Frame-Options"] = "DENY";                          // la página no se puede incrustar en otro sitio (clickjacking)
    h["Referrer-Policy"] = "no-referrer";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // Solo se permite cargar código de este sitio y de Bootstrap (cdn.jsdelivr.net). Bloquea scripts inyectados (XSS).
    if (!contexto.Request.Path.StartsWithSegments("/swagger"))
        h["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "font-src 'self' https://cdn.jsdelivr.net; " +
            "img-src 'self' data: blob:; " +
            "connect-src 'self'; " +
            "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    await siguiente();
});

app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new List<string> { "bienvenida.html" } });
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
