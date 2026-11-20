using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
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

            string rol = Roles.Normalizar(resultado.Rol);
            await IniciarSesion(resultado.IdUsuario, resultado.NombreUsuario ?? "", rol);

            return Ok(new
            {
                isSuccess = true,
                numeroError = 0,
                respuesta = "Inicio de sesión correcto",
                usuario = new
                {
                    idUsuario = resultado.IdUsuario,
                    nombre = resultado.NombreUsuario,
                    rol
                }
            });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new RespuestaOperacion { NumeroError = 0, Respuesta = "Sesión cerrada." });
        }

        // Cambia el nombre de usuario. La contraseña actual sirve como confirmación.
        [Authorize]
        [HttpPost("cambiar-usuario")]
        public async Task<IActionResult> CambiarUsuario([FromBody] CambioCredenciales datos)
        {
            string nuevo = (datos.NuevoNombre ?? "").Trim();
            if (nuevo.Length == 0) return Ok(Falla("Escribe el nuevo nombre de usuario."));
            if (nuevo.Length > 50) return Ok(Falla("El nombre de usuario no puede pasar de 50 caracteres."));

            // El usuario sale de la sesión, no de lo que mande el navegador.
            Usuario actual = await _usuarioData.Login(User.Identity?.Name ?? "", datos.Contrasena);
            if (actual.IdUsuario == 0) return Ok(Falla("La contraseña es incorrecta."));

            RespuestaOperacion resultado = await _usuarioData.Editar(new Usuario
            {
                IdUsuario = actual.IdUsuario,
                NombreUsuario = nuevo,
                Rol = actual.Rol,
                Contrasena = datos.Contrasena
            });
            if (resultado.IsSuccess) await IniciarSesion(actual.IdUsuario, nuevo, Roles.Normalizar(actual.Rol));
            return Ok(resultado);
        }

        // Cambia la contraseña verificando primero la actual.
        [Authorize]
        [HttpPost("cambiar-contrasena")]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambioCredenciales datos)
        {
            if (string.IsNullOrWhiteSpace(datos.NuevaContrasena) || datos.NuevaContrasena.Length < 6 || datos.NuevaContrasena.Length > 20)
                return Ok(Falla("La nueva contraseña debe tener entre 6 y 20 caracteres."));

            Usuario actual = await _usuarioData.Login(User.Identity?.Name ?? "", datos.Contrasena);
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

        private async Task IniciarSesion(int id, string nombre, string rol)
        {
            var identidad = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, nombre),
                new Claim(ClaimTypes.Role, rol)
            }, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
        }

        private static RespuestaOperacion Falla(string mensaje) => new() { NumeroError = 1, Respuesta = mensaje };
    }
}
