const sku = parametro("sku");
const rutaSku = encodeURIComponent(sku ?? "");
if (!sku) location.replace("inventario.html");

const modalMov = new bootstrap.Modal(porId("modalMov"));
let tipoMov = "ENTRADA";

// Solo el administrador mueve existencias o edita el producto.
if (!esAdmin()) ["btnEntrada", "btnSalida", "btnEditar"].forEach((id) => porId(id).classList.add("d-none"));
let stockActual = 0;
let movs = [];
let paginaMov = 1;
const MOV_POR_PAGINA = 5;

function pintarMovimientos() {
    const info = paginar(movs, paginaMov, MOV_POR_PAGINA);
    paginaMov = info.actual;
    porId("tablaMov").innerHTML = info.total
        ? info.items.map((m) => `<tr>
            <td>${fechaHora(m.fecha)}</td>
            <td><i class="bi ${ICONO_MOV[m.tipo] || ""}"></i> ${ETIQUETA_MOV[m.tipo] || esc(m.tipo)}</td>
            <td class="text-end">${m.cantidad}</td>
            <td class="text-end">${m.stockResultante}</td>
            <td>${esc(m.usuario) || "-"}</td></tr>`).join("")
        : '<tr><td colspan="5" class="text-muted">Sin movimientos.</td></tr>';
    pintarPaginacion("paginacionMov", info, (n) => { paginaMov = n; pintarMovimientos(); });
}

async function cargar() {
    try {
        const [p, movimientos] = await Promise.all([api(`${API_URL}/Producto/${rutaSku}`), api(`${API_URL}/Producto/${rutaSku}/movimientos`)]);
        porId("dImagen").innerHTML = imgProducto(p, "detail-img", true);
        porId("dNombre").textContent = p.nombre;
        porId("dSku").textContent = p.sku;
        porId("dCategoria").textContent = p.categoria;
        porId("dPrecio").textContent = moneda(p.precio);
        porId("dTalla").textContent = p.talla || "-";
        porId("dColor").textContent = p.color || "-";
        porId("dStock").textContent = p.stock;
        stockActual = p.stock;
        porId("dStock").classList.toggle("text-danger", p.stock <= STOCK_BAJO);
        porId("dAvisoStock").textContent = p.stock <= STOCK_BAJO ? "Stock bajo" : "";
        porId("dDescripcion").textContent = p.descripcion || "Sin descripción.";
        porId("btnEditar").href = `producto-form.html?sku=${encodeURIComponent(p.sku)}`;

        movs = movimientos;
        paginaMov = 1;
        pintarMovimientos();
    } catch (error) {
        if (/no encontrado/i.test(error.message)) return mostrarNoEncontrado("Ese producto no existe o está dado de baja.");
        aviso(error.message, "danger");
    }
}

function abrirMovimiento(tipo) {
    tipoMov = tipo;
    porId("tituloMov").textContent = tipo === "ENTRADA" ? "Registrar entrada" : "Registrar salida";
    porId("cantidadMov").value = "";
    marcarCampo(porId("cantidadMov"), "");
    porId("cantidadMov").classList.remove("is-valid");
    porId("stockMov").textContent = `Stock actual: ${stockActual} unidades`;
    modalMov.show();
    setTimeout(() => porId("cantidadMov").focus(), 300);
}

porId("btnEntrada").addEventListener("click", () => abrirMovimiento("ENTRADA"));
porId("btnSalida").addEventListener("click", () => abrirMovimiento("SALIDA"));

// Revisa la cantidad y muestra el motivo debajo del campo. Devuelve la cantidad o null si hay error.
function validarMovimiento() {
    const campo = porId("cantidadMov");
    const cantidad = Number(campo.value);
    let mensaje = "";
    if (campo.value.trim() === "") mensaje = "Escribe la cantidad de unidades.";
    else if (!Number.isInteger(cantidad) || cantidad <= 0) mensaje = "La cantidad debe ser un número entero mayor a cero.";
    else if (cantidad > 100000) mensaje = "La cantidad no puede pasar de 100,000.";
    else if (tipoMov === "SALIDA" && cantidad > stockActual) mensaje = `Solo hay ${stockActual} unidades en stock.`;
    return marcarCampo(campo, mensaje) ? cantidad : null;
}
porId("cantidadMov").addEventListener("input", validarMovimiento);

porId("btnGuardarMov").addEventListener("click", async () => {
    const cantidad = validarMovimiento();
    if (cantidad === null) return;
    try {
        const r = await api(`${API_URL}/Producto/${rutaSku}/movimiento`,
            jsonOpts("POST", { tipo: tipoMov, cantidad, idUsuario: obtenerUsuario()?.idUsuario ?? null }));
        modalMov.hide();
        aviso(r.respuesta);
        cargar();
    } catch (error) {
        aviso(error.message, "danger");
    }
});

cargar();
