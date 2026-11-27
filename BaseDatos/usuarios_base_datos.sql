/* =====================================================================================
   DEPORTES CALIFORNIA - Usuarios de la base de datos (seguridad)
   -------------------------------------------------------------------------------------
   Se crean DOS usuarios distintos:

   1) dc_admin  -> ADMINISTRADOR de la base de datos. Tiene TODOS los privilegios (db_owner):
                   crear, modificar y borrar tablas, procedimientos y datos.
                   Úsenlo SOLO ustedes desde Azure Data Studio / SSMS / Editor de consultas de Azure.
                   NUNCA se pone en la aplicación ni en GitHub.

   2) dc_app    -> Usuario de la APLICACIÓN (la página web). Permisos mínimos:
                   puede leer, agregar y modificar datos y ejecutar procedimientos,
                   pero NO puede borrar tablas, cambiar la estructura ni borrar registros
                   (solo las fotos de productos, que la página reemplaza).
                   Así, aunque alguien lograra entrar a la aplicación, no podría borrar la base de datos.

   ANTES DE EJECUTAR: cambia las dos contraseñas marcadas con <<...>>.
   Requisitos de Azure: mínimo 8 caracteres con mayúsculas, minúsculas, números y símbolos.
   Guárdalas en un lugar seguro (no en el código).
   ===================================================================================== */


/* =====================================================================================
   OPCIÓN A: AZURE SQL  (la base de datos del sitio publicado)
   Dónde: Portal de Azure > Base de datos SQL "DeportesCalifornia" > Editor de consultas,
          iniciando sesión con el administrador del servidor.
   Se ejecuta DENTRO de la base DeportesCalifornia (no en master).
   ===================================================================================== */

-- 1) Administrador de la base de datos (todos los privilegios)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'dc_admin')
    CREATE USER dc_admin WITH PASSWORD = '<<CONTRASEÑA_SEGURA_ADMIN>>';
ALTER ROLE db_owner ADD MEMBER dc_admin;
GO

-- 2) Usuario de la aplicación (permisos mínimos)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'dc_app')
    CREATE USER dc_app WITH PASSWORD = '<<CONTRASEÑA_SEGURA_APP>>';
ALTER ROLE db_datareader ADD MEMBER dc_app;   -- leer datos
ALTER ROLE db_datawriter ADD MEMBER dc_app;   -- agregar y modificar datos
GRANT EXECUTE ON SCHEMA::dbo TO dc_app;       -- ejecutar los procedimientos almacenados
GO

-- La aplicación nunca borra registros (usa "baja lógica" con ACTIVO = 0), así que se le prohíbe.
-- Solo puede borrar de PRODUCTO_IMAGEN (cuando se reemplaza la foto de un producto).
DENY DELETE ON dbo.USUARIO       TO dc_app;
DENY DELETE ON dbo.CATEGORIA     TO dc_app;
DENY DELETE ON dbo.PRODUCTO      TO dc_app;
DENY DELETE ON dbo.VENTA         TO dc_app;
DENY DELETE ON dbo.DETALLE_VENTA TO dc_app;
DENY DELETE ON dbo.MOVIMIENTOS   TO dc_app;
GO
-- No se le da db_ddladmin ni db_owner: no puede crear, modificar ni borrar tablas (DROP / ALTER / TRUNCATE).


/* -------------------------------------------------------------------------------------
   Comprobación: muestra los permisos de cada usuario.
   ------------------------------------------------------------------------------------- */
SELECT u.name AS usuario, r.name AS rol
FROM sys.database_role_members m
JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
JOIN sys.database_principals u ON u.principal_id = m.member_principal_id
WHERE u.name IN ('dc_admin', 'dc_app');

SELECT USER_NAME(grantee_principal_id) AS usuario, permission_name AS permiso, state_desc AS estado,
       OBJECT_NAME(major_id) AS tabla
FROM sys.database_permissions
WHERE USER_NAME(grantee_principal_id) = 'dc_app';
GO


/* =====================================================================================
   OPCIÓN B: SQL SERVER LOCAL (opcional, para practicar en su computadora)
   Requiere que SQL Server acepte "Autenticación de SQL Server y Windows"
   (SSMS > clic derecho al servidor > Propiedades > Seguridad). En LocalDB pueden seguir
   usando la conexión de Windows (Trusted_Connection) y no necesitan esta parte.
   Para usarla, quiten los /* */ que la rodean.
   ===================================================================================== */
/*
USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'dc_admin')
    CREATE LOGIN dc_admin WITH PASSWORD = '<<CONTRASEÑA_SEGURA_ADMIN>>', CHECK_POLICY = ON;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'dc_app')
    CREATE LOGIN dc_app WITH PASSWORD = '<<CONTRASEÑA_SEGURA_APP>>', CHECK_POLICY = ON;
GO
USE DeportesCalifornia;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'dc_admin') CREATE USER dc_admin FOR LOGIN dc_admin;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'dc_app')   CREATE USER dc_app   FOR LOGIN dc_app;
ALTER ROLE db_owner      ADD MEMBER dc_admin;
ALTER ROLE db_datareader ADD MEMBER dc_app;
ALTER ROLE db_datawriter ADD MEMBER dc_app;
GRANT EXECUTE ON SCHEMA::dbo TO dc_app;
DENY DELETE ON dbo.USUARIO       TO dc_app;
DENY DELETE ON dbo.CATEGORIA     TO dc_app;
DENY DELETE ON dbo.PRODUCTO      TO dc_app;
DENY DELETE ON dbo.VENTA         TO dc_app;
DENY DELETE ON dbo.DETALLE_VENTA TO dc_app;
DENY DELETE ON dbo.MOVIMIENTOS   TO dc_app;
GO
*/


/* =====================================================================================
   CADENA DE CONEXIÓN DE LA APLICACIÓN EN AZURE (usa dc_app, no el administrador)
   Portal de Azure > App Service "DeportesCalifornia1" > Configuración > Cadenas de conexión
     Nombre: CadenaSQL      Tipo: SQLAzure
     Valor:
   Server=tcp:deportescalifornia.database.windows.net,1433;Initial Catalog=DeportesCalifornia;User ID=dc_app;Password=<<CONTRASEÑA_SEGURA_APP>>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

   Para entrar con dc_admin desde Azure Data Studio o SSMS, en "Base de datos" escriban
   DeportesCalifornia (no "default"), porque el usuario vive dentro de esa base de datos.
   ===================================================================================== */
