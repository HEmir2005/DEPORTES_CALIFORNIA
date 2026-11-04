using Microsoft.AspNetCore.Mvc;
using MiWebAPI.Data;
using MiWebAPI.Models;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VentaController : ControllerBase
    {
        private readonly InventarioData _data;
        public VentaController(InventarioData data) => _data = data;

        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] VentaRequest venta) =>
            Ok(await _data.RegistrarVenta(venta));
    }
}
