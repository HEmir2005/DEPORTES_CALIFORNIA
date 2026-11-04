const sku = parametro("sku");
const rutaSku = encodeURIComponent(sku ?? "");
if (!sku) location.replace("inventario.html");

const modalMov = new bootstrap.Modal(porId("modalMov"));
let tipoMov = "ENTRADA";

async function cargar() {
    try {
        const [p, movimientos] = await Promise.all([api(`/api/Producto/${rutaSku}`), api(`/api/Producto/${rutaSku}/movimientos`)]);
        porId("dImagen").innerHTML = imgProducto(p, "detail-img");
        porId("dNombre").textContent = p.nombre;
        porId("dSku").textContent = p.sku;
        porId("dCategoria").textContent = p.categoria;
        porId("dPrecio").textContent = moneda(p.precio);
        porId("dTalla").textContent = p.talla || "-";
        porId("dColor").textContent = p.color || "-";
        porId("dStock").textContent = p.stock;
        porId("dStock").classList.toggle("text-danger", p.stock <= STOCK_BAJO);
        porId("dDescripcion").textContent = p.descripcion || "Sin descripción.";
        porId("btnEditar").href = `producto-form.html?sku=${encodeURIComponent(p.sku)}`;

        porId("tablaMov").innerHTML = movimientos.length
            ? movimientos.map((m) => `<tr>
                <td>${fechaHora(m.fecha)}</td>
                <td><i class="bi ${ICONO_MOV[m.tipo] || ""}"></i> ${ETIQUETA_MOV[m.tipo] || esc(m.tipo)}</td>
                <td class="text-end">${m.cantidad}</td>
                <td class="text-end">${m.stockResultante}</td></tr>`).join("")
            : '<tr><td colspan="4" class="text-muted">Sin movimientos.</td></tr>';
    } catch (error) {
        aviso(error.message, "danger");
    }
}

function abrirMovimiento(tipo) {
    tipoMov = tipo;
    porId("tituloMov").textContent = tipo === "ENTRADA" ? "Registrar entrada" : "Registrar salida";
    porId("cantidadMov").value = "";
    modalMov.show();
    setTimeout(() => porId("cantidadMov").focus(), 300);
}

porId("btnEntrada").addEventListener("click", () => abrirMovimiento("ENTRADA"));
porId("btnSalida").addEventListener("click", () => abrirMovimiento("SALIDA"));

porId("btnGuardarMov").addEventListener("click", async () => {
    const cantidad = Number(porId("cantidadMov").value);
    if (!Number.isInteger(cantidad) || cantidad <= 0) return aviso("Escribe una cantidad entera mayor a cero.", "warning");
    try {
        const r = await api(`/api/Producto/${rutaSku}/movimiento`,
            jsonOpts("POST", { tipo: tipoMov, cantidad, idUsuario: obtenerUsuario()?.idUsuario ?? null }));
        modalMov.hide();
        aviso(r.respuesta);
        cargar();
    } catch (error) {
        aviso(error.message, "danger");
    }
});

cargar();
