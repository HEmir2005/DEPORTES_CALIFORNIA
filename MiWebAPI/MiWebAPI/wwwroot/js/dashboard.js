function filaMovimiento(m) {
    const talla = m.talla ? ` (Talla ${esc(m.talla)})` : "";
    return `<div class="list-group-item bg-transparent d-flex justify-content-between align-items-center gap-2 px-0 py-2">
        <div class="d-flex align-items-center gap-2">
            <i class="bi ${ICONO_MOV[m.tipo] || ""} fs-4"></i>
            <span class="fw-semibold">${ETIQUETA_MOV[m.tipo] || esc(m.tipo)} - ${esc(m.producto)}${talla} - ${m.cantidad}x</span>
        </div>
        <span class="fw-bold text-nowrap">${fechaCorta(m.fecha)}</span>
    </div>`;
}

let movimientos = [];
let paginaMov = 1;
const MOV_POR_PAGINA = 5;

function pintarMovimientos() {
    const info = paginar(movimientos, paginaMov, MOV_POR_PAGINA);
    paginaMov = info.actual;
    porId("listaMovimientos").innerHTML = info.total
        ? info.items.map(filaMovimiento).join("")
        : '<div class="text-muted py-2">Todavía no hay movimientos.</div>';
    pintarPaginacion("paginacionMovimientos", info, (n) => { paginaMov = n; pintarMovimientos(); });
}

(async function () {
    try {
        const d = await api(API_URL + "/Dashboard");
        porId("totalProductos").textContent = d.totalProductos;
        porId("stockBajo").textContent = d.stockBajo;
        porId("productosVendidosHoy").textContent = d.vendidosHoy;
        porId("ventasHoyMonto").textContent = moneda(d.ventasHoyMonto);
        porId("ventasHoyCantidad").textContent = d.ventasHoyCantidad === 1 ? "1 venta registrada" : `${d.ventasHoyCantidad} ventas registradas`;
        movimientos = d.ultimosMovimientos;
        pintarMovimientos();
    } catch (error) {
        aviso(error.message, "danger");
    }
})();
