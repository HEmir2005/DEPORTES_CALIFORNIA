using System.Collections.Concurrent;

namespace MiWebAPI.Seguridad
{
    // Protección contra quien intenta adivinar contraseñas: después de 5 intentos fallidos seguidos
    // la cuenta queda bloqueada 15 minutos. Se guarda en memoria (se reinicia si se reinicia el servidor).
    public class ControlIntentos
    {
        public const int MaxIntentos = 5;
        public static readonly TimeSpan TiempoBloqueo = TimeSpan.FromMinutes(15);

        private readonly ConcurrentDictionary<string, Registro> _registros = new(StringComparer.OrdinalIgnoreCase);

        private sealed class Registro
        {
            public int Fallos;
            public DateTime UltimoFallo;
            public DateTime? BloqueadoHasta;
        }

        // Devuelve cuánto falta para desbloquear la cuenta, o null si no está bloqueada.
        public TimeSpan? TiempoRestante(string usuario)
        {
            if (!_registros.TryGetValue(Clave(usuario), out var r)) return null;
            lock (r)
            {
                if (r.BloqueadoHasta is DateTime hasta && hasta > DateTime.UtcNow) return hasta - DateTime.UtcNow;
                return null;
            }
        }

        // Registra un intento fallido y devuelve cuántos intentos le quedan antes del bloqueo.
        public int RegistrarFallo(string usuario)
        {
            var r = _registros.GetOrAdd(Clave(usuario), _ => new Registro());
            lock (r)
            {
                var ahora = DateTime.UtcNow;
                // Si el bloqueo ya pasó o el último fallo fue hace mucho, se empieza de cero.
                if (r.BloqueadoHasta <= ahora || ahora - r.UltimoFallo > TiempoBloqueo)
                {
                    r.Fallos = 0;
                    r.BloqueadoHasta = null;
                }
                r.Fallos++;
                r.UltimoFallo = ahora;
                if (r.Fallos >= MaxIntentos) r.BloqueadoHasta = ahora + TiempoBloqueo;
                return Math.Max(0, MaxIntentos - r.Fallos);
            }
        }

        public void RegistrarExito(string usuario) => _registros.TryRemove(Clave(usuario), out _);

        private static string Clave(string usuario) => (usuario ?? "").Trim();
    }
}
