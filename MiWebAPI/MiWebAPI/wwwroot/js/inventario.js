const buscar = porId("inputBuscar");
const categoria = porId("selectCategoria");
const existencias = porId("selectExistencias");
const cuerpo = porId("tablaProductos");
const modalResurtir = new bootstrap.Modal(porId("modalResurtir"));

// El empleado solo consulta: no ve agregar, resurtir, editar ni los productos dados de baja.
if (!esAdmin()) {
    porId("btnAnadir").parentElement.classList.add("d-none");
    porId("grupoVista").classList.add("d-none");
}

let activos = [];
let bajas = [];
let bajasCargadas = false;
let vista = "activos";   // "activos" | "bajas"
let paginaActual = 1;
let porPagina = 5;
let skuResurtir = null;

const lista = () => (vista === "activos" ? activos : bajas);

function celdaStock(p) {
    if (p.stock <= 0) return '<span class="badge text-bg-danger">Agotado</span>';
    if (p.stock <= STOCK_BAJO) return `<span class="text-danger">${p.stock}</span> <span class="badge text-bg-warning">Bajo</span>`;
    return p.stock;
}

function acciones(p) {
    if (vista === "bajas") {
        return `<button type="button" class="btn btn-pill btn-green btn-chico" data-reactivar="${esc(p.sku)}"><i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i> Reactivar</button>`;
    }
    if (!esAdmin()) {
        return `<a class="btn btn-outline-dark btn-chico" href="detalle-producto.html?sku=${encodeURIComponent(p.sku)}"><i class="bi bi-eye" aria-hidden="true"></i> Ver detalle</a>`;
    }
    return `<div class="d-flex flex-wrap justify-content-center gap-2">
        <button type="button" class="btn btn-pill btn-green btn-chico" data-resurtir="${esc(p.sku)}"><i class="bi bi-plus-circle" aria-hidden="true"></i> Resurtir</button>
        <a class="btn btn-outline-dark btn-chico" href="producto-form.html?sku=${encodeURIComponent(p.sku)}"><i class="bi bi-pencil" aria-hidden="true"></i> Editar</a>
    </div>`;
}

function fila(p) {
    const nombre = vista === "activos"
        ? `<a class="text-dark" href="detalle-producto.html?sku=${encodeURIComponent(p.sku)}">${esc(p.nombre)}</a>`
        : esc(p.nombre);
    return `<tr class="${vista === "activos" && p.stock <= 0 ? "fila-agotado" : ""}">
        <td data-label="">${imgProducto(p, "table-img", true)}</td>
        <td data-label="Código" class="fw-semibold">${esc(p.sku)}</td>
        <td data-label="Producto" class="fw-bold">${nombre}</td>
        <td data-label="Categoría">${esc(p.categoria)}</td>
        <td data-label="Talla">${esc(p.talla)}</td>
        <td data-label="Color">${esc(p.color)}</td>
        <td data-label="Precio" class="fw-bold">${moneda(p.precio)}</td>
        <td data-label="Stock" class="fw-bold">${celdaStock(p)}</td>
        <td data-label="">${acciones(p)}</td>
    </tr>`;
}

function filtrar() {
    const texto = buscar.value.trim().toLowerCase();
    const cat = categoria.value;
    const ex = existencias.value;
    return lista().filter((p) =>
        (!cat || p.categoria === cat) &&
        (!ex || (ex === "agotado" ? p.stock <= 0 : p.stock <= STOCK_BAJO)) &&
        (!texto || [p.nombre, p.sku, p.color].some((x) => (x ?? "").toLowerCase().includes(texto))));
}

function pintar() {
    const resultado = filtrar();
    const info = paginar(resultado, paginaActual, porPagina);
    paginaActual = info.actual;
    const sinFiltros = !buscar.value.trim() && !categoria.value && !existencias.value;
    const vacio = vista === "bajas" && sinFiltros ? "No hay productos dados de baja." : "No se encontraron productos.";
    cuerpo.innerHTML = info.total
        ? info.items.map(fila).join("")
        : `<tr><td colspan="9" class="text-muted py-4">${vacio}</td></tr>`;
    pintarPaginacion("paginacionInventario", info,
        (n) => { paginaActual = n; pintar(); porId("paginacionInventario").closest(".table-card").scrollIntoView({ behavior: "smooth", block: "nearest" }); },
        { tamanos: [5, 10, 20], alCambiarTamano: (t) => { porPagina = t; paginaActual = 1; pintar(); } });
}

// Aviso en la parte de arriba cuando hay productos agotados o por agotarse.
function actualizarAlerta() {
    const caja = porId("alertaStock");
    const agotados = activos.filter((p) => p.stock <= 0).length;
    const bajos = activos.filter((p) => p.stock > 0 && p.stock <= STOCK_BAJO).length;
    if (vista !== "activos" || agotados + bajos === 0) { caja.classList.add("d-none"); return; }
    const partes = [];
    if (agotados) partes.push(`${agotados} ${agotados === 1 ? "producto agotado" : "productos agotados"}`);
    if (bajos) partes.push(`${bajos} con stock bajo`);
    porId("alertaStockTexto").innerHTML = `<i class="bi bi-exclamation-triangle-fill" aria-hidden="true"></i> Atención: ${partes.join(" y ")}.`;
    caja.dataset.filtro = agotados ? "agotado" : "bajo";
    caja.classList.remove("d-none");
}

