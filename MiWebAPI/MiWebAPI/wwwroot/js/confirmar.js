let lineas = [];
let metodo = null;
const btnConfirmar = porId("btnConfirmar");

function pintarResumen() {
    let total = 0;
    porId("tablaResumen").innerHTML = lineas.map(({ p, cantidad }) => {
        const subtotal = p.precio * cantidad;
        total += subtotal;
        return `<tr>
            <td data-label="">${imgProducto(p, "table-img")}</td>
            <td data-label="Producto" class="text-start fw-bold">${esc(p.nombre)}<br><small class="fw-normal">Talla: ${esc(p.talla) || "-"}</small></td>
            <td data-label="Cantidad">${cantidad}</td>
            <td data-label="Precio">${moneda(p.precio)}</td>
            <td data-label="Subtotal" class="fw-bold">${moneda(subtotal)}</td></tr>`;
    }).join("");
    porId("totalPagar").textContent = moneda(total);
}

document.querySelectorAll("[data-metodo]").forEach((boton) =>
    boton.addEventListener("click", () => {
        metodo = boton.dataset.metodo;
        document.querySelectorAll("[data-metodo]").forEach((b) => b.classList.toggle("sel", b === boton));
    }));

btnConfirmar.addEventListener("click", async () => {
    if (!metodo) return aviso("Selecciona un método de pago.", "warning");
    btnConfirmar.disabled = true;
    try {
        const r = await api("/api/Venta", jsonOpts("POST", {
            metodoPago: metodo,
            idUsuario: obtenerUsuario()?.idUsuario ?? null,
            items: lineas.map(({ p, cantidad }) => ({ sku: p.sku, cantidad }))
        }));
        guardarCarrito([]);
        porId("folio").textContent = `#${r.datos.idVenta}`;
        porId("totalFinal").textContent = moneda(r.datos.total);
        new bootstrap.Modal(porId("modalExito")).show();
    } catch (error) {
        aviso(error.message, "danger");
        btnConfirmar.disabled = false;
    }
});

(async function () {
    const carrito = leerCarrito();
    if (!carrito.length) return location.replace("registrar-venta.html");
    try {
        const productos = await api("/api/Producto");
        lineas = carrito
            .map((i) => ({ p: productos.find((x) => x.sku === i.sku), cantidad: i.cantidad }))
            .filter((l) => l.p);
        if (!lineas.length) return location.replace("registrar-venta.html");
        pintarResumen();
    } catch (error) {
        aviso(error.message, "danger");
    }
})();
