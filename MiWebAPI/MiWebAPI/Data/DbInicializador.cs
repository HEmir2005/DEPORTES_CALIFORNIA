using System.Data.SqlClient;

namespace MiWebAPI.Data
{
    // Al arrancar, deja la base de datos lista: la crea (solo en SQL Server local), y crea lo que falte
    // (tablas, columnas, procedimientos de usuario y datos iniciales). Es seguro ejecutarlo muchas veces:
    // nunca borra datos y respeta las tablas que ya existan.
    public class DbInicializador
    {
        private readonly string cadena;
        private readonly ILogger<DbInicializador> _log;

        public DbInicializador(IConfiguration configuration, ILogger<DbInicializador> log)
        {
            cadena = configuration.GetConnectionString("CadenaSQL")!;
            _log = log;
        }

        public async Task Ejecutar()
        {
            int numero = 0;
            try
            {
                await CrearBaseSiNoExiste();
                using var con = new SqlConnection(cadena);
                await con.OpenAsync();
                foreach (var lote in Lotes)
                {
                    numero++;
                    using var cmd = new SqlCommand(lote, con) { CommandTimeout = 90 };
                    await cmd.ExecuteNonQueryAsync();
                }
                _log.LogInformation("Base de datos verificada correctamente.");
            }
            catch (Exception ex)
            {
                // No se detiene la aplicación: el error real se muestra al intentar usar la API.
                _log.LogError(ex, "No se pudo preparar la base de datos (paso {Numero}). Revisa CadenaSQL y los permisos.", numero);
            }
        }

        // En SQL Server local crea la base si no existe. En Azure SQL la base se crea en el portal.
        private async Task CrearBaseSiNoExiste()
        {
            var b = new SqlConnectionStringBuilder(cadena);
            var nombre = b.InitialCatalog;
            if (string.IsNullOrWhiteSpace(nombre) || b.DataSource.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                using var prueba = new SqlConnection(cadena);
                await prueba.OpenAsync();
                return; // la base ya existe
            }
            catch (SqlException ex) when (ex.Number == 4060) { /* no existe: se crea abajo */ }

            b.InitialCatalog = "master";
            using var con = new SqlConnection(b.ConnectionString);
            await con.OpenAsync();
            using var cmd = new SqlCommand("IF DB_ID(@n) IS NULL EXEC('CREATE DATABASE [' + REPLACE(@n, ']', ']]') + ']')", con);
            cmd.Parameters.AddWithValue("@n", nombre);
            await cmd.ExecuteNonQueryAsync();
        }

