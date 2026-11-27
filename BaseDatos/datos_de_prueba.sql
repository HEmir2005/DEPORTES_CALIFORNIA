/* =====================================================================================
   DEPORTES CALIFORNIA - Datos de prueba (simulación)
   -------------------------------------------------------------------------------------
   ¡ATENCIÓN! PRIMERO BORRA TODOS LOS DATOS de la base (ventas, movimientos, productos,
   fotos, categorías y usuarios) y después la llena con datos realistas para simular la página:
     - 5 categorías: Tenis, Ropa, Balones, Accesorios y Gimnasio
     - 3 usuarios:
         admin           / Admin123*     -> Administrador (dueño: todo el sistema)
         maria.lopez     / Empleado123*  -> Empleado (ventas e inventario, sin editar productos)
         carlos.ramirez  / Empleado123*  -> Empleado
     - 35 productos (tenis, ropa, balones, accesorios y gimnasio) con precios en pesos
     - 60 días de historial: registro de productos, ventas diarias (incluye ventas de HOY
       si ya pasaron las 10:00 a.m.), resurtidos y algunos productos con stock bajo o agotados
     - 2 productos dados de baja (para ver la vista de "bajas")

   Todo queda coherente: el stock final de cada producto coincide con sus movimientos,
   y el total de cada venta coincide con su detalle.

   - Se puede ejecutar las veces que quieras: cada vez deja la base limpia con datos nuevos.
   - Si algo falla, no se borra nada (todo se deshace).
   - Las sesiones abiertas en la página dejarán de servir: vuelvan a iniciar sesión.
   - Ejecútalo como UN SOLO bloque (sin separar con GO), en la base DeportesCalifornia,
     con el usuario administrador (dc_admin o el administrador del servidor), NO con dc_app.
   - Las fotos no se incluyen: se suben desde la página (Editar producto).
   - Las contraseñas de los empleados se cifran solas al iniciar la aplicación o al iniciar sesión.
   ===================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON; -- si algo falla, se deshace todo

IF OBJECT_ID('dbo.PRODUCTO') IS NULL OR OBJECT_ID('dbo.VENTA') IS NULL OR OBJECT_ID('dbo.MOVIMIENTOS') IS NULL
BEGIN
    PRINT 'Esta no es la base del sistema (faltan tablas). Conéctate a la base de Deportes California. No se hizo ningún cambio.';
    RETURN;
END

BEGIN TRANSACTION;

/* ---------- 0. Borrar todos los datos anteriores (en orden, por las llaves foráneas) ---------- */
DELETE FROM dbo.DETALLE_VENTA;
DELETE FROM dbo.VENTA;
DELETE FROM dbo.MOVIMIENTOS;
DELETE FROM dbo.PRODUCTO_IMAGEN;
DELETE FROM dbo.PRODUCTO;
DELETE FROM dbo.CATEGORIA;
DELETE FROM dbo.USUARIO;

-- Los ID vuelven a empezar en 1 (solo en las tablas que ya habían tenido registros).
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.DETALLE_VENTA') AND last_value IS NOT NULL) DBCC CHECKIDENT ('dbo.DETALLE_VENTA', RESEED, 0) WITH NO_INFOMSGS;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.VENTA')         AND last_value IS NOT NULL) DBCC CHECKIDENT ('dbo.VENTA',         RESEED, 0) WITH NO_INFOMSGS;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.MOVIMIENTOS')   AND last_value IS NOT NULL) DBCC CHECKIDENT ('dbo.MOVIMIENTOS',   RESEED, 0) WITH NO_INFOMSGS;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.CATEGORIA')     AND last_value IS NOT NULL) DBCC CHECKIDENT ('dbo.CATEGORIA',     RESEED, 0) WITH NO_INFOMSGS;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.USUARIO')       AND last_value IS NOT NULL) DBCC CHECKIDENT ('dbo.USUARIO',       RESEED, 0) WITH NO_INFOMSGS;

