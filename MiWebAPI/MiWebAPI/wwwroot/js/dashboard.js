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

(async function () {
    try {
        const d = await api("/api/Dashboard");
        porId("totalProductos").textContent = d.totalProductos;
        porId("stockBajo").textContent = d.stockBajo;
        porId("productosVendidosHoy").textContent = d.vendidosHoy;
        porId("listaMovimientos").innerHTML = d.ultimosMovimientos.length
            ? d.ultimosMovimientos.map(filaMovimiento).join("")
            : '<div class="text-muted py-2">Todavía no hay movimientos.</div>';
    } catch (error) {
        aviso(error.message, "danger");
    }
})();
