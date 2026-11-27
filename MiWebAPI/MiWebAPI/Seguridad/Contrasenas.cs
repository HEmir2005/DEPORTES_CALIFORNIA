using System.Security.Cryptography;
using System.Text;

namespace MiWebAPI.Seguridad
{
    // Las contraseñas NUNCA se guardan como texto. Se guarda un "hash" PBKDF2-SHA256 con sal aleatoria:
    // aunque alguien robe la tabla USUARIO, no puede saber cuál es la contraseña.
    // Formato guardado: PBKDF2$<iteraciones>$<sal en Base64>$<hash en Base64>  (unos 83 caracteres).
    public static class Contrasenas
    {
        private const string Prefijo = "PBKDF2";
        private const int Iteraciones = 100_000;
        private const int BytesSal = 16;
        private const int BytesHash = 32;

        public static string Generar(string contrasena)
        {
            byte[] sal = RandomNumberGenerator.GetBytes(BytesSal);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, BytesHash);
            return $"{Prefijo}${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
        }

        public static bool EsHash(string? guardada) => guardada != null && guardada.StartsWith(Prefijo + "$", StringComparison.Ordinal);

        public static bool Verificar(string contrasena, string? guardada)
        {
            if (string.IsNullOrEmpty(guardada)) return false;

            // Contraseña antigua todavía sin cifrar: se compara una sola vez y después se cifra (ver UsuarioData.Login).
            // TrimEnd: si la columna era CHAR/NCHAR, SQL Server rellenaba con espacios al final.
            if (!EsHash(guardada))
                return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(contrasena), Encoding.UTF8.GetBytes(guardada.TrimEnd()));

            string[] partes = guardada.Split('$');
            if (partes.Length != 4 || !int.TryParse(partes[1], out int iteraciones) || iteraciones <= 0) return false;
            try
            {
                byte[] sal = Convert.FromBase64String(partes[2]);
                byte[] esperado = Convert.FromBase64String(partes[3]);
                byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
                return CryptographicOperations.FixedTimeEquals(calculado, esperado);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