/* ---------- Fechas (las fechas se guardan en UTC; México centro = UTC - 6) ---------- */
DECLARE @ahoraLocal  DATETIME2(0) = DATEADD(HOUR, -6, SYSUTCDATETIME());
DECLARE @hoyLocal    DATE         = CAST(@ahoraLocal AS DATE);
DECLARE @registroUtc DATETIME2(0) = DATEADD(HOUR, 6 + 9, CAST(DATEADD(DAY, -60, @hoyLocal) AS DATETIME2(0))); -- hace 60 días, 9:00 a.m.

/* ---------- 1. Categorías ---------- */
INSERT INTO dbo.CATEGORIA (NOMBRE, FECHA_ULTIMA_MODIFICACION)
VALUES ('Tenis', GETDATE()), ('Ropa', GETDATE()), ('Balones', GETDATE()), ('Accesorios', GETDATE()), ('Gimnasio', GETDATE());

/* ---------- 2. Usuarios con su rol ----------
   Los roles deben escribirse exactamente "Administrador" o "Empleado" (así los reconoce la aplicación).
   Las contraseñas se escriben aquí en texto, pero la aplicación las cifra sola al arrancar
   o en el primer inicio de sesión de cada usuario. */
INSERT INTO dbo.USUARIO (NOMBRE_USUARIO, ROL, CONTRASEÑA, FECHA_ULTIMA_MODIFICACION)
VALUES ('admin',          'Administrador', 'Admin123*',    GETDATE()),
       ('maria.lopez',    'Empleado',      'Empleado123*', GETDATE()),
       ('carlos.ramirez', 'Empleado',      'Empleado123*', GETDATE());

DECLARE @idAdmin INT = (SELECT ID_USUARIO FROM dbo.USUARIO WHERE NOMBRE_USUARIO = 'admin');

-- Quiénes venden (los tres usuarios)
DECLARE @Vendedores TABLE (ID INT);
INSERT INTO @Vendedores (ID) SELECT ID_USUARIO FROM dbo.USUARIO;

/* ---------- 3. Productos ---------- */
-- RESURTIR = 1: cuando se acaba se vuelve a surtir.  RESURTIR = 0: se deja acabar (aparece en "stock bajo").
CREATE TABLE #P (
    SKU VARCHAR(50), CATEGORIA VARCHAR(100), NOMBRE VARCHAR(150), TALLA VARCHAR(10), COLOR VARCHAR(30),
    PRECIO DECIMAL(10,2), STOCK_INICIAL INT, RESURTIR BIT, DESCRIPCION VARCHAR(500));

