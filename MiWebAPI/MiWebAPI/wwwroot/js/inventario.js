const buscar = porId("inputBuscar");
const categoria = porId("selectCategoria");
const cuerpo = porId("tablaProductos");
let productos = [];

function fila(p) {
    const bajo = p.stock <= STOCK_BAJO ? "text-danger" : "";
    return `<tr>
        <td data-label="">${imgProducto(p, "table-img", true)}</td>
        <td data-label="Código" class="fw-semibold">${esc(p.sku)}</td>
        <td data-label="Producto" class="fw-bold"><a class="text-dark" href="detalle-producto.html?sku=${encodeURIComponent(p.sku)}">${esc(p.nombre)}</a></td>
        <td data-label="Categoría">${esc(p.categoria)}</td>
        <td data-label="Talla">${esc(p.talla)}</td>
        <td data-label="Color">${esc(p.color)}</td>
        <td data-label="Precio" class="fw-bold">${moneda(p.precio)}</td>
        <td data-label="Stock" class="fw-bold ${bajo}">${p.stock}</td>
        <td data-label=""><a class="btn btn-outline-dark btn-sm" href="producto-form.html?sku=${encodeURIComponent(p.sku)}"><i class="bi bi-pencil" aria-hidden="true"></i> Editar</a></td>
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