        private static readonly string[] Lotes =
        {
            // ---- Usuarios ----
            """
            IF OBJECT_ID('dbo.USUARIO') IS NULL
            CREATE TABLE dbo.USUARIO (
                ID_USUARIO                INT IDENTITY(1,1) PRIMARY KEY,
                NOMBRE_USUARIO            VARCHAR(50)  NOT NULL,
                ROL                       VARCHAR(255) NOT NULL,
                CONTRASEÑA                VARCHAR(20)  NOT NULL,
                FECHA_ULTIMA_MODIFICACION DATETIME     NOT NULL DEFAULT GETDATE()
            );
            """,
            """
            IF COL_LENGTH('dbo.USUARIO', 'ACTIVO') IS NULL
                ALTER TABLE dbo.USUARIO ADD ACTIVO BIT NOT NULL CONSTRAINT DF_USUARIO_ACTIVO DEFAULT 1;
            """,
            "UPDATE dbo.USUARIO SET ACTIVO = 1 WHERE ACTIVO IS NULL;",
            """
            CREATE OR ALTER PROCEDURE dbo.usp_login_usuario (@NOMBRE_USUARIO VARCHAR(50), @CONTRASEÑA VARCHAR(20))
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT ID_USUARIO, NOMBRE_USUARIO, ROL
                FROM USUARIO
                WHERE NOMBRE_USUARIO = @NOMBRE_USUARIO AND CONTRASEÑA = @CONTRASEÑA AND ACTIVO = 1;
            END
            """,
            """
            CREATE OR ALTER PROCEDURE dbo.AGREGAR_USUARIO (
                @NOMBRE_USUARIO VARCHAR(50), @ROL VARCHAR(255), @CONTRASEÑA VARCHAR(20),
                @numeroError INT OUTPUT, @respuesta VARCHAR(1000) OUTPUT)
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM USUARIO WHERE NOMBRE_USUARIO = @NOMBRE_USUARIO)
                BEGIN
                    SET @numeroError = 1; SET @respuesta = 'Error: El nombre de usuario ya existe.'; RETURN;
                END
                BEGIN TRY
                    INSERT INTO USUARIO (NOMBRE_USUARIO, ROL, CONTRASEÑA, FECHA_ULTIMA_MODIFICACION)
                    VALUES (@NOMBRE_USUARIO, @ROL, @CONTRASEÑA, GETDATE());
                    SET @numeroError = 0; SET @respuesta = 'Usuario registrado exitosamente.';
                END TRY
                BEGIN CATCH
                    SET @numeroError = ERROR_NUMBER(); SET @respuesta = ERROR_MESSAGE();
                END CATCH
            END
            """,
            """
            CREATE OR ALTER PROCEDURE dbo.ACTUALIZAR_USUARIO (
                @ID_USUARIO INT, @NOMBRE_USUARIO VARCHAR(50), @ROL VARCHAR(255), @CONTRASEÑA VARCHAR(20),
                @numeroError INT OUTPUT, @respuesta VARCHAR(1000) OUTPUT)
            AS
            BEGIN
                SET NOCOUNT ON;
                BEGIN TRY
                    UPDATE USUARIO
                    SET NOMBRE_USUARIO = @NOMBRE_USUARIO, ROL = @ROL, CONTRASEÑA = @CONTRASEÑA,
                        FECHA_ULTIMA_MODIFICACION = GETDATE()
                    WHERE ID_USUARIO = @ID_USUARIO;
                    SET @numeroError = 0; SET @respuesta = 'Usuario actualizado exitosamente.';
                END TRY
                BEGIN CATCH
                    SET @numeroError = ERROR_NUMBER(); SET @respuesta = ERROR_MESSAGE();
                END CATCH
            END
            """,
            """
            CREATE OR ALTER PROCEDURE dbo.ELIMINAR_USUARIO (
                @ID_USUARIO INT, @numeroError INT OUTPUT, @respuesta VARCHAR(1000) OUTPUT)
            AS
            BEGIN
                SET NOCOUNT ON;
                BEGIN TRY
                    UPDATE USUARIO SET ACTIVO = 0 WHERE ID_USUARIO = @ID_USUARIO;
                    SET @numeroError = 0; SET @respuesta = 'Usuario dado de baja exitosamente.';
                END TRY
                BEGIN CATCH
                    SET @numeroError = ERROR_NUMBER(); SET @respuesta = ERROR_MESSAGE();
                END CATCH
            END
            """,

            // ---- Inventario y ventas (según el diagrama E-R) ----
            """
            IF OBJECT_ID('dbo.CATEGORIA') IS NULL
            CREATE TABLE dbo.CATEGORIA (
                ID_CATEGORIA              INT IDENTITY(1,1) PRIMARY KEY,
                NOMBRE                    VARCHAR(100) NOT NULL,
                FECHA_ULTIMA_MODIFICACION DATETIME     NOT NULL DEFAULT GETDATE()
            );
            """,
            """
            IF OBJECT_ID('dbo.PRODUCTO') IS NULL
            CREATE TABLE dbo.PRODUCTO (
                SKU_CODIGO                VARCHAR(50)   NOT NULL PRIMARY KEY,
                ID_CATEGORIA              INT           NOT NULL REFERENCES dbo.CATEGORIA(ID_CATEGORIA),
                NOMBRE                    VARCHAR(150)  NOT NULL,
                TALLA                     VARCHAR(10)   NOT NULL DEFAULT '',
                COLOR                     VARCHAR(30)   NOT NULL DEFAULT '',
                PRECIO                    DECIMAL(10,2) NOT NULL CHECK (PRECIO >= 0),
                STOCK                     INT           NOT NULL DEFAULT 0 CHECK (STOCK >= 0),
                IMAGEN_URL                VARCHAR(500)  NULL,
                DESCRIPCION               VARCHAR(MAX)  NULL,
                FECHA_ULTIMA_MODIFICACION DATETIME      NOT NULL DEFAULT GETDATE()
            );
            """,
            """
            IF COL_LENGTH('dbo.PRODUCTO', 'ACTIVO') IS NULL
                ALTER TABLE dbo.PRODUCTO ADD ACTIVO BIT NOT NULL CONSTRAINT DF_PRODUCTO_ACTIVO DEFAULT 1;
            """,
            """
            IF OBJECT_ID('dbo.VENTA') IS NULL
            CREATE TABLE dbo.VENTA (
                ID_VENTA                  INT IDENTITY(1,1) PRIMARY KEY,
                ID_USUARIO                INT           NOT NULL REFERENCES dbo.USUARIO(ID_USUARIO),
                FECHA_VENTA               DATETIME      NOT NULL DEFAULT GETDATE(),
                TOTAL                     DECIMAL(10,2) NOT NULL DEFAULT 0,
                METODO_PAGO               VARCHAR(50)   NOT NULL,
                FECHA_ULTIMA_MODIFICACION DATETIME      NOT NULL DEFAULT GETDATE()
            );
            """,
            """
            IF OBJECT_ID('dbo.DETALLE_VENTA') IS NULL
            CREATE TABLE dbo.DETALLE_VENTA (
                ID_DETALLE                INT IDENTITY(1,1) PRIMARY KEY,
                ID_VENTA                  INT           NOT NULL REFERENCES dbo.VENTA(ID_VENTA),
                SKU_CODIGO                VARCHAR(50)   NOT NULL REFERENCES dbo.PRODUCTO(SKU_CODIGO),
                CANTIDAD                  INT           NOT NULL CHECK (CANTIDAD > 0),
                PRECIO_UNITARIO           DECIMAL(10,2) NOT NULL,
                SUBTOTAL                  DECIMAL(10,2) NOT NULL,
                FECHA_ULTIMA_MODIFICACION DATETIME      NOT NULL DEFAULT GETDATE()
            );
            """,

            // ---- Tablas adicionales del sistema ----
            """
            IF OBJECT_ID('dbo.MOVIMIENTOS') IS NULL
            CREATE TABLE dbo.MOVIMIENTOS (
                ID_MOVIMIENTO    INT IDENTITY(1,1) PRIMARY KEY,
                FECHA            DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
                TIPO             VARCHAR(10)  NOT NULL CHECK (TIPO IN ('VENTA','REGISTRO','ENTRADA','SALIDA')),
                SKU_CODIGO       VARCHAR(50)  NOT NULL,
                CANTIDAD         INT          NOT NULL,
                STOCK_RESULTANTE INT          NOT NULL,
                ID_USUARIO       INT          NULL
            );
            """,
            """
            IF OBJECT_ID('dbo.PRODUCTO_IMAGEN') IS NULL
            CREATE TABLE dbo.PRODUCTO_IMAGEN (
                SKU_CODIGO                VARCHAR(50)    NOT NULL PRIMARY KEY,
                TIPO                      VARCHAR(50)    NOT NULL,
                DATOS                     VARBINARY(MAX) NOT NULL,
                FECHA_ULTIMA_MODIFICACION DATETIME2(0)   NOT NULL DEFAULT SYSUTCDATETIME()
            );
            """,
            // Las imágenes antiguas se guardaban como archivos (img/productos/...) y ya no existen: se limpian esas rutas.
            "UPDATE dbo.PRODUCTO SET IMAGEN_URL = NULL WHERE IMAGEN_URL LIKE 'img/productos/%';",

            // ---- Datos iniciales (solo si las tablas están vacías) ----
            """
            IF NOT EXISTS (SELECT 1 FROM dbo.CATEGORIA)
                INSERT INTO dbo.CATEGORIA (NOMBRE, FECHA_ULTIMA_MODIFICACION)
                VALUES ('Tenis', GETDATE()), ('Ropa', GETDATE()), ('Balones', GETDATE()), ('Accesorios', GETDATE());
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM dbo.USUARIO)
                INSERT INTO dbo.USUARIO (NOMBRE_USUARIO, ROL, CONTRASEÑA, FECHA_ULTIMA_MODIFICACION)
                VALUES ('admin', 'Administrador', 'Admin123*', GETDATE());
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM dbo.PRODUCTO)
            BEGIN
                INSERT INTO dbo.PRODUCTO (SKU_CODIGO, ID_CATEGORIA, NOMBRE, TALLA, COLOR, PRECIO, STOCK, DESCRIPCION, FECHA_ULTIMA_MODIFICACION)
                SELECT v.SKU, c.ID_CATEGORIA, v.NOMBRE, v.TALLA, v.COLOR, v.PRECIO, v.STOCK, v.DESCRIPCION, GETDATE()
                FROM (VALUES
                    ('20000001','Tenis Nike Air','Tenis','10','Gris/Negro',785.00,5,'Tenis deportivos para correr.'),
                    ('30000033','Sudadera Puma','Ropa','M','Gris',250.00,2,'Sudadera cómoda de algodón.'),
                    ('20000003','Balon Adidas','Balones','5','Azul/Negro',180.00,6,'Balón de futbol tamaño 5.'),
                    ('20000095','Gorra New Era','Ropa','Unitalla','Roja',125.00,2,'Gorra ajustable.')
                ) v (SKU, NOMBRE, CATEGORIA, TALLA, COLOR, PRECIO, STOCK, DESCRIPCION)
                INNER JOIN dbo.CATEGORIA c ON c.NOMBRE = v.CATEGORIA;

                INSERT INTO dbo.MOVIMIENTOS (FECHA, TIPO, SKU_CODIGO, CANTIDAD, STOCK_RESULTANTE)
                SELECT SYSUTCDATETIME(), 'REGISTRO', SKU_CODIGO, STOCK, STOCK FROM dbo.PRODUCTO;
            END
            """
        };
    }
}