INSERT INTO #P VALUES
-- Tenis
('TEN-NIKE-REV7-27-NEG',  'Tenis', 'Tenis Nike Revolution 7',          '27',  'Negro',          1299.00, 18, 1, 'Tenis para correr con amortiguación de espuma suave. Ideales para entrenamiento diario.'),
('TEN-NIKE-REV7-26-BLA',  'Tenis', 'Tenis Nike Revolution 7',          '26',  'Blanco',         1299.00, 14, 1, 'Tenis para correr con amortiguación de espuma suave. Ideales para entrenamiento diario.'),
('TEN-ADI-RUNF3-27-AZU',  'Tenis', 'Tenis Adidas Runfalcon 3',         '27',  'Azul marino',    1199.00, 16, 1, 'Tenis ligeros de malla transpirable para correr o caminar.'),
('TEN-ADI-RUNF3-25-ROS',  'Tenis', 'Tenis Adidas Runfalcon 3',         '25',  'Rosa',           1199.00, 10, 0, 'Tenis ligeros de malla transpirable para correr o caminar.'),
('TEN-PUMA-SMASH-26-BLA', 'Tenis', 'Tenis Puma Smash v2',              '26',  'Blanco',          999.00, 15, 1, 'Tenis casuales de piel sintética con suela de goma.'),
('TEN-NB-411-28-GRI',     'Tenis', 'Tenis New Balance 411',            '28',  'Gris',           1399.00, 12, 1, 'Tenis para correr con soporte y buena tracción.'),
('TEN-CHARLY-FUT-26-NEG', 'Tenis', 'Tenis de fútbol rápido Charly',    '26',  'Negro/Dorado',    899.00, 14, 1, 'Tenis para fútbol rápido con suela antiderrapante.'),
('TEN-PIRMA-TAC-27-BLA',  'Tenis', 'Tachones Pirma Brasil',            '27',  'Blanco/Rojo',     749.00, 12, 1, 'Tachones para pasto natural, corte bajo.'),
('TEN-UA-CHARG-28-NEG',   'Tenis', 'Tenis Under Armour Charged Assert','28',  'Negro',          1599.00,  8, 0, 'Tenis de alto rendimiento con amortiguación Charged.'),
-- Ropa
('ROP-PLAY-DFIT-M-NEG',   'Ropa',  'Playera deportiva Nike Dri-FIT',   'M',   'Negro',           549.00, 25, 1, 'Playera de secado rápido que aleja el sudor del cuerpo.'),
('ROP-PLAY-DFIT-G-AZU',   'Ropa',  'Playera deportiva Nike Dri-FIT',   'G',   'Azul',            549.00, 20, 1, 'Playera de secado rápido que aleja el sudor del cuerpo.'),
('ROP-SHORT-ADI-M-NEG',   'Ropa',  'Short Adidas 3 franjas',           'M',   'Negro',           499.00, 20, 1, 'Short deportivo con cintura elástica y bolsas laterales.'),
('ROP-PANTS-PUMA-M-GRI',  'Ropa',  'Pants Puma Essentials',            'M',   'Gris jaspe',      799.00, 12, 1, 'Pants de felpa suave con puños ajustados.'),
('ROP-SUD-NIKE-G-NEG',    'Ropa',  'Sudadera con capucha Nike Club',   'G',   'Negro',          1099.00, 10, 1, 'Sudadera de algodón con capucha y bolsa canguro.'),
('ROP-JERSEY-MEX-M-VER',  'Ropa',  'Jersey Selección Mexicana local',  'M',   'Verde',          1499.00,  9, 0, 'Jersey de aficionado de la Selección Mexicana.'),
('ROP-LEGG-CH-MOR',       'Ropa',  'Leggings deportivos de compresión','CH',  'Morado',          459.00, 12, 1, 'Leggings de tiro alto con tela elástica.'),
('ROP-ROMPE-G-NEG',       'Ropa',  'Chamarra rompevientos',            'G',   'Negro',           899.00, 10, 1, 'Chamarra ligera resistente al viento y a la lluvia ligera.'),
('ROP-GORRA-NE-U-NEG',    'Ropa',  'Gorra New Era 9FORTY',             'Unitalla','Negro',       599.00, 14, 1, 'Gorra ajustable con broche trasero.'),
-- Balones
('BAL-ADI-FUT5-BLA',      'Balones','Balón de fútbol Adidas',          '5',   'Blanco/Negro',    649.00, 15, 1, 'Balón cosido a máquina para pasto natural o sintético.'),
('BAL-VOIT-FUT4-AMA',     'Balones','Balón de fútbol Voit infantil',   '4',   'Amarillo',        349.00, 18, 1, 'Balón tamaño 4 ideal para niños de 8 a 12 años.'),
('BAL-MOLT-BAS7-NAR',     'Balones','Balón de basquetbol Molten',      '7',   'Naranja',         899.00, 10, 1, 'Balón de piel sintética para interior y exterior.'),
('BAL-SPAL-BAS5-NAR',     'Balones','Balón de basquetbol Spalding',    '5',   'Naranja',         449.00, 12, 1, 'Balón tamaño 5 para niños.'),
('BAL-MIKA-VOL5-AZU',     'Balones','Balón de voleibol Mikasa',        '5',   'Azul/Amarillo',   799.00,  8, 0, 'Balón de voleibol de toque suave.'),
('BAL-WILS-AME-CAF',      'Balones','Balón de fútbol americano Wilson','Oficial','Café',         999.00,  9, 1, 'Balón de piel sintética con agujetas cosidas.'),
-- Accesorios
('ACC-ESPIN-M-NEG',       'Accesorios','Espinilleras Nike Charge',     'M',   'Negro',           399.00, 16, 1, 'Espinilleras ligeras con funda de ajuste.'),
('ACC-GUANT-POR-9-VER',   'Accesorios','Guantes de portero',           '9',   'Verde fosforescente', 699.00, 8, 1, 'Guantes con palma de látex y protección en los dedos.'),
('ACC-TERMO-750-AZU',     'Accesorios','Termo deportivo 750 ml',       '750 ml','Azul',          249.00, 25, 1, 'Termo de acero inoxidable que mantiene frío hasta 12 horas.'),
('ACC-MOCH-ADI-U-NEG',    'Accesorios','Mochila Adidas',               'Unitalla','Negro',       799.00, 10, 1, 'Mochila con compartimento para laptop y bolsa lateral.'),
('ACC-CALC-3P-U-BLA',     'Accesorios','Calcetas deportivas (3 pares)','Unitalla','Blanco',      349.00, 30, 1, 'Paquete de 3 pares de calcetas acolchadas.'),
('ACC-MUNEQ-U-NEG',       'Accesorios','Muñequeras (par)',             'Unitalla','Negro',       129.00, 20, 1, 'Muñequeras de toalla absorbente.'),
('ACC-BOMBA-U-NEG',       'Accesorios','Bomba para inflar balones',    'Unitalla','Negro',       199.00,  7, 0, 'Bomba de doble acción con aguja incluida.'),
-- Gimnasio
('GYM-MANC-5KG-NEG',      'Gimnasio','Mancuerna hexagonal 5 kg',       '5 kg','Negro',           449.00, 14, 1, 'Mancuerna de hierro con recubrimiento de goma (precio por pieza).'),
('GYM-TAPETE-6MM-MOR',    'Gimnasio','Tapete de yoga 6 mm',            'Unitalla','Morado',      399.00, 12, 1, 'Tapete antiderrapante con correa para transportar.'),
('GYM-LIGAS-SET-MUL',     'Gimnasio','Set de ligas de resistencia',    '5 pzas','Multicolor',    299.00, 15, 1, 'Cinco ligas con distinta resistencia y bolsa.'),
('GYM-CUERDA-U-NEG',      'Gimnasio','Cuerda para saltar',             'Unitalla','Negro',       179.00, 15, 1, 'Cuerda ajustable con mangos y rodamientos.');

