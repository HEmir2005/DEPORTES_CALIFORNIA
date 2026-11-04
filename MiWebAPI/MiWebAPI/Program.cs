using MiWebAPI.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<UsuarioData>();
builder.Services.AddSingleton<InventarioData>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// El frontend (wwwroot) y la API viven en el mismo origen, por eso ya no hace falta CORS abierto.
// La página de inicio es bienvenida.html.
app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new List<string> { "bienvenida.html" } });
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();
