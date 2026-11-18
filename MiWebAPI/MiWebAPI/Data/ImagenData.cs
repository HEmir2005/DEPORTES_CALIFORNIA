using System.Data;
using System.Data.SqlClient;

namespace MiWebAPI.Data
{
    // Guarda las imágenes de los productos DENTRO de la base de datos (tabla PRODUCTO_IMAGEN).
    // Así funcionan igual en cualquier computadora y en Azure, sin depender de carpetas del servidor.
    public class ImagenData
    {
        private readonly string conexion;

        public ImagenData(IConfiguration configuration)
        {
            conexion = configuration.GetConnectionString("CadenaSQL")!;
        }

        public async Task Guardar(string sku, string tipo, byte[] datos)
        {
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand(@"
                UPDATE PRODUCTO_IMAGEN SET TIPO = @tipo, DATOS = @datos, FECHA_ULTIMA_MODIFICACION = SYSUTCDATETIME()
                WHERE SKU_CODIGO = @sku;
                IF @@ROWCOUNT = 0
                    INSERT INTO PRODUCTO_IMAGEN (SKU_CODIGO, TIPO, DATOS, FECHA_ULTIMA_MODIFICACION)
                    VALUES (@sku, @tipo, @datos, SYSUTCDATETIME());", con);
            cmd.Parameters.AddWithValue("@sku", sku);
            cmd.Parameters.AddWithValue("@tipo", tipo);
            cmd.Parameters.Add("@datos", SqlDbType.VarBinary, -1).Value = datos;
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<(byte[] Datos, string Tipo)?> Obtener(string sku)
        {
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand("SELECT TIPO, DATOS FROM PRODUCTO_IMAGEN WHERE SKU_CODIGO = @sku", con);
            cmd.Parameters.AddWithValue("@sku", sku);
            await con.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            return ((byte[])r.GetValue(1), r.GetString(0));
        }

        public async Task Eliminar(string sku)
        {
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand("DELETE FROM PRODUCTO_IMAGEN WHERE SKU_CODIGO = @sku", con);
            cmd.Parameters.AddWithValue("@sku", sku);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
