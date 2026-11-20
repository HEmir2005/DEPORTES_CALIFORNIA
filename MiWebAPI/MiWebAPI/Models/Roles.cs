namespace MiWebAPI.Models
{
    // Dos roles: el administrador gestiona productos, existencias y ve todas las ventas;
    // el empleado consulta el inventario, registra ventas y ve solo sus ventas.
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Empleado = "Empleado";

        // En la base de datos el rol es texto libre: todo lo que empiece con "admin" cuenta como administrador.
        public static string Normalizar(string? rol) =>
            rol != null && rol.Trim().StartsWith("admin", StringComparison.OrdinalIgnoreCase) ? Administrador : Empleado;
    }
}
