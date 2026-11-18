using Microsoft.AspNetCore.Mvc;
using MiWebAPI.Data;
using MiWebAPI.Models;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private static readonly Dictionary<string, string> TiposImagen = new()
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };
        private const long TamanoMaximo = 2 * 1024 * 1024; // 2 MB

        private readonly InventarioData _data;
        private readonly ImagenData _imagenes;

        public ProductoController(InventarioData data, ImagenData imagenes)
        {
            _data = data;
            _imagenes = imagenes;
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

        [HttpGet("{sku}/imagen")]
        public async Task<IActionResult> Imagen(string sku)
        {
            var imagen = await _imagenes.Obtener(sku);
            if (imagen == null) return NotFound();
            Response.Headers.CacheControl = "public, max-age=604800";
            return File(imagen.Value.Datos, imagen.Value.Tipo);
        }

        [HttpGet("{sku}/movimientos")]
        public async Task<IActionResult> Movimientos(string sku) => Ok(await _data.ListarMovimientos(sku, 50));

        [HttpPost]
        public async Task<IActionResult> Crear()
        {
            try
            {
                if (!Request.HasFormContentType)
                    return Ok(Falla("La petición no tiene el formato multipart/form-data correcto."));

                var form = Request.Form;

                string sku = form["sku"].ToString()?.Trim() ?? "";
                string nombre = form["nombre"].ToString()?.Trim() ?? "";
                string descripcion = form["descripcion"].ToString()?.Trim() ?? "";

                if (!int.TryParse(form["idCategoria"], out int idCategoria))
                    return Ok(Falla("La categoría no es válida."));

                string talla = form["talla"].ToString()?.Trim() ?? "";
                string color = form["color"].ToString()?.Trim() ?? "";

                if (!decimal.TryParse(form["precio"], out decimal precio))
                    return Ok(Falla("El precio no es válido."));

                if (!int.TryParse(form["stock"], out int stock))
                    return Ok(Falla("El stock no es válido."));

                int? idUsuario = int.TryParse(form["idUsuario"], out int idU) ? idU : null;

                if (string.IsNullOrEmpty(sku)) return Ok(Falla("El SKU es obligatorio."));
                if (nombre.Length > 150) return Ok(Falla("El nombre no puede pasar de 150 caracteres."));

                if (await _data.ObtenerProducto(sku) != null)
                    return Ok(Falla("Ya existe un producto activo con ese SKU."));

                IFormFile? archivoImagen = form.Files["Imagen"] ?? (form.Files.Count > 0 ? form.Files[0] : null);

                var (datos, tipo, errorImagen) = await LeerImagen(archivoImagen);
                if (errorImagen != null) return Ok(Falla(errorImagen));

                string? url = null;
                if (datos != null)
                {
                    url = UrlImagen(sku);
                    await _imagenes.Guardar(sku, tipo!, datos);
                }

                var productoForm = new ProductoForm
                {
                    Sku = sku,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    IdCategoria = idCategoria,
                    Talla = talla,
                    Color = color,
                    Precio = precio,
                    Stock = stock,
                    IdUsuario = idUsuario
                };

                var resultado = await _data.CrearProducto(productoForm, url);
                if (!resultado.IsSuccess && datos != null)
                {
                    try { await _imagenes.Eliminar(sku); } catch { }
                }
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return Ok(Falla("Error crítico controlado al crear: " + ex.Message));
            }
        }

        [HttpPut("{sku}")]
        public async Task<IActionResult> Editar(string sku)
        {
            try
            {
                if (!Request.HasFormContentType)
                    return Ok(Falla("La petición no tiene el formato multipart/form-data correcto."));

                var form = Request.Form;

                string nombre = form["nombre"].ToString()?.Trim() ?? "";
                string descripcion = form["descripcion"].ToString()?.Trim() ?? "";

                if (!int.TryParse(form["idCategoria"], out int idCategoria))
                    return Ok(Falla("La categoría no es válida."));

                string talla = form["talla"].ToString()?.Trim() ?? "";
                string color = form["color"].ToString()?.Trim() ?? "";

                if (!decimal.TryParse(form["precio"], out decimal precio))
                    return Ok(Falla("El precio no es válido."));

                if (!int.TryParse(form["stock"], out int stock))
                    return Ok(Falla("El stock no es válido."));

                int? idUsuario = int.TryParse(form["idUsuario"], out int idU) ? idU : null;

                if (nombre.Length > 150) return Ok(Falla("El nombre no puede pasar de 150 caracteres."));

                IFormFile? archivoImagen = form.Files["Imagen"] ?? (form.Files.Count > 0 ? form.Files[0] : null);

                var (datos, tipo, errorImagen) = await LeerImagen(archivoImagen);
                if (errorImagen != null) return Ok(Falla(errorImagen));

                string? url = datos != null ? UrlImagen(sku) : null;

                var productoForm = new ProductoForm
                {
                    Sku = sku,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    IdCategoria = idCategoria,
                    Talla = talla,
                    Color = color,
                    Precio = precio,
                    Stock = stock,
                    IdUsuario = idUsuario
                };

                var resultado = await _data.EditarProducto(sku, productoForm, url);
                if (!resultado.IsSuccess || datos == null) return Ok(resultado);

                try
                {
                    await _imagenes.Guardar(sku, tipo!, datos);
                }
                catch (Exception ex)
                {
                    return Ok(Falla("El producto se actualizó, pero falló la imagen: " + ex.Message));
                }
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return Ok(Falla("Error crítico controlado al editar: " + ex.Message));
            }
        }

        [HttpDelete("{sku}")]
        public async Task<IActionResult> Eliminar(string sku) => Ok(await _data.EliminarProducto(sku));

        [HttpPost("{sku}/movimiento")]
        public async Task<IActionResult> Mover(string sku, [FromBody] MovimientoRequest movimiento) =>
            Ok(await _data.MoverStock(sku, movimiento));

        // ---------- Utilidades ----------

        private static RespuestaOperacion Falla(string mensaje) => new() { NumeroError = 1, Respuesta = mensaje };

        private static string UrlImagen(string sku) => $"api/Producto/{Uri.EscapeDataString(sku)}/imagen?v={Guid.NewGuid():N}";

        private static async Task<(byte[]? datos, string? tipo, string? error)> LeerImagen(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0) return (null, null, null);

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!TiposImagen.TryGetValue(extension, out var tipo))
                return (null, null, "Formato de imagen no permitido. Usa JPG, PNG o WEBP.");
            if (archivo.Length > TamanoMaximo)
                return (null, null, "La imagen no puede pesar más de 2 MB.");

            try
            {
                using var memoria = new MemoryStream();
                await archivo.CopyToAsync(memoria);
                var bytes = memoria.ToArray();
                if (!EsImagenValida(bytes, tipo))
                    return (null, null, "El archivo no es una imagen válida.");

                return (bytes, tipo, null);
            }
            catch (Exception ex)
            {
                return (null, null, "Error al procesar el flujo de la imagen: " + ex.Message);
            }
        }

        private static bool EsImagenValida(byte[] d, string tipo) => tipo switch
        {
            "image/jpeg" => d.Length > 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF,
            "image/png" => d.Length > 8 && d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47,
            "image/webp" => d.Length > 12 && d[0] == 0x52 && d[1] == 0x49 && d[2] == 0x46 && d[3] == 0x46
                                           && d[8] == 0x57 && d[9] == 0x45 && d[10] == 0x42 && d[11] == 0x50,
            _ => false
        };
    }
}