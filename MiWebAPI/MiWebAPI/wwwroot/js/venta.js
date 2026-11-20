const lista = porId("listaProductos");
const itemsCarrito = porId("itemsCarrito");
const buscar = porId("inputBuscar");
const categoria = porId("selectCategoria");
let productos = [];
let carrito = leerCarrito();
let paginaLista = 1;
const POR_PAGINA_VENTA = 5;

function pintarLista() {
    const texto = buscar.value.trim().toLowerCase();
    const cat = categoria.value;
    const visibles = productos.filter((p) =>
        (!cat || p.categoria === cat) &&
        (!texto || [p.nombre, p.sku, p.color].some((x) => (x ?? "").toLowerCase().includes(texto))));

    const info = paginar(visibles, paginaLista, POR_PAGINA_VENTA);
    paginaLista = info.actual;

    lista.innerHTML = info.total ? info.items.map((p) => `
        <div class="prod-row ${p.stock <= 0 ? "agotado" : ""}" data-add="${esc(p.sku)}">
            ${imgProducto(p)}
            <div class="flex-grow-1 lh-sm">
                <div class="fw-bold">${esc(p.nombre)}</div>
                <small>Talla: ${esc(p.talla) || "-"} | ${esc(p.color)}</small>
            </div>
            <div class="text-end lh-sm">
                <div class="fw-bold">${moneda(p.precio)}</div>
                <small class="${p.stock <= STOCK_BAJO ? "text-danger fw-bold" : ""}">${p.stock > 0 ? `Stock: ${p.stock}` : "Agotado"}</small>
            </div>
        </div>`).join("") : '<div class="text-muted p-2">No se encontraron productos.</div>';
    pintarPaginacion("paginacionVenta", info, (n) => { paginaLista = n; pintarLista(); });
}

function pintarCarrito() {
    let total = 0;
    itemsCarrito.innerHTML = carrito.length ? carrito.map((item) => {
        const p = productos.find((x) => x.sku === item.sku);
        const subtotal = p.precio * item.cantidad;
        total += subtotal;
        return `<div class="cart-row">
            ${imgProducto(p)}
            <div class="cart-info lh-sm">
                <div class="fw-bold">${esc(p.nombre)}</div>
                <small>Talla: ${esc(p.talla) || "-"} | ${moneda(p.precio)}</small>
                ${item.cantidad >= p.stock ? `<div class="text-danger small fw-semibold">Máximo disponible: ${p.stock}</div>` : ""}
            </div>
            <div class="cart-fin">
                <div class="qty-box">
                    <button type="button" data-act="menos" data-id="${esc(p.sku)}" aria-label="Quitar uno">-</button>
                    <span>${item.cantidad}</span>
                    <button type="button" data-act="mas" data-id="${esc(p.sku)}" aria-label="Agregar uno" ${item.cantidad >= p.stock ? "disabled" : ""}>+</button>
                </div>
                <div class="fw-bold text-end" style="min-width:80px">${moneda(subtotal)}</div>
                <button type="button" class="btn p-0 border-0 text-danger" data-act="quitar" data-id="${esc(p.sku)}" aria-label="Eliminar del carrito"><i class="bi bi-trash fs-5"></i></button>
            </div>
        </div>`;
    }).join("") : '<div class="text-muted p-2">El carrito está vacío. Toca un producto para agregarlo.</div>';

    porId("totalCarrito").textContent = moneda(total);
    porId("btnFinalizar").disabled = carrito.length === 0;
}

function agregar(sku) {
    const p = productos.find((x) => x.sku === sku);
    if (!p || p.stock <= 0) return aviso("Este producto está agotado.", "warning");
    const item = carrito.find((i) => i.sku === sku);
    if (item) {
        if (item.cantidad >= p.stock) return aviso("No hay más unidades disponibles de este producto.", "warning");
        item.cantidad++;
    } else {
        carrito.push({ sku, cantidad: 1 });
    }
    guardarCarrito(carrito);
    pintarCarrito();
}

lista.addEventListener("click", (e) => {
    const fila = e.target.closest("[data-add]");
    if (fila) agregar(fila.dataset.add);
});

itemsCarrito.addEventListener("click", (e) => {
    const boton = e.target.closest("[data-act]");
    if (!boton) return;
    const sku = boton.dataset.id;
    const item = carrito.find((i) => i.sku === sku);
    const p = productos.find((x) => x.sku === sku);

    if (boton.dataset.act === "mas") {
        if (item.cantidad < p.stock) item.cantidad++;
        else aviso("No hay más unidades disponibles de este producto.", "warning");
    }
    if (boton.dataset.act === "menos") item.cantidad--;
    if (boton.dataset.act === "quitar" || item.cantidad <= 0) carrito = carrito.filter((i) => i.sku !== sku);

    guardarCarrito(carrito);
    pintarCarrito();
});

buscar.addEventListener("input", () => { paginaLista = 1; pintarLista(); });
categoria.addEventListener("change", () => { paginaLista = 1; pintarLista(); });
porId("btnFinalizar").addEventListener("click", () => (location.href = "confirmar-venta.html"));

(async function () {
    try {
        const [categorias, lista] = await Promise.all([api(API_URL + "/Producto/categorias"), api(API_URL + "/Producto")]);
        categoria.insertAdjacentHTML("beforeend",
            categorias.map((c) => `<option value="${esc(c.nombre)}">${esc(c.nombre)}</option>`).join(""));
        productos = lista;
        // Descarta del carrito lo que ya no existe y ajusta cantidades al stock actual.
        carrito = carrito
            .filter((i) => productos.some((p) => p.sku === i.sku && p.stock > 0))
            .map((i) => ({ ...i, cantidad: Math.min(i.cantidad, productos.find((p) => p.sku === i.sku).stock) }));
        guardarCarrito(carrito);
        pintarLista();
        pintarCarrito();
    } catch (error) {
        aviso(error.message, "danger");
    }
})();
