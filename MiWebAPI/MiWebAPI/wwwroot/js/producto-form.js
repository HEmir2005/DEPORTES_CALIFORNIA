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

const MAX_LADO = 1280;            // píxeles del lado más largo de la foto
const MAX_BYTES = 2 * 1024 * 1024; // límite del servidor

// Las fotos del celular pesan varios MB: se reducen aquí, en el navegador, antes de enviarlas.
async function prepararImagen(archivo) {
    const imagen = await createImageBitmap(archivo, { imageOrientation: "from-image" });
    const escala = Math.min(1, MAX_LADO / Math.max(imagen.width, imagen.height));
    const lienzo = document.createElement("canvas");
    lienzo.width = Math.round(imagen.width * escala);
    lienzo.height = Math.round(imagen.height * escala);
    const ctx = lienzo.getContext("2d");
    ctx.fillStyle = "#fff"; // fondo blanco (el JPG no tiene transparencia)
    ctx.fillRect(0, 0, lienzo.width, lienzo.height);
    ctx.drawImage(imagen, 0, 0, lienzo.width, lienzo.height);
    const blob = await new Promise((resolver) => lienzo.toBlob(resolver, "image/jpeg", 0.85));
    if (!blob) throw new Error("No se pudo procesar la imagen.");
    return new File([blob], archivo.name.replace(/\.[^.]+$/, "") + ".jpg", { type: "image/jpeg" });
}

inputImagen.addEventListener("change", async () => {
    const archivo = inputImagen.files[0];
    if (!archivo) return;
    if (!["image/jpeg", "image/png", "image/webp"].includes(archivo.type)) {
        inputImagen.value = "";
        return aviso("Formato no permitido. Usa JPG, PNG o WEBP.", "warning");
    }
    try {
        let lista = archivo;
        if (typeof DataTransfer !== "undefined" && typeof createImageBitmap !== "undefined") {
            lista = await prepararImagen(archivo);
            const transferencia = new DataTransfer();
            transferencia.items.add(lista);
            inputImagen.files = transferencia.files; // el formulario enviará la foto ya reducida
        }
        if (lista.size > MAX_BYTES) throw new Error("La imagen pesa más de 2 MB. Elige una más ligera.");
        mostrarVistaPrevia(URL.createObjectURL(lista));
    } catch (error) {
        inputImagen.value = "";
        aviso(error.message || "No se pudo preparar la imagen.", "danger");
    }
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
            porId("tituloForm").textContent = "Editar producto";
            btnGuardar.textContent = "Guardar cambios";
            porId("btnEliminar").classList.remove("d-none");
            llenar(await api(`/api/Producto/${encodeURIComponent(skuEditar)}`));
        }
    } catch (error) {
        aviso(error.message, "danger");
    }
})();