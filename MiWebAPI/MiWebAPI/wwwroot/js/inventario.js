const buscar = porId("inputBuscar");
const categoria = porId("selectCategoria");
const cuerpo = porId("tablaProductos");
let productos = [];

function fila(p) {
    const bajo = p.stock <= STOCK_BAJO ? "text-danger" : "";
    return `<tr>
        <td>${imgProducto(p, "table-img")}</td>
        <td class="fw-semibold">${esc(p.sku)}</td>
        <td class="fw-bold"><a class="text-dark" href="detalle-producto.html?sku=${encodeURIComponent(p.sku)}">${esc(p.nombre)}</a></td>
        <td>${esc(p.categoria)}</td>
        <td>${esc(p.talla)}</td>
        <td>${esc(p.color)}</td>
        <td class="fw-bold">${moneda(p.precio)}</td>
        <td class="fw-bold ${bajo}">${p.stock}</td>
        <td><a class="text-dark" href="producto-form.html?sku=${encodeURIComponent(p.sku)}" title="Editar"><i class="bi bi-pencil fs-5"></i></a></td>
    </tr>`;
}

function pintar() {
    const texto = buscar.value.trim().toLowerCase();
    const cat = categoria.value;
    const lista = productos.filter((p) =>
        (!cat || p.categoria === cat) &&
        (!texto || [p.nombre, p.sku, p.color].some((x) => (x ?? "").toLowerCase().includes(texto))));
    cuerpo.innerHTML = lista.length
        ? lista.map(fila).join("")
        : '<tr><td colspan="9" class="text-muted py-4">No se encontraron productos.</td></tr>';
}

buscar.addEventListener("input", pintar);
categoria.addEventListener("change", pintar);

(async function () {
    try {
        const [categorias, lista] = await Promise.all([api("/api/Producto/categorias"), api("/api/Producto")]);
        categoria.insertAdjacentHTML("beforeend",
            categorias.map((c) => `<option value="${esc(c.nombre)}">${esc(c.nombre)}</option>`).join(""));
        productos = lista;
        pintar();
    } catch (error) {
        aviso(error.message, "danger");
        cuerpo.innerHTML = '<tr><td colspan="9" class="text-danger py-4">No se pudo cargar el inventario.</td></tr>';
    }
})();
