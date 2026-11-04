using MiWebAPI.Models;
using System.Data;
using System.Data.SqlClient;

namespace MiWebAPI.Data
{
    public class UsuarioData
    {
        private readonly string conexion;

        public UsuarioData(IConfiguration configuration)
        {
            conexion = configuration.GetConnectionString("CadenaSQL")!; //[cite: 13]
        }
        public async Task<Usuario> Login(string nombreUsuario, string contrasena)
        {
            Usuario objeto = new Usuario();
            using (var con = new SqlConnection(conexion))
            {
                SqlCommand cmd = new SqlCommand("usp_login_usuario", con);
                cmd.Parameters.AddWithValue("@NOMBRE_USUARIO", nombreUsuario);
                cmd.Parameters.AddWithValue("@CONTRASEÑA", contrasena);
                cmd.CommandType = CommandType.StoredProcedure;

                try
                {
                    await con.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            objeto.IdUsuario = Convert.ToInt32(reader["ID_USUARIO"]);
                            objeto.NombreUsuario = reader["NOMBRE_USUARIO"].ToString();
                            objeto.Rol = reader["ROL"].ToString();
                            objeto.Activo = true; // Si pudo hacer login, está activo
                        }
                    }
                }
                catch (Exception)
                {
                    objeto = new Usuario(); // Devuelve un objeto vacío si falla la conexión
                }
            }
            return objeto;
        }

        // 2. MÉTODO CREAR[cite: 13, 14]
        public async Task<RespuestaOperacion> Crear(Usuario objeto)
        {
            using (var con = new SqlConnection(conexion)) //[cite: 13]
            {
                SqlCommand cmd = new SqlCommand("AGREGAR_USUARIO", con); //[cite: 13]
                cmd.Parameters.AddWithValue("@NOMBRE_USUARIO", objeto.NombreUsuario);
                cmd.Parameters.AddWithValue("@ROL", objeto.Rol);
                cmd.Parameters.AddWithValue("@CONTRASEÑA", objeto.Contrasena);

                SqlParameter numeroError = new SqlParameter("@numeroError", SqlDbType.Int) //[cite: 13]
                {
                    Direction = ParameterDirection.Output //[cite: 14]
                };
                SqlParameter respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 1000) //[cite: 14]
                {
                    Direction = ParameterDirection.Output //[cite: 14]
                };

                cmd.Parameters.Add(numeroError); //[cite: 14]
                cmd.Parameters.Add(respuesta); //[cite: 14]
                cmd.CommandType = CommandType.StoredProcedure; //[cite: 14]

                try
                {
                    await con.OpenAsync(); //[cite: 14]
                    await cmd.ExecuteNonQueryAsync(); //[cite: 14]

                    return new RespuestaOperacion //[cite: 14]
                    {
                        NumeroError = Convert.ToInt32(numeroError.Value), //[cite: 14]
                        Respuesta = respuesta.Value?.ToString() ?? string.Empty //[cite: 14]
                    };
                }
                catch (Exception ex) //[cite: 14]
                {
                    return new RespuestaOperacion //[cite: 14]
                    {
                        NumeroError = -1, //[cite: 14]
                        Respuesta = ex.Message //[cite: 14]
                    };
                }
            }
        }

        // 3. MÉTODO EDITAR[cite: 15]
        public async Task<RespuestaOperacion> Editar(Usuario objeto)
        {
            using (var con = new SqlConnection(conexion)) //[cite: 15]
            {
                SqlCommand cmd = new SqlCommand("ACTUALIZAR_USUARIO", con); //[cite: 15]
                cmd.Parameters.AddWithValue("@ID_USUARIO", objeto.IdUsuario);
                cmd.Parameters.AddWithValue("@NOMBRE_USUARIO", objeto.NombreUsuario);
                cmd.Parameters.AddWithValue("@ROL", objeto.Rol);
                cmd.Parameters.AddWithValue("@CONTRASEÑA", objeto.Contrasena);

                SqlParameter numeroError = new SqlParameter("@numeroError", SqlDbType.Int) //[cite: 15]
                {
                    Direction = ParameterDirection.Output //[cite: 15]
                };
                SqlParameter respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 1000) //[cite: 15]
                {
                    Direction = ParameterDirection.Output //[cite: 15]
                };

                cmd.Parameters.Add(numeroError); //[cite: 15]
                cmd.Parameters.Add(respuesta); //[cite: 15]
                cmd.CommandType = CommandType.StoredProcedure; //[cite: 15]

                try
                {
                    await con.OpenAsync(); //[cite: 15]
                    await cmd.ExecuteNonQueryAsync(); //[cite: 15]

                    return new RespuestaOperacion //[cite: 15]
                    {
                        NumeroError = Convert.ToInt32(numeroError.Value), //[cite: 15]
                        Respuesta = respuesta.Value?.ToString() ?? string.Empty //[cite: 15]
                    };
                }
                catch (Exception ex) //[cite: 15]
                {
                    return new RespuestaOperacion //[cite: 15]
                    {
                        NumeroError = -1, //[cite: 15]
                        Respuesta = ex.Message //[cite: 15]
                    };
                }
            }
        }

        // 4. MÉTODO ELIMINAR (BAJA LÓGICA)[cite: 16]
        public async Task<RespuestaOperacion> Eliminar(int id) //[cite: 16]
        {
            using (var con = new SqlConnection(conexion)) //[cite: 16]
            {
                SqlCommand cmd = new SqlCommand("ELIMINAR_USUARIO", con); //[cite: 16]
                cmd.Parameters.AddWithValue("@ID_USUARIO", id);

                SqlParameter numeroError = new SqlParameter("@numeroError", SqlDbType.Int) //[cite: 16]
                {
                    Direction = ParameterDirection.Output //[cite: 16]
                };
                SqlParameter respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 1000) //[cite: 16]
                {
                    Direction = ParameterDirection.Output //[cite: 16]
                };

                cmd.Parameters.Add(numeroError); //[cite: 16]
                cmd.Parameters.Add(respuesta); //[cite: 16]
                cmd.CommandType = CommandType.StoredProcedure; //[cite: 16]

                try
                {
                    await con.OpenAsync(); //[cite: 16]
                    await cmd.ExecuteNonQueryAsync(); //[cite: 16]

                    return new RespuestaOperacion //[cite: 16]
                    {
                        NumeroError = Convert.ToInt32(numeroError.Value), //[cite: 16]
                        Respuesta = respuesta.Value?.ToString() ?? string.Empty //[cite: 16]
                    };
                }
                catch (Exception ex) //[cite: 16]
                {
                    return new RespuestaOperacion //[cite: 16]
                    {
                        NumeroError = -1, //[cite: 16]
                        Respuesta = ex.Message //[cite: 16]
                    };
                }
            }
        }
    }
}