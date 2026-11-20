namespace MiWebAPI.Models
{
    public class Categoria
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class Producto
    {
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int IdCategoria { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string? Imagen { get; set; }
    }

    // Se recibe como multipart/form-data para poder subir la imagen junto con los datos.
    public class ProductoForm
    {
        public string? Sku { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public int IdCategoria { get; set; }
        public string? Talla { get; set; }
        public string? Color { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int? IdUsuario { get; set; }
        public IFormFile? Imagen { get; set; }

        public string? Validar()
        {
            if (string.IsNullOrWhiteSpace(Sku)) return "El SKU es obligatorio.";
            if (!System.Text.RegularExpressions.Regex.IsMatch(Sku.Trim(), @"^[A-Za-z0-9._-]{1,30}$"))
                return "El SKU solo puede tener letras, números, punto, guion y guion bajo (máximo 30).";
            if (string.IsNullOrWhiteSpace(Nombre)) return "El nombre es obligatorio.";
            if (IdCategoria <= 0) return "Selecciona una categoría.";
            if (Precio <= 0) return "El precio debe ser mayor a cero.";
            if (Stock < 0) return "Las unidades no pueden ser negativas.";
            return null;
        }
    }

    public class Movimiento
    {
        public string Sku { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public int StockResultante { get; set; }
        public string Usuario { get; set; } = string.Empty;
    }

    public class MovimientoRequest
    {
        public string Tipo { get; set; } = string.Empty;   // ENTRADA | SALIDA
        public int Cantidad { get; set; }
        public int? IdUsuario { get; set; }
    }

    public class VentaItem
    {
        public string Sku { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class VentaRequest
    {
        public string MetodoPago { get; set; } = string.Empty; // EFECTIVO | TRANSFERENCIA
        public int? IdUsuario { get; set; }
        public List<VentaItem> Items { get; set; } = new();
    }

    public class DashboardResumen
    {
        public int TotalProductos { get; set; }
        public int StockBajo { get; set; }
        public int VendidosHoy { get; set; }
        public decimal VentasHoyMonto { get; set; }
        public int VentasHoyCantidad { get; set; }
        public List<Movimiento> UltimosMovimientos { get; set; } = new();
    }

    public class VentaLinea
    {
        public string Sku { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class VentaResumen
    {
        public int IdVenta { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public int Articulos { get; set; }
    }

    public class VentaDetalle : VentaResumen
    {
        public List<VentaLinea> Lineas { get; set; } = new();
    }

    public class CambioCredenciales
    {
        public string NombreActual { get; set; } = string.Empty;
        public string? NuevoNombre { get; set; }
        public string Contrasena { get; set; } = string.Empty;
        public string? NuevaContrasena { get; set; }
    }
}
