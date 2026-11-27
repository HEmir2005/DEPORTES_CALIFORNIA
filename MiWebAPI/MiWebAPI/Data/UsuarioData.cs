using MiWebAPI.Models;
using MiWebAPI.Seguridad;
using System.Data;
using System.Data.SqlClient;

namespace MiWebAPI.Data
{
    public class UsuarioData
    {
        private readonly string conexion;
        private readonly ILogger<UsuarioData> _log;

        public UsuarioData(IConfiguration configuration, ILogger<UsuarioData> log)
        {
            conexion = configuration.GetConnectionString("CadenaSQL")!;
            _log = log;
        }

        // 1. LOGIN
        // Busca al usuario por nombre y compara la contraseña contra el hash guardado (nunca en texto).
        // Si el usuario todavía tenía la contraseña sin cifrar, aquí mismo se cifra.
        public async Task<Usuario> Login(string nombreUsuario, string contrasena)
        {
            var objeto = new Usuario();
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrEmpty(contrasena)) return objeto;

            using var con = new SqlConnection(conexion);
            await con.OpenAsync();

            string? guardada = null;
            using (var cmd = new SqlCommand(
                "SELECT ID_USUARIO, NOMBRE_USUARIO, ROL, CONTRASEÑA FROM USUARIO WHERE NOMBRE_USUARIO = @nombre AND ACTIVO = 1", con))
            {
                cmd.Parameters.Add("@nombre", SqlDbType.VarChar, 50).Value = nombreUsuario;
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    objeto.IdUsuario = Convert.ToInt32(reader["ID_USUARIO"]);
                    objeto.NombreUsuario = reader["NOMBRE_USUARIO"].ToString();
                    objeto.Rol = reader["ROL"].ToString();
                    guardada = reader["CONTRASEÑA"]?.ToString();
                }
            }

            if (objeto.IdUsuario == 0 || !Contrasenas.Verificar(contrasena, guardada))
                return new Usuario(); // mismo resultado si no existe el usuario o si la contraseña es incorrecta

            if (!Contrasenas.EsHash(guardada))
                await GuardarHash(con, objeto.IdUsuario, Contrasenas.Generar(contrasena));

            objeto.Activo = true;
            return objeto;
        }

        // 2. CREAR
        public async Task<RespuestaOperacion> Crear(Usuario objeto)
        {
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand("AGREGAR_USUARIO", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@NOMBRE_USUARIO", objeto.NombreUsuario);
            cmd.Parameters.AddWithValue("@ROL", objeto.Rol);
            cmd.Parameters.AddWithValue("@CONTRASEÑA", Contrasenas.Generar(objeto.Contrasena ?? ""));
            return await EjecutarConRespuesta(con, cmd, "crear el usuario");
        }

        // 3. EDITAR (la contraseña que llega se guarda cifrada)
        public async Task<RespuestaOperacion> Editar(Usuario objeto)
        {
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand("ACTUALIZAR_USUARIO", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ID_USUARIO", objeto.IdUsuario);
            cmd.Parameters.AddWithValue("@NOMBRE_USUARIO", objeto.NombreUsuario);
            cmd.Parameters.AddWithValue("@ROL", objeto.Rol);
            cmd.Parameters.AddWithValue("@CONTRASEÑA", Contrasenas.Generar(objeto.Contrasena ?? ""));
            return await EjecutarConRespuesta(con, cmd, "actualizar el usuario");
        }

        // 4. ELIMINAR (BAJA LÓGICA)
        public async Task<RespuestaOperacion> Eliminar(int id)
        {
            using var con = new SqlConnection(conexion);
            using var cmd = new SqlCommand("ELIMINAR_USUARIO", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ID_USUARIO", id);
            return await EjecutarConRespuesta(con, cmd, "dar de baja el usuario");
        }

        private static async Task GuardarHash(SqlConnection con, int idUsuario, string hash)
        {
            using var cmd = new SqlCommand("UPDATE USUARIO SET CONTRASEÑA = @hash WHERE ID_USUARIO = @id", con);
            cmd.Parameters.Add("@hash", SqlDbType.VarChar, 255).Value = hash;
            cmd.Parameters.AddWithValue("@id", idUsuario);
            await cmd.ExecuteNonQueryAsync();
        }

        // Ejecuta un procedimiento con @numeroError y @respuesta de salida.
        // Si falla la base de datos, el detalle técnico se guarda en el registro del servidor y al usuario
        // solo se le muestra un mensaje general (así no se revelan nombres de tablas ni datos internos).
        private async Task<RespuestaOperacion> EjecutarConRespuesta(SqlConnection con, SqlCommand cmd, string accion)
        {
            var numeroError = new SqlParameter("@numeroError", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(numeroError);
            cmd.Parameters.Add(respuesta);
            try
            {
                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                int numero = Convert.ToInt32(numeroError.Value);
                return new RespuestaOperacion
                {
                    NumeroError = numero,
                    // Los mensajes propios del procedimiento (0 = éxito, 1 = aviso) se muestran; los de SQL Server no.
                    Respuesta = numero is 0 or 1 ? respuesta.Value?.ToString() ?? "" : $"No se pudo {accion}."
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al {Accion}", accion);
                return new RespuestaOperacion { NumeroError = -1, Respuesta = $"No se pudo {accion}. Inténtalo de nuevo." };
            }
        }
    }
}
