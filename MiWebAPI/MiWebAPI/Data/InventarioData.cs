using MiWebAPI.Models;
using System.Data;
using System.Data.SqlClient;

namespace MiWebAPI.Data
{
    // Tablas usadas (según el diagrama E-R): CATEGORIA, PRODUCTO, VENTA, DETALLE_VENTA.
    // MOVIMIENTOS es adicional: guarda el historial de entradas, salidas, ventas y registros.
    // Todas las fechas nuevas se guardan en UTC (así es igual en cualquier computadora y en Azure);
    // el navegador las muestra en la hora local de quien las ve.
    public class InventarioData
    {
        public const int UmbralStockBajo = 5; // Debe coincidir con STOCK_BAJO en wwwroot/js/config.js
        private const string ZonaHoraria = "Central Standard Time (Mexico)"; // para calcular "ventas de hoy"
        private readonly string conexion;

        public InventarioData(IConfiguration configuration)
        {
            conexion = configuration.GetConnectionString("CadenaSQL")!;
        }

        private const string BaseProducto = @"
            SELECT p.SKU_CODIGO, p.NOMBRE, p.DESCRIPCION, p.ID_CATEGORIA, c.NOMBRE,
                   p.TALLA, p.COLOR, p.PRECIO, p.STOCK, p.IMAGEN_URL
            FROM PRODUCTO p
            INNER JOIN CATEGORIA c ON c.ID_CATEGORIA = p.ID_CATEGORIA
            WHERE p.ACTIVO = 1";

        // Se lee con Convert para que funcione aunque SKU o TALLA estén definidos como numéricos.
        private static string Txt(SqlDataReader r, int i) => r.IsDBNull(i) ? "" : Convert.ToString(r.GetValue(i)) ?? "";

        private static Producto LeerProducto(SqlDataReader r) => new()
        {
            Sku = Txt(r, 0),
            Nombre = Txt(r, 1),
            Descripcion = Txt(r, 2),
            IdCategoria = Convert.ToInt32(r.GetValue(3)),
            Categoria = Txt(r, 4),
            Talla = Txt(r, 5),
            Color = Txt(r, 6),
            Precio = Convert.ToDecimal(r.GetValue(7)),
            Stock = Convert.ToInt32(r.GetValue(8)),
            Imagen = Txt(r, 9)
        };

        private static RespuestaOperacion Error(string mensaje, int numero = -1) =>
            new() { NumeroError = numero, Respuesta = mensaje };

        private static object ValorODbNull(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? DBNull.Value : valor.Trim();

        // ---------- Consultas ----------

        public async Task<List<Categoria>> ListarCategorias()
        {
            var lista = new List<Categoria>();
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand("SELECT ID_CATEGORIA, NOMBRE FROM CATEGORIA ORDER BY NOMBRE", con);
            await con.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                lista.Add(new Categoria { IdCategoria = Convert.ToInt32(r.GetValue(0)), Nombre = Txt(r, 1) });
            return lista;
        }

        public async Task<List<Producto>> ListarProductos()
        {
            var lista = new List<Producto>();
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand(BaseProducto + " ORDER BY p.NOMBRE", con);
            await con.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) lista.Add(LeerProducto(r));
            return lista;
        }

        public async Task<Producto?> ObtenerProducto(string sku)
        {
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand(BaseProducto + " AND p.SKU_CODIGO = @sku", con);
            cmd.Parameters.AddWithValue("@sku", sku);
            await con.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? LeerProducto(r) : null;
        }

