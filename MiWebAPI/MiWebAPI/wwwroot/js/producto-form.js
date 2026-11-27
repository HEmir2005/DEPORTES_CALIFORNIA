// Sirve para registrar (sin ?sku) y para editar (?sku=20000001) un producto.
const form = porId("formProducto");
const skuEditar = parametro("sku"); // sin ?sku = registrar; con ?sku = editar
const selCategoria = porId("idCategoria");
const inputImagen = porId("imagen");
const vistaPrevia = porId("imgPreview");
const btnGuardar = porId("btnGuardar");
const errorForm = porId("errorForm");

// Solo el administrador agrega, edita o da de baja productos.
if (!esAdmin()) denegarAcceso("inventario.html");

// ---------- Validaciones: cada una devuelve el mensaje de error, o "" si el dato es correcto ----------
const validadores = {
    sku: (v) => {
        if (skuEditar) return ""; // el código no se cambia al editar
        if (v === "") return "Escribe el código del producto.";
        if (!/^[A-Za-z0-9._-]+$/.test(v)) return "Usa solo letras, números, punto, guion o guion bajo (sin espacios).";
        if (v.length > 30) return "El código no puede pasar de 30 caracteres.";
        return "";
    },
    nombre: (v) => (v === "" ? "Escribe el nombre del producto." : v.length > 150 ? "El nombre no puede pasar de 150 caracteres." : ""),
    idCategoria: (v) => (v === "" ? "Selecciona una categoría." : ""),
    talla: (v) => (v.length > 10 ? "La talla no puede pasar de 10 caracteres." : ""),
    color: (v) => (v.length > 30 ? "El color no puede pasar de 30 caracteres." : ""),
    precio: (v) => {
        if (v === "") return "Escribe el precio.";
        const n = Number(v);
        if (!(n > 0)) return "El precio debe ser mayor a cero.";
        if (n > 99999999) return "El precio es demasiado alto.";
        if (!/^\d+(\.\d{1,2})?$/.test(v)) return "El precio solo puede tener dos decimales.";
        return "";
    },
    stock: (v) => {
        if (v === "") return "Escribe las unidades (0 si no hay).";
        const n = Number(v);
        if (!Number.isInteger(n) || n < 0) return "Las unidades deben ser un número entero, 0 o mayor.";
        if (n > 100000) return "Las unidades no pueden pasar de 100,000.";
        return "";
    }
};

function validarCampo(nombre) {
    const campo = form.elements[nombre];
    return marcarCampo(campo, validadores[nombre](campo.value.trim()));
}

for (const nombre of Object.keys(validadores)) {
    const campo = form.elements[nombre];
    campo.addEventListener("blur", () => validarCampo(nombre));
    campo.addEventListener(campo.tagName === "SELECT" ? "change" : "input", () => {
        if (campo.classList.contains("is-invalid")) validarCampo(nombre); // se corrige en cuanto se escribe bien
    });
}

function mostrarError(texto) {
    errorForm.innerHTML = `<i class="bi bi-exclamation-triangle-fill" aria-hidden="true"></i> ${esc(texto)}`;
    errorForm.classList.remove("d-none");
    errorForm.scrollIntoView({ behavior: "smooth", block: "center" });
}

// ---------- Imagen ----------
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

const LADO_FOTO = 800;              // todas las fotos quedan cuadradas de 800 x 800 píxeles
const MAX_BYTES = 2 * 1024 * 1024; // límite del servidor