INSERT INTO dbo.PRODUCTO (SKU_CODIGO, ID_CATEGORIA, NOMBRE, TALLA, COLOR, PRECIO, STOCK, IMAGEN_URL, DESCRIPCION, ACTIVO, FECHA_ULTIMA_MODIFICACION)
SELECT p.SKU, c.ID_CATEGORIA, p.NOMBRE, p.TALLA, p.COLOR, p.PRECIO, p.STOCK_INICIAL, NULL, p.DESCRIPCION, 1, @registroUtc
FROM #P p
JOIN dbo.CATEGORIA c ON c.NOMBRE = p.CATEGORIA;

INSERT INTO dbo.MOVIMIENTOS (FECHA, TIPO, SKU_CODIGO, CANTIDAD, STOCK_RESULTANTE, ID_USUARIO)
SELECT @registroUtc, 'REGISTRO', SKU, STOCK_INICIAL, STOCK_INICIAL, @idAdmin FROM #P;

/* ---------- 4. Horarios de las ventas (60 días, de 10:00 a 20:00) ---------- */
CREATE TABLE #Ventas (N INT IDENTITY(1,1) PRIMARY KEY, FECHA_LOCAL DATETIME2(0));

DECLARE @d INT = 59, @i INT, @cuantas INT, @dia DATE, @hora DATETIME2(0), @finDeSemana BIT;
WHILE @d >= 0
BEGIN
    SET @dia = DATEADD(DAY, -@d, @hoyLocal);
    SET @finDeSemana = CASE WHEN DATEDIFF(DAY, '19000107', @dia) % 7 IN (0, 6) THEN 1 ELSE 0 END; -- domingo o sábado
    SET @cuantas = CASE WHEN @d = 0 THEN 4 + ABS(CHECKSUM(NEWID())) % 3
                        ELSE 1 + ABS(CHECKSUM(NEWID())) % 4 + IIF(@finDeSemana = 1, 2, 0) END;
    SET @i = 0;
    WHILE @i < @cuantas
    BEGIN
        SET @hora = DATEADD(MINUTE, 600 + ABS(CHECKSUM(NEWID())) % 600, CAST(@dia AS DATETIME2(0)));
        IF @hora < @ahoraLocal -- hoy solo se simulan ventas que ya pasaron
            INSERT INTO #Ventas (FECHA_LOCAL) VALUES (@hora);
        SET @i += 1;
    END
    SET @d -= 1;
