using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiWebAPI.Data;
using MiWebAPI.Models;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        private readonly ILogger<ProductoController> _log;

        public ProductoController(InventarioData data, ImagenData imagenes, ILogger<ProductoController> log)
        {
            _data = data;
            _imagenes = imagenes;
            _log = log;
        }

        private int? UsuarioActualId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

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

        [Authorize(Roles = Roles.Administrador)]
        [RequestSizeLimit(5 * 1024 * 1024)] // foto (máx. 2 MB) + datos del formulario
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

                if (!decimal.TryParse(form["precio"], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal precio))
                    return Ok(Falla("El precio no es válido."));

                if (!int.TryParse(form["stock"], out int stock))
                    return Ok(Falla("El stock no es válido."));

                int? idUsuario = UsuarioActualId;

                var errorCampos = ValidarCampos(sku, nombre, idCategoria, talla, color, precio, stock, true);
                if (errorCampos != null) return Ok(Falla(errorCampos));

                if (await _data.ObtenerProducto(sku) != null)
                    return Ok(Falla("Ya existe un producto activo con ese código (SKU)."));
                if (await _data.ExisteSku(sku))
                    return Ok(Falla("Ese código (SKU) pertenece a un producto dado de baja. Reactívalo desde Inventario > Dados de baja."));

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
                _log.LogError(ex, "Error al crear el producto");
                return Ok(Falla("No se pudo registrar el producto. Inténtalo de nuevo."));
            }
        }

        [Authorize(Roles = Roles.Administrador)]
        [RequestSizeLimit(5 * 1024 * 1024)]
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

                if (!decimal.TryParse(form["precio"], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal precio))
                    return Ok(Falla("El precio no es válido."));

                if (!int.TryParse(form["stock"], out int stock))
                    return Ok(Falla("El stock no es válido."));

                int? idUsuario = UsuarioActualId;

                var errorCampos = ValidarCampos(sku, nombre, idCategoria, talla, color, precio, stock, false);
                if (errorCampos != null) return Ok(Falla(errorCampos));

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
                    _log.LogError(ex, "Error al guardar la imagen del producto {Sku}", sku);
                    return Ok(Falla("El producto se actualizó, pero no se pudo guardar la imagen. Inténtalo de nuevo."));
                }
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al editar el producto {Sku}", sku);
                return Ok(Falla("No se pudieron guardar los cambios. Inténtalo de nuevo."));
            }
        }

        [Authorize(Roles = Roles.Administrador)]
        [HttpGet("inactivos")]
        public async Task<IActionResult> Inactivos() => Ok(await _data.ListarProductosInactivos());

        [Authorize(Roles = Roles.Administrador)]
        [HttpPost("{sku}/reactivar")]
        public async Task<IActionResult> Reactivar(string sku) => Ok(await _data.ReactivarProducto(sku));

        [Authorize(Roles = Roles.Administrador)]
        [HttpDelete("{sku}")]
        public async Task<IActionResult> Eliminar(string sku) => Ok(await _data.EliminarProducto(sku));

        [Authorize(Roles = Roles.Administrador)]
        [HttpPost("{sku}/movimiento")]
        public async Task<IActionResult> Mover(string sku, [FromBody] MovimientoRequest movimiento)
        {
            movimiento.IdUsuario = UsuarioActualId; // quien hace el movimiento es quien tiene la sesión
            return Ok(await _data.MoverStock(sku, movimiento));
        }

        // ---------- Utilidades ----------

        private static RespuestaOperacion Falla(string mensaje) => new() { NumeroError = 1, Respuesta = mensaje };

        // Reglas de los datos del producto (mismos límites que las columnas de la base de datos).
        private static string? ValidarCampos(string sku, string nombre, int idCategoria, string talla, string color, decimal precio, int stock, bool validarSku)
        {
            if (validarSku)
            {
                if (sku.Length == 0) return "El código (SKU) es obligatorio.";
                if (!System.Text.RegularExpressions.Regex.IsMatch(sku, @"^[A-Za-z0-9._-]{1,30}$"))
                    return "El código (SKU) solo puede tener letras, números, punto, guion y guion bajo (máximo 30).";
            }
            if (nombre.Length == 0) return "El nombre es obligatorio.";
            if (nombre.Length > 150) return "El nombre no puede pasar de 150 caracteres.";
            if (idCategoria <= 0) return "Selecciona una categoría.";
            if (talla.Length > 10) return "La talla no puede pasar de 10 caracteres.";
            if (color.Length > 30) return "El color no puede pasar de 30 caracteres.";
            if (precio <= 0) return "El precio debe ser mayor a cero.";
            if (precio > 99999999m) return "El precio es demasiado alto.";
            if (decimal.Round(precio, 2) != precio) return "El precio solo puede tener dos decimales.";
            if (stock < 0) return "Las unidades no pueden ser negativas.";
            if (stock > 100000) return "Las unidades no pueden pasar de 100,000.";
            return null;
        }

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
            catch (Exception)
            {
                return (null, null, "No se pudo leer la imagen. Intenta con otra.");
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