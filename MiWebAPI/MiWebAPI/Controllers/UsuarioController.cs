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
            RespuestaOperacion resultado = await _usuarioData.Crear(objeto); //[cite: 17]
            return StatusCode(StatusCodes.Status200OK, resultado); //[cite: 17]
        }

        [HttpPut]
        public async Task<IActionResult> Editar([FromBody] Usuario objeto) //[cite: 17]
        {
            RespuestaOperacion resultado = await _usuarioData.Editar(objeto); //[cite: 17]
            return StatusCode(StatusCodes.Status200OK, resultado); //[cite: 17]
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id) //[cite: 17]
        {
            RespuestaOperacion resultado = await _usuarioData.Eliminar(id); //[cite: 17]
            return StatusCode(StatusCodes.Status200OK, resultado); //[cite: 17]
        }
    }
}