END

/* ---------- 5. Ventas, detalle, movimientos y resurtidos (en orden de fecha) ---------- */
DECLARE @Items TABLE (SKU VARCHAR(50), CANT INT, PRECIO DECIMAL(10,2));
DECLARE @Cambios TABLE (SKU VARCHAR(50), CANT INT, STOCK INT);
DECLARE @fechaLocal DATETIME2(0), @fechaUtc DATETIME2(0), @n INT, @idVenta INT, @total DECIMAL(10,2), @vendedor INT;

DECLARE cVentas CURSOR LOCAL FAST_FORWARD FOR
    SELECT FECHA_LOCAL FROM #Ventas ORDER BY FECHA_LOCAL, N;
OPEN cVentas;
FETCH NEXT FROM cVentas INTO @fechaLocal;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @fechaUtc = DATEADD(HOUR, 6, @fechaLocal);

    -- Resurtido: si un producto que se resurte quedó en 2 o menos, llega mercancía (12 a 20 piezas).
    DELETE FROM @Cambios;
    UPDATE pr
       SET STOCK = pr.STOCK + x.CANT, FECHA_ULTIMA_MODIFICACION = DATEADD(MINUTE, -1, @fechaUtc)
    OUTPUT inserted.SKU_CODIGO, inserted.STOCK - deleted.STOCK, inserted.STOCK INTO @Cambios (SKU, CANT, STOCK)
    FROM dbo.PRODUCTO pr
    JOIN (SELECT SKU, 12 + ABS(CHECKSUM(NEWID())) % 9 AS CANT FROM #P WHERE RESURTIR = 1) x ON x.SKU = pr.SKU_CODIGO
    WHERE pr.STOCK <= 2;

    INSERT INTO dbo.MOVIMIENTOS (FECHA, TIPO, SKU_CODIGO, CANTIDAD, STOCK_RESULTANTE, ID_USUARIO)
    SELECT DATEADD(MINUTE, -1, @fechaUtc), 'ENTRADA', SKU, CANT, STOCK, @idAdmin FROM @Cambios;

    -- Productos de la venta: 1 (60 %), 2 (30 %) o 3 (10 %) productos distintos; a veces 2 piezas del mismo.
    SET @n = ABS(CHECKSUM(NEWID())) % 10;
    SET @n = CASE WHEN @n < 6 THEN 1 WHEN @n < 9 THEN 2 ELSE 3 END;

    DELETE FROM @Items;
    INSERT INTO @Items (SKU, CANT, PRECIO)
    SELECT TOP (@n) pr.SKU_CODIGO,
           CASE WHEN pr.STOCK >= 2 AND ABS(CHECKSUM(NEWID())) % 5 = 0 THEN 2 ELSE 1 END,
           pr.PRECIO
    FROM dbo.PRODUCTO pr
    JOIN #P x ON x.SKU = pr.SKU_CODIGO
    WHERE pr.STOCK > 0
    ORDER BY NEWID();

    IF EXISTS (SELECT 1 FROM @Items)
    BEGIN
        SET @total = (SELECT SUM(CANT * PRECIO) FROM @Items);
        SET @vendedor = (SELECT TOP 1 ID FROM @Vendedores ORDER BY NEWID());

        INSERT INTO dbo.VENTA (ID_USUARIO, FECHA_VENTA, TOTAL, METODO_PAGO, FECHA_ULTIMA_MODIFICACION)
        VALUES (@vendedor, @fechaUtc, @total,
                CASE WHEN ABS(CHECKSUM(NEWID())) % 10 < 6 THEN 'EFECTIVO' ELSE 'TRANSFERENCIA' END,
                @fechaUtc);
        SET @idVenta = SCOPE_IDENTITY();

        INSERT INTO dbo.DETALLE_VENTA (ID_VENTA, SKU_CODIGO, CANTIDAD, PRECIO_UNITARIO, SUBTOTAL, FECHA_ULTIMA_MODIFICACION)
        SELECT @idVenta, SKU, CANT, PRECIO, CANT * PRECIO, @fechaUtc FROM @Items;

        DELETE FROM @Cambios;
        UPDATE pr
           SET STOCK = pr.STOCK - i.CANT, FECHA_ULTIMA_MODIFICACION = @fechaUtc
        OUTPUT inserted.SKU_CODIGO, deleted.STOCK - inserted.STOCK, inserted.STOCK INTO @Cambios (SKU, CANT, STOCK)
        FROM dbo.PRODUCTO pr
        JOIN @Items i ON i.SKU = pr.SKU_CODIGO;

        INSERT INTO dbo.MOVIMIENTOS (FECHA, TIPO, SKU_CODIGO, CANTIDAD, STOCK_RESULTANTE, ID_USUARIO)
        SELECT @fechaUtc, 'VENTA', SKU, CANT, STOCK, @vendedor FROM @Cambios;
    END

    FETCH NEXT FROM cVentas INTO @fechaLocal;
END

CLOSE cVentas;
DEALLOCATE cVentas;

/* ---------- 6. Dos productos dados de baja (baja lógica) ---------- */
UPDATE dbo.PRODUCTO
   SET ACTIVO = 0, FECHA_ULTIMA_MODIFICACION = DATEADD(DAY, -3, SYSUTCDATETIME())
 WHERE SKU_CODIGO IN ('ROP-LEGG-CH-MOR', 'GYM-CUERDA-U-NEG');

COMMIT TRANSACTION;

/* ---------- Resumen ---------- */
SELECT
    (SELECT COUNT(*) FROM dbo.USUARIO)                                           AS usuarios,
    (SELECT COUNT(*) FROM #P)                                                    AS productos_agregados,
    (SELECT COUNT(*) FROM dbo.PRODUCTO p JOIN #P x ON x.SKU = p.SKU_CODIGO
      WHERE p.ACTIVO = 1 AND p.STOCK <= 5)                                       AS con_stock_bajo,
    (SELECT COUNT(*) FROM dbo.VENTA WHERE FECHA_VENTA >= DATEADD(HOUR, 6, CAST(DATEADD(DAY, -60, @hoyLocal) AS DATETIME2(0)))) AS ventas_simuladas,
    (SELECT COUNT(*) FROM dbo.VENTA WHERE FECHA_VENTA >= DATEADD(HOUR, 6, CAST(@hoyLocal AS DATETIME2(0))))                    AS ventas_de_hoy,
    (SELECT SUM(TOTAL) FROM dbo.VENTA WHERE FECHA_VENTA >= DATEADD(HOUR, 6, CAST(DATEADD(DAY, -60, @hoyLocal) AS DATETIME2(0)))) AS total_vendido;

DROP TABLE #Ventas;
DROP TABLE #P;

PRINT 'Datos de prueba cargados correctamente.';
