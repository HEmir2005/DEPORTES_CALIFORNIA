using Microsoft.AspNetCore.Mvc;
using MiWebAPI.Data;
using MiWebAPI.Models;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long TamanoMaximo = 2 * 1024 * 1024; // 2 MB

        private readonly InventarioData _data;
        private readonly IWebHostEnvironment _env;

        public ProductoController(InventarioData data, IWebHostEnvironment env)
        {
            _data = data;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Listar() => Ok(await _data.ListarProductos());

        [HttpGet("categorias")]
        public async Task<IActionResult> Categorias() => Ok(await _data.ListarCategorias());

        [HttpGet("{sku}")]
        public async Task<IActionResult> Obtener(string sku)
        {
            var producto = await _data.ObtenerProducto(sku);
            return producto == null
                ? NotFound(new RespuestaOperacion { NumeroError = 404, Respuesta = "Producto no encontrado." })
                : Ok(producto);
        }

        [HttpGet("{sku}/movimientos")]
        public async Task<IActionResult> Movimientos(string sku) => Ok(await _data.ListarMovimientos(sku, 50));

        [HttpPost]
        public async Task<IActionResult> Crear([FromForm] ProductoForm form)
        {
            var errorValidacion = form.Validar();
            if (errorValidacion != null) return Ok(Falla(errorValidacion));

            var (ruta, errorImagen) = await GuardarImagen(form.Imagen);
            if (errorImagen != null) return Ok(Falla(errorImagen));

            var resultado = await _data.CrearProducto(form, ruta);
            if (!resultado.IsSuccess) BorrarImagen(ruta);
            return Ok(resultado);
        }

        [HttpPut("{sku}")]
        public async Task<IActionResult> Editar(string sku, [FromForm] ProductoForm form)
        {
            form.Sku = sku; // el SKU es la clave del producto y no se puede cambiar
            var errorValidacion = form.Validar();
            if (errorValidacion != null) return Ok(Falla(errorValidacion));

            var actual = await _data.ObtenerProducto(sku);
            if (actual == null) return Ok(Falla("Producto no encontrado."));

            var (ruta, errorImagen) = await GuardarImagen(form.Imagen);
            if (errorImagen != null) return Ok(Falla(errorImagen));

            var resultado = await _data.EditarProducto(sku, form, ruta);
            if (!resultado.IsSuccess) BorrarImagen(ruta);
            else if (ruta != null) BorrarImagen(actual.Imagen); // se reemplazó la imagen: se borra la anterior
            return Ok(resultado);
        }

        [HttpDelete("{sku}")]
        public async Task<IActionResult> Eliminar(string sku) => Ok(await _data.EliminarProducto(sku));

        [HttpPost("{sku}/movimiento")]
        public async Task<IActionResult> Mover(string sku, [FromBody] MovimientoRequest movimiento) =>
            Ok(await _data.MoverStock(sku, movimiento));

        // ---------- Utilidades ----------

        private static RespuestaOperacion Falla(string mensaje) => new() { NumeroError = 1, Respuesta = mensaje };

        private async Task<(string? ruta, string? error)> GuardarImagen(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0) return (null, null);

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                return (null, "Formato de imagen no permitido. Usa JPG, PNG o WEBP.");
            if (archivo.Length > TamanoMaximo)
                return (null, "La imagen no puede pesar más de 2 MB.");

            var carpeta = Path.Combine(_env.WebRootPath, "img", "productos");
            Directory.CreateDirectory(carpeta);
            var nombre = $"{Guid.NewGuid():N}{extension}";
            using (var stream = System.IO.File.Create(Path.Combine(carpeta, nombre)))
                await archivo.CopyToAsync(stream);

            return ($"img/productos/{nombre}", null);
        }

        private void BorrarImagen(string? ruta)
        {
            if (string.IsNullOrEmpty(ruta) || !ruta.StartsWith("img/productos/")) return;
            var archivo = Path.Combine(_env.WebRootPath, ruta.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(archivo)) System.IO.File.Delete(archivo);
        }
    }
}
