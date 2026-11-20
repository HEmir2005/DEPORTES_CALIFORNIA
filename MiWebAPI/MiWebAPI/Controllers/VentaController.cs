using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiWebAPI.Data;
using MiWebAPI.Models;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VentaController : ControllerBase
    {
        private readonly InventarioData _data;
        public VentaController(InventarioData data) => _data = data;

        private int? UsuarioActualId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        // El administrador ve todas las ventas; el empleado, solo las suyas.
        private int? FiltroUsuario => User.IsInRole(Roles.Administrador) ? null : (UsuarioActualId ?? -1);

        [HttpGet]
        public async Task<IActionResult> Listar() => Ok(await _data.ListarVentas(FiltroUsuario));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Obtener(int id)
        {
            var venta = await _data.ObtenerVenta(id, FiltroUsuario);
            return venta == null
                ? NotFound(new RespuestaOperacion { NumeroError = 404, Respuesta = "Venta no encontrada." })
                : Ok(venta);
        }

        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] VentaRequest venta)
        {
            venta.IdUsuario = UsuarioActualId; // el vendedor es quien tiene la sesión, no lo que mande el navegador
            return Ok(await _data.RegistrarVenta(venta));
        }
    }
}
