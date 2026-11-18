// Sirve para registrar (sin ?sku) y para editar (?sku=20000001) un producto.
const form = porId("formProducto");
const skuEditar = parametro("sku"); // sin ?sku = registrar; con ?sku = editar
const selCategoria = porId("idCategoria");
const inputImagen = porId("imagen");
const vistaPrevia = porId("imgPreview");
const btnGuardar = porId("btnGuardar");

function mostrarVistaPrevia(src) {
    vistaPrevia.innerHTML = `<img src="${esc(src)}" alt="Vista previa">`;
}

function llenar(p) {
    for (const campo of ["sku", "nombre", "talla", "color", "precio", "stock", "descripcion"]) {
        form.elements[campo].value = p[campo] ?? "";
    }
    form.elements.sku.disabled = true; // el SKU es la clave del producto y no se cambia
    selCategoria.value = p.idCategoria;
    if (p.imagen) mostrarVistaPrevia(p.imagen);
}

inputImagen.addEventListener("change", () => {
    const archivo = inputImagen.files[0];
    if (archivo) mostrarVistaPrevia(URL.createObjectURL(archivo));
});

form.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    if (!form.checkValidity()) {
        form.classList.add("was-validated");
        return;
    }
    const datos = new FormData(form);
    datos.append("idUsuario", obtenerUsuario()?.idUsuario ?? "");

    btnGuardar.disabled = true;
    try {
        const urlDestino = skuEditar ? `/api/Producto/${encodeURIComponent(skuEditar)}` : "/api/Producto";
        const metodo = skuEditar ? "PUT" : "POST";

        // Usamos fetch nativo para FormData para que el navegador asigne correctamente multipart/form-data
        const respuestaHttp = await fetch(urlDestino, {
            method: metodo,
            body: datos
        });

        const r = await respuestaHttp.json();
        if (!respuestaHttp.ok || !r.isSuccess) {
            throw new Error(r.respuesta || "Error al procesar la solicitud.");
        }

        aviso(r.respuesta);
        setTimeout(() => (location.href = "inventario.html"), 900);
    } catch (error) {
        aviso(error.message, "danger");
        btnGuardar.disabled = false;
    }
});

porId("btnEliminar").addEventListener("click", async () => {
    if (!confirm("¿Seguro que deseas eliminar este producto? Dejará de aparecer en el inventario.")) return;
    try {
        const r = await api(`/api/Producto/${encodeURIComponent(skuEditar)}`, { method: "DELETE" });
        aviso(r.respuesta);
        setTimeout(() => (location.href = "inventario.html"), 900);
    } catch (error) {
        aviso(error.message, "danger");
    }
});

(async function () {
    try {
        const categorias = await api("/api/Producto/categorias");
        selCategoria.innerHTML = '<option value="" selected disabled>Selecciona...</option>' +
            categorias.map((c) => `<option value="${c.idCategoria}">${esc(c.nombre)}</option>`).join("");

        if (skuEditar) {
            porId("tituloForm").textContent = "EDITAR INFORMACIÓN";
            btnGuardar.textContent = "CONFIRMAR";
            porId("btnEliminar").classList.remove("d-none");
            llenar(await api(`/api/Producto/${encodeURIComponent(skuEditar)}`));
        }
    } catch (error) {
        aviso(error.message, "danger");
    }
})();