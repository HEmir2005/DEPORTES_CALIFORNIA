using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiWebAPI.Data;
using MiWebAPI.Models;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Administrador)]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioData _usuarioData;

        public UsuarioController(UsuarioData usuarioData) //[cite: 17]
        {
            _usuarioData = usuarioData; //[cite: 17]
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Usuario objeto) //[cite: 17]
        {
            string? error = Validar(objeto);
            if (error != null) return Ok(new RespuestaOperacion { NumeroError = 1, Respuesta = error });
            RespuestaOperacion resultado = await _usuarioData.Crear(objeto); //[cite: 17]
            return StatusCode(StatusCodes.Status200OK, resultado); //[cite: 17]
        }

        [HttpPut]
        public async Task<IActionResult> Editar([FromBody] Usuario objeto) //[cite: 17]
        {
            string? error = objeto.IdUsuario <= 0 ? "El usuario no es válido." : Validar(objeto);
            if (error != null) return Ok(new RespuestaOperacion { NumeroError = 1, Respuesta = error });
            RespuestaOperacion resultado = await _usuarioData.Editar(objeto); //[cite: 17]
            return StatusCode(StatusCodes.Status200OK, resultado); //[cite: 17]
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id) //[cite: 17]
        {
            // Un administrador no puede darse de baja a sí mismo (se quedaría el sistema sin acceso).
            if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id.ToString())
                return Ok(new RespuestaOperacion { NumeroError = 1, Respuesta = "No puedes dar de baja tu propio usuario." });
            RespuestaOperacion resultado = await _usuarioData.Eliminar(id); //[cite: 17]
            return StatusCode(StatusCodes.Status200OK, resultado); //[cite: 17]
        }

        // Reglas para crear o editar usuarios: nombre, rol permitido y contraseña de 6 a 64 caracteres.
        private static string? Validar(Usuario u)
        {
            u.NombreUsuario = (u.NombreUsuario ?? "").Trim();
            if (u.NombreUsuario.Length == 0 || u.NombreUsuario.Length > 50) return "El nombre de usuario debe tener entre 1 y 50 caracteres.";
            if (u.Rol != Roles.Administrador && u.Rol != Roles.Empleado) return "El rol debe ser Administrador o Empleado.";
            if (string.IsNullOrEmpty(u.Contrasena) || u.Contrasena.Length < 6 || u.Contrasena.Length > 64)
                return "La contraseña debe tener entre 6 y 64 caracteres.";
            return null;
        }
    }
}