        public async Task<List<Movimiento>> ListarMovimientos(string? sku, int cantidad)
        {
            var lista = new List<Movimiento>();
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand(@"
                SELECT TOP (@top) m.SKU_CODIGO, m.FECHA, m.TIPO, p.NOMBRE, p.TALLA, m.CANTIDAD, m.STOCK_RESULTANTE
                FROM MOVIMIENTOS m
                INNER JOIN PRODUCTO p ON CAST(p.SKU_CODIGO AS VARCHAR(50)) = m.SKU_CODIGO
                WHERE (@sku IS NULL OR m.SKU_CODIGO = @sku)
                ORDER BY m.FECHA DESC, m.ID_MOVIMIENTO DESC", con);
            cmd.Parameters.AddWithValue("@top", cantidad);
            cmd.Parameters.AddWithValue("@sku", (object?)sku ?? DBNull.Value);
            await con.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                lista.Add(new Movimiento
                {
                    Sku = Txt(r, 0),
                    Fecha = DateTime.SpecifyKind(r.GetDateTime(1), DateTimeKind.Utc), // se envía con "Z" y el navegador la convierte a su hora
                    Tipo = Txt(r, 2),
                    Producto = Txt(r, 3),
                    Talla = Txt(r, 4),
                    Cantidad = Convert.ToInt32(r.GetValue(5)),
                    StockResultante = Convert.ToInt32(r.GetValue(6))
                });
            return lista;
        }

        public async Task<DashboardResumen> ObtenerResumen()
        {
            var resumen = new DashboardResumen();
            using (var con = new SqlConnection(conexion))
            using (var cmd = new SqlCommand($@"
                SELECT ISNULL(SUM(STOCK), 0) FROM PRODUCTO WHERE ACTIVO = 1;
                SELECT COUNT(*) FROM PRODUCTO WHERE ACTIVO = 1 AND STOCK <= @umbral;
                SELECT ISNULL(SUM(CANTIDAD), 0) FROM MOVIMIENTOS
                WHERE TIPO = 'VENTA'
                  AND CAST((FECHA AT TIME ZONE 'UTC' AT TIME ZONE '{ZonaHoraria}') AS date)
                    = CAST((SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE '{ZonaHoraria}') AS date);
                SELECT ISNULL(SUM(TOTAL), 0), COUNT(*) FROM VENTA
                WHERE CAST((FECHA_VENTA AT TIME ZONE 'UTC' AT TIME ZONE '{ZonaHoraria}') AS date)
                    = CAST((SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE '{ZonaHoraria}') AS date);", con))
            {
                cmd.Parameters.AddWithValue("@umbral", UmbralStockBajo);
                await con.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                await r.ReadAsync(); resumen.TotalProductos = Convert.ToInt32(r.GetValue(0));
                await r.NextResultAsync(); await r.ReadAsync(); resumen.StockBajo = Convert.ToInt32(r.GetValue(0));
                await r.NextResultAsync(); await r.ReadAsync(); resumen.VendidosHoy = Convert.ToInt32(r.GetValue(0));
                await r.NextResultAsync(); await r.ReadAsync();
                resumen.VentasHoyMonto = Convert.ToDecimal(r.GetValue(0));
                resumen.VentasHoyCantidad = Convert.ToInt32(r.GetValue(1));
            }
            resumen.UltimosMovimientos = await ListarMovimientos(null, 5);
            return resumen;
        }

        // ---------- Productos ----------

        private static void AgregarParametros(SqlCommand cmd, string sku, ProductoForm f)
        {
            cmd.Parameters.AddWithValue("@sku", sku);
            cmd.Parameters.AddWithValue("@nombre", f.Nombre!.Trim());
            cmd.Parameters.AddWithValue("@desc", ValorODbNull(f.Descripcion));
            cmd.Parameters.AddWithValue("@cat", f.IdCategoria);
            cmd.Parameters.AddWithValue("@talla", (f.Talla ?? "").Trim());
            cmd.Parameters.AddWithValue("@color", (f.Color ?? "").Trim());
            cmd.Parameters.AddWithValue("@precio", f.Precio);
            cmd.Parameters.AddWithValue("@stock", f.Stock);
        }

        private static async Task RegistrarMovimiento(SqlConnection con, SqlTransaction tx,
            string tipo, string sku, int cantidad, int stock, int? idUsuario)
        {
            using var cmd = new SqlCommand(@"
                INSERT INTO MOVIMIENTOS (FECHA, TIPO, SKU_CODIGO, CANTIDAD, STOCK_RESULTANTE, ID_USUARIO)
                VALUES (SYSUTCDATETIME(), @tipo, @sku, @cant, @stock, @idu)", con, tx);
            cmd.Parameters.AddWithValue("@tipo", tipo);
            cmd.Parameters.AddWithValue("@sku", sku);
            cmd.Parameters.AddWithValue("@cant", cantidad);
            cmd.Parameters.AddWithValue("@stock", stock);
            cmd.Parameters.AddWithValue("@idu", (object?)idUsuario ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<RespuestaOperacion> CrearProducto(ProductoForm f, string? imagenUrl)
        {
            try
            {
                string sku = f.Sku!.Trim();
                using var con = new SqlConnection(conexion);
                await con.OpenAsync();
                using var tx = con.BeginTransaction();

                // ¿Ya existe ese SKU? Si fue eliminado antes (ACTIVO = 0) se vuelve a activar con los datos nuevos.
                object? activo;
                using (var existe = new SqlCommand("SELECT ACTIVO FROM PRODUCTO WITH (UPDLOCK) WHERE SKU_CODIGO = @sku", con, tx))
                {
                    existe.Parameters.AddWithValue("@sku", sku);
                    activo = await existe.ExecuteScalarAsync();
                }
                if (activo != null && Convert.ToBoolean(activo)) return Error("Ya existe un producto activo con ese SKU.");

                string sql = activo == null
                    ? @"INSERT INTO PRODUCTO (SKU_CODIGO, NOMBRE, DESCRIPCION, ID_CATEGORIA, TALLA, COLOR, PRECIO, STOCK, IMAGEN_URL, ACTIVO, FECHA_ULTIMA_MODIFICACION)
                        VALUES (@sku, @nombre, @desc, @cat, @talla, @color, @precio, @stock, @img, 1, SYSUTCDATETIME())"
                    : @"UPDATE PRODUCTO SET NOMBRE = @nombre, DESCRIPCION = @desc, ID_CATEGORIA = @cat, TALLA = @talla, COLOR = @color,
                               PRECIO = @precio, STOCK = @stock, IMAGEN_URL = @img, ACTIVO = 1, FECHA_ULTIMA_MODIFICACION = SYSUTCDATETIME()
                        WHERE SKU_CODIGO = @sku";
                using (var cmd = new SqlCommand(sql, con, tx))
                {
                    AgregarParametros(cmd, sku, f);
                    cmd.Parameters.AddWithValue("@img", (object?)imagenUrl ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                await RegistrarMovimiento(con, tx, "REGISTRO", sku, f.Stock, f.Stock, f.IdUsuario);
                tx.Commit();
                return new RespuestaOperacion { NumeroError = 0, Respuesta = "Producto registrado correctamente.", Datos = new { sku } };
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        public async Task<RespuestaOperacion> EditarProducto(string sku, ProductoForm f, string? imagenUrlNueva)
        {
            try
            {
                using var con = new SqlConnection(conexion);
                await con.OpenAsync();
                using var tx = con.BeginTransaction();

                int? stockActual = null;
                using (var sel = new SqlCommand("SELECT STOCK FROM PRODUCTO WITH (UPDLOCK) WHERE SKU_CODIGO = @sku AND ACTIVO = 1", con, tx))
                {
                    sel.Parameters.AddWithValue("@sku", sku);
                    var valor = await sel.ExecuteScalarAsync();
                    if (valor != null) stockActual = Convert.ToInt32(valor);
                }
                if (stockActual == null) return Error("Producto no encontrado.", 404);

                using (var cmd = new SqlCommand(@"
                    UPDATE PRODUCTO
                    SET NOMBRE = @nombre, DESCRIPCION = @desc, ID_CATEGORIA = @cat, TALLA = @talla, COLOR = @color,
                        PRECIO = @precio, STOCK = @stock, IMAGEN_URL = COALESCE(@img, IMAGEN_URL),
                        FECHA_ULTIMA_MODIFICACION = SYSUTCDATETIME()
                    WHERE SKU_CODIGO = @sku", con, tx))
                {
                    AgregarParametros(cmd, sku, f);
                    cmd.Parameters.Add("@img", SqlDbType.VarChar, 500).Value = (object?)imagenUrlNueva ?? DBNull.Value;
                    await cmd.ExecuteNonQueryAsync();
                }

                int diferencia = f.Stock - stockActual.Value;
                if (diferencia != 0)
                    await RegistrarMovimiento(con, tx, diferencia > 0 ? "ENTRADA" : "SALIDA", sku, Math.Abs(diferencia), f.Stock, f.IdUsuario);

                tx.Commit();
                return new RespuestaOperacion { NumeroError = 0, Respuesta = "Producto actualizado correctamente." };
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        public async Task<RespuestaOperacion> EliminarProducto(string sku)
        {
            try
            {
                using var con = new SqlConnection(conexion);
                using var cmd = new SqlCommand(@"UPDATE PRODUCTO SET ACTIVO = 0, FECHA_ULTIMA_MODIFICACION = SYSUTCDATETIME()
                                                 WHERE SKU_CODIGO = @sku AND ACTIVO = 1", con);
                cmd.Parameters.AddWithValue("@sku", sku);
                await con.OpenAsync();
                int filas = await cmd.ExecuteNonQueryAsync();
                return filas > 0
                    ? new RespuestaOperacion { NumeroError = 0, Respuesta = "Producto eliminado correctamente." }
                    : Error("Producto no encontrado.", 404);
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        // ---------- Movimientos manuales de stock (botones ENTRADA / SALIDA) ----------

        public async Task<RespuestaOperacion> MoverStock(string sku, MovimientoRequest m)
        {
            if (m.Tipo != "ENTRADA" && m.Tipo != "SALIDA") return Error("Tipo de movimiento no válido.");
            if (m.Cantidad <= 0) return Error("La cantidad debe ser mayor a cero.");

            try
            {
                using var con = new SqlConnection(conexion);
                await con.OpenAsync();
                using var tx = con.BeginTransaction();

                using var cmd = new SqlCommand(@"
                    UPDATE PRODUCTO SET STOCK = STOCK + @delta, FECHA_ULTIMA_MODIFICACION = SYSUTCDATETIME()
                    OUTPUT INSERTED.STOCK
                    WHERE SKU_CODIGO = @sku AND ACTIVO = 1 AND STOCK + @delta >= 0", con, tx);
                cmd.Parameters.AddWithValue("@delta", m.Tipo == "ENTRADA" ? m.Cantidad : -m.Cantidad);
                cmd.Parameters.AddWithValue("@sku", sku);
                var stock = await cmd.ExecuteScalarAsync();
                if (stock == null) return Error("Producto no encontrado o stock insuficiente para la salida.");

                await RegistrarMovimiento(con, tx, m.Tipo, sku, m.Cantidad, Convert.ToInt32(stock), m.IdUsuario);
                tx.Commit();
                return new RespuestaOperacion { NumeroError = 0, Respuesta = "Movimiento registrado correctamente." };
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        // ---------- Ventas ----------

        public async Task<RespuestaOperacion> RegistrarVenta(VentaRequest v)
        {
            if (v.IdUsuario == null) return Error("No se identificó al usuario. Cierra sesión e inicia de nuevo.");
            if (v.Items == null || v.Items.Count == 0) return Error("El carrito está vacío.");
            if (v.MetodoPago != "EFECTIVO" && v.MetodoPago != "TRANSFERENCIA") return Error("Selecciona un método de pago válido.");
            if (v.Items.Any(i => i.Cantidad <= 0 || string.IsNullOrWhiteSpace(i.Sku))) return Error("Hay un producto o cantidad no válida en el carrito.");

            // Si el mismo producto llega repetido, se suman las cantidades.
            var items = v.Items.GroupBy(i => i.Sku.Trim())
                               .Select(g => new VentaItem { Sku = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                               .ToList();
            try
            {
                using var con = new SqlConnection(conexion);
                await con.OpenAsync();
                using var tx = con.BeginTransaction();

                int idVenta;
                using (var cmd = new SqlCommand(@"
                    INSERT INTO VENTA (ID_USUARIO, FECHA_VENTA, TOTAL, METODO_PAGO, FECHA_ULTIMA_MODIFICACION)
                    OUTPUT INSERTED.ID_VENTA
                    VALUES (@u, SYSUTCDATETIME(), 0, @m, SYSUTCDATETIME())", con, tx))
                {
                    cmd.Parameters.AddWithValue("@u", v.IdUsuario.Value);
                    cmd.Parameters.AddWithValue("@m", v.MetodoPago);
                    idVenta = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                decimal total = 0;
                foreach (var item in items)
                {
                    int? stock = null;
                    decimal precio = 0;
                    // Descuenta el stock de forma atómica; el precio se toma de la base de datos, no del navegador.
                    using (var cmd = new SqlCommand(@"
                        UPDATE PRODUCTO SET STOCK = STOCK - @c, FECHA_ULTIMA_MODIFICACION = SYSUTCDATETIME()
                        OUTPUT INSERTED.STOCK, INSERTED.PRECIO
                        WHERE SKU_CODIGO = @sku AND ACTIVO = 1 AND STOCK >= @c", con, tx))
                    {
                        cmd.Parameters.AddWithValue("@c", item.Cantidad);
                        cmd.Parameters.AddWithValue("@sku", item.Sku);
                        using var r = await cmd.ExecuteReaderAsync();
                        if (await r.ReadAsync()) { stock = Convert.ToInt32(r.GetValue(0)); precio = Convert.ToDecimal(r.GetValue(1)); }
                    }
                    if (stock == null)
                    {
                        tx.Rollback();
                        return Error($"El producto {item.Sku} ya no tiene stock suficiente. Revisa el carrito.");
                    }

                    decimal subtotal = precio * item.Cantidad;
                    using (var det = new SqlCommand(@"
                        INSERT INTO DETALLE_VENTA (ID_VENTA, SKU_CODIGO, CANTIDAD, PRECIO_UNITARIO, SUBTOTAL, FECHA_ULTIMA_MODIFICACION)
                        VALUES (@v, @sku, @c, @pr, @sub, SYSUTCDATETIME())", con, tx))
                    {
                        det.Parameters.AddWithValue("@v", idVenta);
                        det.Parameters.AddWithValue("@sku", item.Sku);
                        det.Parameters.AddWithValue("@c", item.Cantidad);
                        det.Parameters.AddWithValue("@pr", precio);
                        det.Parameters.AddWithValue("@sub", subtotal);
                        await det.ExecuteNonQueryAsync();
                    }

                    await RegistrarMovimiento(con, tx, "VENTA", item.Sku, item.Cantidad, stock.Value, v.IdUsuario);
                    total += subtotal;
                }

                using (var upd = new SqlCommand("UPDATE VENTA SET TOTAL = @t, FECHA_ULTIMA_MODIFICACION = SYSUTCDATETIME() WHERE ID_VENTA = @v", con, tx))
                {
                    upd.Parameters.AddWithValue("@t", total);
                    upd.Parameters.AddWithValue("@v", idVenta);
                    await upd.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return new RespuestaOperacion { NumeroError = 0, Respuesta = "Venta registrada correctamente.", Datos = new { idVenta, total } };
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }
    }
}