// Formato único para todas las fotos: un cuadrado de 800 x 800 con fondo blanco.
// La foto se escala completa (sin recortarla ni estirarla) y se centra; el espacio sobrante queda en blanco.
// Así todas se ven del mismo tamaño en el inventario, el detalle y las ventas, sin deformarse.
async function prepararImagen(archivo) {
    const imagen = await createImageBitmap(archivo, { imageOrientation: "from-image" });
    const escala = Math.min(LADO_FOTO / imagen.width, LADO_FOTO / imagen.height);
    const ancho = Math.round(imagen.width * escala);
    const alto = Math.round(imagen.height * escala);
    const lienzo = document.createElement("canvas");
    lienzo.width = LADO_FOTO;
    lienzo.height = LADO_FOTO;
    const ctx = lienzo.getContext("2d");
    ctx.fillStyle = "#fff"; // fondo blanco (el JPG no tiene transparencia)
    ctx.fillRect(0, 0, LADO_FOTO, LADO_FOTO);
    ctx.imageSmoothingQuality = "high";
    ctx.drawImage(imagen, Math.round((LADO_FOTO - ancho) / 2), Math.round((LADO_FOTO - alto) / 2), ancho, alto);
    imagen.close?.();
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

// ---------- Pantalla de confirmación (igual que la de venta) ----------
function mostrarExito(titulo, mensaje, producto) {
    porId("exitoTitulo").textContent = titulo;
    porId("exitoMensaje").textContent = mensaje;
    const ficha = porId("exitoFicha");
    const verProducto = porId("exitoVer");
    ficha.classList.toggle("d-none", !producto);
    verProducto.classList.toggle("d-none", !producto);
    if (producto) {
        porId("exitoImagen").innerHTML = imgProducto(producto, "table-img", true);
        porId("exitoNombre").textContent = producto.nombre;
        porId("exitoDatos").innerHTML =
            `Código: <b>${esc(producto.sku)}</b> | ${esc(producto.categoria)}<br>` +
            `Talla: ${esc(producto.talla) || "-"} | Color: ${esc(producto.color) || "-"}<br>` +
            `Precio: <b>${moneda(producto.precio)}</b> | Stock: <b>${producto.stock}</b>`;
        verProducto.href = `detalle-producto.html?sku=${encodeURIComponent(producto.sku)}`;
    }
    new bootstrap.Modal(porId("modalExito")).show();
}

// ---------- Guardar ----------
form.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    errorForm.classList.add("d-none");

    const conError = Object.keys(validadores).filter((nombre) => !validarCampo(nombre));
    if (conError.length) {
        mostrarError(conError.length === 1
            ? "Revisa el campo marcado en rojo."
            : `Revisa los ${conError.length} campos marcados en rojo.`);
        form.elements[conError[0]].focus();
        return;
    }

    const datos = new FormData(form);
    datos.append("idUsuario", obtenerUsuario()?.idUsuario ?? "");

    btnGuardar.disabled = true;
    btnGuardar.textContent = "Guardando...";
    try {
        const urlDestino = skuEditar ? `${API_URL}/Producto/${encodeURIComponent(skuEditar)}` : API_URL + "/Producto";
        // fetch directo para que el navegador arme el multipart/form-data con su separador
        const respuestaHttp = await fetchSeguro(urlDestino, { method: skuEditar ? "PUT" : "POST", body: datos });
        if (respuestaHttp.status === 401) { sesionExpirada(); return; }
        if (respuestaHttp.status === 403) throw new Error("No tienes permiso para realizar esta acción.");
        let r = null;
        try { r = await respuestaHttp.json(); } catch { /* sin JSON */ }
        if (!r) throw new Error("El servidor no respondió como se esperaba. Inténtalo de nuevo.");
        if (!respuestaHttp.ok || !r.isSuccess) throw new Error(r.respuesta || "Error al procesar la solicitud.");

        // Se consulta el producto guardado para mostrarlo en la pantalla de confirmación.
        const sku = skuEditar || String(datos.get("sku")).trim();
        let guardado = null;
        try { guardado = await api(`${API_URL}/Producto/${encodeURIComponent(sku)}`); } catch { /* se muestra sin ficha */ }
        mostrarExito(
            skuEditar ? "Cambios guardados" : "Producto registrado",
            skuEditar ? "El producto se actualizó correctamente." : "El producto ya aparece en el inventario.",
            guardado);
    } catch (error) {
        mostrarError(error.message);
        btnGuardar.disabled = false;
        btnGuardar.textContent = skuEditar ? "Guardar cambios" : "Guardar";
    }
});

// ---------- Dar de baja ----------
porId("btnEliminar").addEventListener("click", async () => {
    const acepta = await confirmarAccion({
        titulo: "Dar de baja el producto",
        mensaje: "El producto dejará de aparecer en el inventario y en las ventas, pero se conserva su historial. " +
                 "Podrás reactivarlo desde Inventario > Dados de baja.",
        boton: "Dar de baja",
        tipo: "red"
    });
    if (!acepta) return;
    try {
        const r = await api(`${API_URL}/Producto/${encodeURIComponent(skuEditar)}`, { method: "DELETE" });
        mostrarExito("Producto dado de baja", r.respuesta || "El producto ya no aparece en el inventario.", null);
    } catch (error) {
        mostrarError(error.message);
    }
});

(async function () {
    try {
        const categorias = await api(API_URL + "/Producto/categorias");
        selCategoria.innerHTML = '<option value="" selected>Selecciona una categoría...</option>' +
            categorias.map((c) => `<option value="${c.idCategoria}">${esc(c.nombre)}</option>`).join("");

        if (skuEditar) {
            porId("tituloForm").textContent = "Editar producto";
            btnGuardar.textContent = "Guardar cambios";
            porId("btnEliminar").classList.remove("d-none");
            llenar(await api(`${API_URL}/Producto/${encodeURIComponent(skuEditar)}`));
        }
    } catch (error) {
        if (/no encontrado/i.test(error.message)) return mostrarNoEncontrado("Ese producto no existe o está dado de baja.");
        aviso(error.message, "danger");
    }
})();
