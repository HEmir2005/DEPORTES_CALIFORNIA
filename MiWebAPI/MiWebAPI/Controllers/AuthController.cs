using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiWebAPI.Data;
using MiWebAPI.Models;
using MiWebAPI.Seguridad;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const int LargoMaximoContrasena = 64;

        private readonly UsuarioData _usuarioData;
        private readonly ControlIntentos _intentos;

        public AuthController(UsuarioData usuarioData, ControlIntentos intentos)
        {
            _usuarioData = usuarioData;
            _intentos = intentos;
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")] // máximo 10 intentos por minuto desde la misma conexión
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Usuario credenciales)
        {
            string nombre = (credenciales.NombreUsuario ?? "").Trim();
            string contrasena = credenciales.Contrasena ?? "";

            if (nombre.Length == 0 || contrasena.Length == 0)
                return BadRequest(new { isSuccess = false, numeroError = 400, respuesta = "Escribe tu usuario y tu contraseña." });
            if (nombre.Length > 50 || contrasena.Length > LargoMaximoContrasena)
                return Unauthorized(new { isSuccess = false, numeroError = 401, respuesta = "Usuario o contraseña incorrectos." });

            // Cuenta bloqueada por demasiados intentos fallidos.
            if (_intentos.TiempoRestante(nombre) is TimeSpan espera)
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    isSuccess = false,
                    numeroError = 429,
                    respuesta = $"Cuenta bloqueada por demasiados intentos fallidos. Inténtalo de nuevo en {Math.Ceiling(espera.TotalMinutes)} minuto(s)."
                });

            Usuario resultado = await _usuarioData.Login(nombre, contrasena);

            // Si el IdUsuario es 0, las credenciales fallaron
            if (resultado.IdUsuario == 0)
            {
                int restantes = _intentos.RegistrarFallo(nombre);
                string mensaje = restantes == 0
                    ? $"Demasiados intentos fallidos. La cuenta se bloqueó {ControlIntentos.TiempoBloqueo.TotalMinutes} minutos."
                    : restantes <= 2
                        ? $"Usuario o contraseña incorrectos. Te quedan {restantes} intento(s) antes de que se bloquee la cuenta."
                        : "Usuario o contraseña incorrectos.";
                return Unauthorized(new { isSuccess = false, numeroError = 401, respuesta = mensaje });
            }

            _intentos.RegistrarExito(nombre);

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
            if (string.IsNullOrEmpty(datos.Contrasena) || datos.Contrasena.Length > LargoMaximoContrasena)
                return Ok(Falla("La contraseña es incorrecta."));

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
            if (string.IsNullOrWhiteSpace(datos.NuevaContrasena) || datos.NuevaContrasena.Length < 6 || datos.NuevaContrasena.Length > LargoMaximoContrasena)
                return Ok(Falla($"La nueva contraseña debe tener entre 6 y {LargoMaximoContrasena} caracteres."));
            if (string.IsNullOrEmpty(datos.Contrasena) || datos.Contrasena.Length > LargoMaximoContrasena)
                return Ok(Falla("La contraseña actual es incorrecta."));

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