async function cargarActivos() { activos = await api(API_URL + "/Producto"); }
async function cargarBajas() { bajas = await api(API_URL + "/Producto/inactivos"); bajasCargadas = true; }

// ---------- Filtros y vistas ----------
const volverAPagina1 = () => { paginaActual = 1; pintar(); };
buscar.addEventListener("input", volverAPagina1);
categoria.addEventListener("change", volverAPagina1);
existencias.addEventListener("change", volverAPagina1);
porId("btnVerFaltantes").addEventListener("click", () => { existencias.value = porId("alertaStock").dataset.filtro; volverAPagina1(); });

document.querySelectorAll('input[name="vista"]').forEach((opcion) =>
    opcion.addEventListener("change", async () => {
        vista = porId("vistaBaja").checked ? "bajas" : "activos";
        try { if (vista === "bajas" && !bajasCargadas) await cargarBajas(); }
        catch (error) { aviso(error.message, "danger"); }
        actualizarAlerta();
        volverAPagina1();
    }));

// ---------- Resurtir ----------
function abrirResurtir(sku) {
    const p = activos.find((x) => x.sku === sku);
    if (!p) return;
    skuResurtir = sku;
    porId("resurtirNombre").textContent = `${p.nombre} (Talla ${p.talla || "-"})`;
    porId("resurtirStock").textContent = `Stock actual: ${p.stock} unidades`;
    porId("cantidadResurtir").value = "";
    porId("resurtirResultado").textContent = "";
    marcarCampo(porId("cantidadResurtir"), "");
    modalResurtir.show();
    setTimeout(() => porId("cantidadResurtir").focus(), 300);
}

function validarResurtir() {
    const campo = porId("cantidadResurtir");
    const cantidad = Number(campo.value);
    let mensaje = "";
    if (campo.value.trim() === "") mensaje = "Escribe cuántas unidades llegaron.";
    else if (!Number.isInteger(cantidad) || cantidad <= 0) mensaje = "La cantidad debe ser un número entero mayor a cero.";
    else if (cantidad > 100000) mensaje = "La cantidad no puede pasar de 100,000.";
    const p = activos.find((x) => x.sku === skuResurtir);
    porId("resurtirResultado").textContent = !mensaje && p ? `Quedarán ${p.stock + cantidad} unidades en stock.` : "";
    return marcarCampo(campo, mensaje) ? cantidad : null;
}
porId("cantidadResurtir").addEventListener("input", validarResurtir);

porId("btnConfirmarResurtir").addEventListener("click", async () => {
    const cantidad = validarResurtir();
    if (cantidad === null) return;
    try {
        const r = await api(`${API_URL}/Producto/${encodeURIComponent(skuResurtir)}/movimiento`,
            jsonOpts("POST", { tipo: "ENTRADA", cantidad, idUsuario: obtenerUsuario()?.idUsuario ?? null }));
        modalResurtir.hide();
        await cargarActivos();
        actualizarAlerta();
        pintar();
        aviso(`${r.respuesta} Se agregaron ${cantidad} unidades.`);
    } catch (error) {
        aviso(error.message, "danger");
    }
});

// ---------- Reactivar un producto dado de baja ----------
async function reactivar(sku) {
    const p = bajas.find((x) => x.sku === sku);
    if (!p) return;
    const acepta = await confirmarAccion({
        titulo: "Reactivar producto",
        mensaje: `¿Quieres volver a activar "${p.nombre}"? Aparecerá otra vez en el inventario y en las ventas.`,
        boton: "Reactivar",
        tipo: "green"
    });
    if (!acepta) return;
    try {
        const r = await api(`${API_URL}/Producto/${encodeURIComponent(sku)}/reactivar`, { method: "POST" });
        await Promise.all([cargarActivos(), cargarBajas()]);
        actualizarAlerta();
        pintar();
        aviso(r.respuesta);
    } catch (error) {
        aviso(error.message, "danger");
    }
}

cuerpo.addEventListener("click", (e) => {
    const bResurtir = e.target.closest("[data-resurtir]");
    if (bResurtir) return abrirResurtir(bResurtir.dataset.resurtir);
    const bReactivar = e.target.closest("[data-reactivar]");
    if (bReactivar) return reactivar(bReactivar.dataset.reactivar);
});

(async function () {
    try {
        const [categorias] = await Promise.all([api(API_URL + "/Producto/categorias"), cargarActivos()]);
        categoria.insertAdjacentHTML("beforeend",
            categorias.map((c) => `<option value="${esc(c.nombre)}">${esc(c.nombre)}</option>`).join(""));
        actualizarAlerta();
        pintar();
    } catch (error) {
        aviso(error.message, "danger");
        cuerpo.innerHTML = '<tr><td colspan="9" class="text-danger py-4">No se pudo cargar el inventario.</td></tr>';
    }
})();
