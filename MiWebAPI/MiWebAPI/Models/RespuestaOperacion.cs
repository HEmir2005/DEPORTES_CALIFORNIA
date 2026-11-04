namespace MiWebAPI.Models
{
    public class RespuestaOperacion
    {
        public int NumeroError { get; set; }
        public string Respuesta { get; set; } = string.Empty;
        public bool IsSuccess => NumeroError == 0;
        // Datos opcionales de la operación (por ejemplo el id del registro creado).
        public object? Datos { get; set; }
    }
}
