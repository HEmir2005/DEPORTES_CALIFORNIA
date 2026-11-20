const cuerpo = porId("tablaVentas");
const fDesde = porId("fDesde");
const fHasta = porId("fHasta");
const fMetodo = porId("fMetodo");
const modalVenta = new bootstrap.Modal(porId("modalVenta"));
const METODOS = { EFECTIVO: "Efectivo", TRANSFERENCIA: "Transferencia" };

if (!esAdmin()) {
    document.querySelector(".dash-banner").insertAdjacentHTML("afterend",
        '<p class="text-muted"><i class="bi bi-info-circle" aria-hidden="true"></i> Aquí se muestran solo las ventas que registraste tú.</p>');
}

let ventas = [];
let paginaActual = 1;
let porPagina = 10;

// Fecha local de una venta en formato AAAA-MM-DD, para compararla con los filtros.
function fechaLocal(iso) {
    const d = new Date(iso);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function fila(v) {
    return `<tr>
        <td data-label="Folio" class="fw-bold">#${v.idVenta}</td>
        <td data-label="Fecha y hora">${fechaHora(v.fecha)}</td>
        <td data-label="Vendedor">${esc(v.vendedor) || "-"}</td>
        <td data-label="Método de pago">${esc(METODOS[v.metodoPago] || v.metodoPago)}</td>
        <td data-label="Artículos">${v.articulos}</td>
        <td data-label="Total" class="fw-bold">${moneda(v.total)}</td>
        <td data-label=""><button type="button" class="btn btn-pill btn-blue btn-chico" data-venta="${v.idVenta}"><i class="bi bi-receipt" aria-hidden="true"></i> Ver detalle</button></td>
    </tr>`;
}

function filtrar() {
    const rangoInvalido = fDesde.value && fHasta.value && fDesde.value > fHasta.value;
    porId("errorFechas").classList.toggle("d-none", !rangoInvalido);
    fDesde.classList.toggle("is-invalid", !!rangoInvalido);
    fHasta.classList.toggle("is-invalid", !!rangoInvalido);
    if (rangoInvalido) return [];
    return ventas.filter((v) => {
        const dia = fechaLocal(v.fecha);
        return (!fDesde.value || dia >= fDesde.value) &&
               (!fHasta.value || dia <= fHasta.value) &&
               (!fMetodo.value || v.metodoPago === fMetodo.value);
    });
}

function pintar() {
    const resultado = filtrar();
    const info = paginar(resultado, paginaActual, porPagina);
    paginaActual = info.actual;
    porId("resCantidad").textContent = resultado.length;
    porId("resTotal").textContent = moneda(resultado.reduce((suma, v) => suma + v.total, 0));
    cuerpo.innerHTML = info.total
        ? info.items.map(fila).join("")
        : '<tr><td colspan="7" class="text-muted py-4">No hay ventas con esos filtros.</td></tr>';
    pintarPaginacion("paginacionVentas", info,
        (n) => { paginaActual = n; pintar(); porId("paginacionVentas").closest(".table-card").scrollIntoView({ behavior: "smooth", block: "nearest" }); },
        { tamanos: [5, 10, 20], alCambiarTamano: (t) => { porPagina = t; paginaActual = 1; pintar(); } });
}

const volverAPagina1 = () => { paginaActual = 1; pintar(); };
[fDesde, fHasta, fMetodo].forEach((campo) => campo.addEventListener("change", volverAPagina1));
porId("btnLimpiar").addEventListener("click", () => {
    fDesde.value = ""; fHasta.value = ""; fMetodo.value = "";
    volverAPagina1();
});

cuerpo.addEventListener("click", async (e) => {
    const boton = e.target.closest("[data-venta]");
    if (!boton) return;
    try {
        const v = await api(`${API_URL}/Venta/${boton.dataset.venta}`);
        porId("detalleTitulo").textContent = `Detalle de la venta #${v.idVenta}`;
        porId("dvFecha").textContent = fechaHora(v.fecha);
        porId("dvVendedor").textContent = v.vendedor || "-";
        porId("dvMetodo").textContent = METODOS[v.metodoPago] || v.metodoPago;
        porId("dvArticulos").textContent = v.articulos;
        porId("dvTotal").textContent = moneda(v.total);
        porId("dvLineas").innerHTML = v.lineas.map((l) => `<tr>
            <td>${esc(l.producto)}${l.talla ? ` <small class="text-muted">(Talla ${esc(l.talla)})</small>` : ""}</td>
            <td class="text-end">${l.cantidad}</td>
            <td class="text-end">${moneda(l.precioUnitario)}</td>
            <td class="text-end fw-semibold">${moneda(l.subtotal)}</td></tr>`).join("")
            || '<tr><td colspan="4" class="text-muted">Esta venta no tiene detalle registrado.</td></tr>';
        modalVenta.show();
    } catch (error) {
        aviso(error.message, "danger");
    }
});

(async function () {
    try {
        ventas = await api(API_URL + "/Venta");
        pintar();
    } catch (error) {
        aviso(error.message, "danger");
        cuerpo.innerHTML = '<tr><td colspan="7" class="text-danger py-4">No se pudo cargar el historial de ventas.</td></tr>';
    }
})();
