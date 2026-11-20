let lineas = [];
let metodo = null;
const btnConfirmar = porId("btnConfirmar");

const productosSinStock = () => lineas.filter(({ p, cantidad }) => cantidad > p.stock);

function mostrarErrorVenta(texto) {
    const alerta = porId("alertaVenta");
    alerta.innerHTML = `<i class="bi bi-exclamation-triangle-fill" aria-hidden="true"></i> ${esc(texto)}`;
    alerta.classList.remove("d-none");
    alerta.scrollIntoView({ behavior: "smooth", block: "center" });
}

function pintarResumen() {
    let total = 0;
    porId("tablaResumen").innerHTML = lineas.map(({ p, cantidad }) => {
        const subtotal = p.precio * cantidad;
        total += subtotal;
        const falla = cantidad > p.stock;
        return `<tr class="${falla ? "fila-problema" : ""}">
            <td data-label="">${imgProducto(p, "table-img")}</td>
            <td data-label="Producto" class="text-start fw-bold">${esc(p.nombre)}<br><small class="fw-normal">Talla: ${esc(p.talla) || "-"}</small></td>
            <td data-label="Cantidad">${cantidad}${falla ? `<div class="text-danger small fw-semibold">Solo hay ${p.stock}</div>` : ""}</td>
            <td data-label="Precio">${moneda(p.precio)}</td>
            <td data-label="Subtotal" class="fw-bold">${moneda(subtotal)}</td></tr>`;
    }).join("");
    porId("totalPagar").textContent = moneda(total);

    // Si algún producto ya no tiene suficiente stock, se avisa y se bloquea la venta.
    const malos = productosSinStock();
    const alerta = porId("alertaVenta");
    if (malos.length) {
        alerta.innerHTML = `<i class="bi bi-exclamation-triangle-fill" aria-hidden="true"></i> Revisa el carrito: ` +
            malos.map(({ p }) => `${esc(p.nombre)} solo tiene ${p.stock} en stock`).join("; ") + ".";
        alerta.classList.remove("d-none");
    } else {
        alerta.classList.add("d-none");
    }
    btnConfirmar.disabled = malos.length > 0;
}

document.querySelectorAll("[data-metodo]").forEach((boton) =>
    boton.addEventListener("click", () => {
        metodo = boton.dataset.metodo;
        document.querySelectorAll("[data-metodo]").forEach((b) => b.classList.toggle("sel", b === boton));
        porId("errorPago").classList.add("d-none");
        porId("opcionesPago").classList.remove("pago-error");
    }));

btnConfirmar.addEventListener("click", async () => {
    if (productosSinStock().length) return;
    if (!metodo) {
        porId("errorPago").classList.remove("d-none");
        porId("opcionesPago").classList.add("pago-error");
        porId("opcionesPago").scrollIntoView({ behavior: "smooth", block: "center" });
        return;
    }

    porId("alertaVenta").classList.add("d-none");
    btnConfirmar.disabled = true;
    btnConfirmar.textContent = "Registrando...";
    try {
        const r = await api(API_URL + "/Venta", jsonOpts("POST", {
            metodoPago: metodo,
            idUsuario: obtenerUsuario()?.idUsuario ?? null,
            items: lineas.map(({ p, cantidad }) => ({ sku: p.sku, cantidad }))
        }));
        guardarCarrito([]);
        porId("folio").textContent = `#${r.datos.idVenta}`;
        porId("totalFinal").textContent = moneda(r.datos.total);
        new bootstrap.Modal(porId("modalExito")).show();
    } catch (error) {
        mostrarErrorVenta(error.message);
        btnConfirmar.disabled = false;
        btnConfirmar.textContent = "Confirmar venta";
    }
});

(async function () {
    const carrito = leerCarrito();
    if (!carrito.length) return location.replace("registrar-venta.html");
    try {
        const productos = await api(API_URL + "/Producto");
        lineas = carrito
            .map((i) => ({ p: productos.find((x) => x.sku === i.sku), cantidad: i.cantidad }))
            .filter((l) => l.p);
        if (!lineas.length) return location.replace("registrar-venta.html");
        pintarResumen();
    } catch (error) {
        mostrarErrorVenta(error.message);
    }
})();
