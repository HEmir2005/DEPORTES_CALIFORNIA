using Microsoft.AspNetCore.Mvc;
using MiWebAPI.Data;
using MiWebAPI.Models;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UsuarioData _usuarioData;

        public AuthController(UsuarioData usuarioData)
        {
            _usuarioData = usuarioData;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Usuario credenciales)
        {
            Usuario resultado = await _usuarioData.Login(credenciales.NombreUsuario ?? "", credenciales.Contrasena ?? "");

            // Si el IdUsuario es 0, las credenciales fallaron
            if (resultado.IdUsuario == 0)
            {
                return Unauthorized(new
                {
                    isSuccess = false,
                    numeroError = 401,
                    respuesta = "Usuario o contraseña incorrectos"
                });
            }

            return Ok(new
            {
                isSuccess = true,
                numeroError = 0,
                respuesta = "Inicio de sesión correcto",
                usuario = new
                {
                    idUsuario = resultado.IdUsuario,
                    nombre = resultado.NombreUsuario,
                    rol = resultado.Rol
                }
            });
        }

        // Cambia el nombre de usuario. La contraseña actual sirve como confirmación.
        [HttpPost("cambiar-usuario")]
        public async Task<IActionResult> CambiarUsuario([FromBody] CambioCredenciales datos)
        {
            if (string.IsNullOrWhiteSpace(datos.NuevoNombre))
                return Ok(Falla("Escribe el nuevo nombre de usuario."));

            Usuario actual = await _usuarioData.Login(datos.NombreActual, datos.Contrasena);
            if (actual.IdUsuario == 0) return Ok(Falla("La contraseña es incorrecta."));

            RespuestaOperacion resultado = await _usuarioData.Editar(new Usuario
            {
                IdUsuario = actual.IdUsuario,
                NombreUsuario = datos.NuevoNombre.Trim(),
                Rol = actual.Rol,
                Contrasena = datos.Contrasena
            });
            return Ok(resultado);
        }

        // Cambia la contraseña verificando primero la actual.
        [HttpPost("cambiar-contrasena")]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambioCredenciales datos)
        {
            if (string.IsNullOrWhiteSpace(datos.NuevaContrasena) || datos.NuevaContrasena.Length < 6)
                return Ok(Falla("La nueva contraseña debe tener al menos 6 caracteres."));

            Usuario actual = await _usuarioData.Login(datos.NombreActual, datos.Contrasena);
            if (actual.IdUsuario == 0) return Ok(Falla("La contraseña actual es incorrecta."));

            RespuestaOperacion resultado = await _usuarioData.Editar(new Usuario
            {
                IdUsuario = actual.IdUsuario,
                NombreUsuario = actual.NombreUsuario,
                Rol = actual.Rol,
                Contrasena = datos.NuevaContrasena
            });
            return Ok(resultado);
        }

        private static RespuestaOperacion Falla(string mensaje) => new() { NumeroError = 1, Respuesta = mensaje };
    }
}
