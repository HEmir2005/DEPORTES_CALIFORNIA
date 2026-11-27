// Utilidades compartidas por todas las páginas internas.
const porId = (id) => document.getElementById(id);

const IMG_VACIA = "data:image/svg+xml;utf8," + encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="80" height="80" viewBox="0 0 80 80"><rect width="80" height="80" rx="10" fill="#e5e7eb"/><circle cx="28" cy="28" r="6" fill="#9ca3af"/><path d="M16 62l18-22 12 14 8-10 10 18z" fill="#9ca3af"/></svg>');

const ETIQUETA_MOV = { VENTA: "Venta", REGISTRO: "Registro", ENTRADA: "Entrada", SALIDA: "Salida" };
const ICONO_MOV = {
    VENTA: "bi-cash-coin text-success",
    REGISTRO: "bi-file-earmark-text text-primary",
    ENTRADA: "bi-box-arrow-in-down text-success",
    SALIDA: "bi-box-arrow-up text-danger"
};

// Todas las llamadas a la API pasan por aquí. Muestra el indicador de carga, bloquea los botones mientras se
// guarda o modifica algo (así no se puede enviar dos veces) y convierte los fallos de red en un mensaje claro.
let peticionesActivas = 0;
async function fetchSeguro(url, opciones = {}) {
    const modifica = (opciones.method || "GET").toUpperCase() !== "GET";
    const bloqueados = [];
    if (!porId("indicadorCarga")) {
        document.body.insertAdjacentHTML("beforeend",
            '<div id="indicadorCarga" class="indicador-carga" role="status" hidden>' +
            '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Cargando...</div>');
    }
    peticionesActivas++;
    porId("indicadorCarga").hidden = false;
    if (modifica) document.querySelectorAll("button:not(:disabled)").forEach((b) => { b.disabled = true; bloqueados.push(b); });
    try {
        return await fetch(url, opciones);
    } catch {
        throw new Error("No se pudo conectar con el servidor. Revisa tu conexión a internet e inténtalo de nuevo.");
    } finally {
        peticionesActivas--;
        if (peticionesActivas === 0) porId("indicadorCarga").hidden = true;
        bloqueados.forEach((b) => { b.disabled = false; });
    }
}

// Llama a la API. Lanza Error si el servidor falla o si la respuesta trae isSuccess = false.
async function api(url, opciones = {}) {
    const respuesta = await fetchSeguro(url, opciones);
    if (respuesta.status === 401) { sesionExpirada(); throw new Error("Tu sesión terminó. Inicia sesión de nuevo."); }
    if (respuesta.status === 403) throw new Error("No tienes permiso para realizar esta acción.");
    let datos = null;
    try { datos = await respuesta.json(); } catch { /* respuesta sin JSON */ }
    if (!respuesta.ok) throw new Error(datos?.respuesta || (respuesta.status >= 500
        ? "El servidor no está disponible en este momento. Inténtalo de nuevo en unos minutos."
        : `No se pudo completar la solicitud (${respuesta.status}).`));
    if (datos && datos.isSuccess === false) throw new Error(datos.respuesta || "La operación no se pudo completar.");
    return datos;
}

function jsonOpts(metodo, cuerpo) {
    return { method: metodo, headers: { "Content-Type": "application/json" }, body: JSON.stringify(cuerpo) };
}

function esc(valor) {
    return String(valor ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" }[c]));
}

const moneda = (v) => new Intl.NumberFormat("es-MX", { style: "currency", currency: "MXN" }).format(v ?? 0);
const fechaCorta = (iso) => new Date(iso).toLocaleDateString("es-MX", { day: "2-digit", month: "2-digit", year: "2-digit" });
const fechaHora = (iso) => new Date(iso).toLocaleString("es-MX", { day: "2-digit", month: "2-digit", year: "2-digit", hour: "2-digit", minute: "2-digit" });
const parametro = (nombre) => new URLSearchParams(location.search).get(nombre);

function imgProducto(p, clase = "", ampliable = false) {
    const zoom = ampliable && p.imagen ? ' data-zoom="1" title="Toca la imagen para ampliarla"' : "";
    return `<img src="${esc(p.imagen || IMG_VACIA)}" alt="${esc(p.nombre)}" class="${clase}"${zoom}>`;
}

// Muestra la imagen completa y grande en una ventana (se abre al tocar una imagen marcada con data-zoom).
function mostrarImagen(src, titulo) {
    if (!porId("modalImagen")) {
        document.body.insertAdjacentHTML("beforeend", `
            <div class="modal fade" id="modalImagen" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="tituloImagen"></h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                        </div>
                        <div class="modal-body text-center"><img id="imagenAmpliada" alt="" class="img-ampliada"></div>
                    </div>
                </div>
            </div>`);
    }
    porId("tituloImagen").textContent = titulo || "";
    porId("imagenAmpliada").src = src;
    porId("imagenAmpliada").alt = titulo || "";
    bootstrap.Modal.getOrCreateInstance(porId("modalImagen")).show();
}

// Si una foto no carga, se muestra la imagen genérica. Se hace aquí (y no con onerror="..." en el HTML)
// porque la política de seguridad del sitio no permite código JavaScript escrito dentro del HTML.
document.addEventListener("error", (e) => {
    const img = e.target;
    if (!(img instanceof HTMLImageElement) || img.src === IMG_VACIA) return;
    img.removeAttribute("data-zoom");
    img.src = IMG_VACIA;
}, true);

document.addEventListener("click", (e) => {
    const img = e.target.closest("img[data-zoom]");
    if (img) mostrarImagen(img.src, img.alt);
});

function leerCarrito() {
    try { return JSON.parse(sessionStorage.getItem(CARRITO_KEY)) || []; } catch { return []; }
}
function guardarCarrito(carrito) {
    sessionStorage.setItem(CARRITO_KEY, JSON.stringify(carrito));
}

function aviso(texto, tipo = "success") {
    let zona = porId("toastZone");
    if (!zona) {
        zona = document.createElement("div");
        zona.id = "toastZone";
        zona.className = "toast-zone";
        document.body.appendChild(zona);
    }
    const el = document.createElement("div");
    el.className = `alert alert-${tipo} mb-0 shadow border-2 border-dark`;
    el.textContent = texto;
    zona.appendChild(el);
    setTimeout(() => el.remove(), 4000);
}


// ---------- Paginación ----------
function paginar(lista, pagina, porPagina) {
    const total = lista.length;
    const paginas = Math.max(1, Math.ceil(total / porPagina));
    const actual = Math.min(Math.max(1, pagina), paginas);
    const inicio = (actual - 1) * porPagina;
    const fin = Math.min(total, inicio + porPagina);
    return { items: lista.slice(inicio, fin), actual, paginas, total, inicio, fin, porPagina };
}

// Dibuja "Mostrando X-Y de Z", el selector "Por página" (opcional) y los botones de página.
function pintarPaginacion(idContenedor, info, alCambiarPagina, opciones = {}) {
    const cont = porId(idContenedor);
    if (!cont) return;
    if (info.total === 0) { cont.innerHTML = ""; return; }

    const botones = [];
    const agregar = (n, texto, estado, etiqueta) => botones.push(
        `<li class="page-item ${estado}"><a class="page-link" href="#" data-pagina="${n}" aria-label="${etiqueta}">${texto}</a></li>`);
    agregar(info.actual - 1, "Anterior", info.actual === 1 ? "disabled" : "", "Página anterior");
    let previo = 0;
    for (let n = 1; n <= info.paginas; n++) {
        if (n === 1 || n === info.paginas || Math.abs(n - info.actual) <= 1) {
            if (n - previo > 1) botones.push('<li class="page-item disabled"><span class="page-link">…</span></li>');
            agregar(n, n, n === info.actual ? "active" : "", `Página ${n}`);
            previo = n;
        }
    }
    agregar(info.actual + 1, "Siguiente", info.actual === info.paginas ? "disabled" : "", "Página siguiente");

    const selector = opciones.tamanos
        ? `<label class="d-flex align-items-center gap-2 mb-0">Por página
               <select class="form-select form-select-sm w-auto" data-por-pagina>
                   ${opciones.tamanos.map((t) => `<option value="${t}" ${t === info.porPagina ? "selected" : ""}>${t}</option>`).join("")}
               </select></label>`
        : "";

    cont.innerHTML = `
        <div class="paginacion d-flex flex-wrap justify-content-between align-items-center gap-3">
            <div class="d-flex flex-wrap align-items-center gap-3">
                <span class="text-muted">Mostrando ${info.inicio + 1}–${info.fin} de ${info.total}</span>${selector}
            </div>
            ${info.paginas > 1 ? `<nav aria-label="Páginas"><ul class="pagination mb-0">${botones.join("")}</ul></nav>` : ""}
        </div>`;

    cont.onclick = (e) => {
        const enlace = e.target.closest("[data-pagina]");
        if (!enlace) return;
        e.preventDefault();
        if (enlace.closest(".disabled, .active")) return;
        alCambiarPagina(Number(enlace.dataset.pagina));
    };
    cont.onchange = (e) => {
        const s = e.target.closest("[data-por-pagina]");
        if (s && opciones.alCambiarTamano) opciones.alCambiarTamano(Number(s.value));
    };
}


// ---------- Validaciones visuales ----------
// Marca un campo como correcto o con error y muestra el mensaje debajo (elemento .invalid-feedback).
// Devuelve true si el campo es válido.
function marcarCampo(input, mensaje) {
    input.classList.toggle("is-invalid", !!mensaje);
    input.classList.toggle("is-valid", !mensaje && input.value !== "" && !input.disabled);
    const feedback = input.parentElement.querySelector(".invalid-feedback");
    if (feedback && mensaje) feedback.textContent = mensaje;
    return !mensaje;
}

// ---------- Ventana de confirmación ----------
// Sustituye al confirm() del navegador. Devuelve una promesa: true si la persona acepta.
function confirmarAccion({ titulo, mensaje, boton = "Aceptar", tipo = "blue" }) {
    return new Promise((resolver) => {
        porId("modalConfirmar")?.remove();
        document.body.insertAdjacentHTML("beforeend", `
            <div class="modal fade" id="modalConfirmar" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header"><h5 class="modal-title fw-bold">${esc(titulo)}</h5></div>
                        <div class="modal-body">${esc(mensaje)}</div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-outline-dark" data-respuesta="no">Cancelar</button>
                            <button type="button" class="btn btn-pill btn-${tipo}" data-respuesta="si">${esc(boton)}</button>
                        </div>
                    </div>
                </div>
            </div>`);
        const elemento = porId("modalConfirmar");
        const modal = new bootstrap.Modal(elemento);
        let respuesta = false;
        elemento.addEventListener("click", (e) => {
            const b = e.target.closest("[data-respuesta]");
            if (b) { respuesta = b.dataset.respuesta === "si"; modal.hide(); }
        });
        elemento.addEventListener("hidden.bs.modal", () => { elemento.remove(); resolver(respuesta); });
        modal.show();
    });
}


// ---------- Sesión y permisos ----------
function sesionExpirada() {
    sessionStorage.removeItem(SESSION_KEY);
    window.location.replace("login.html");
}

// Manda a otra pantalla y allí muestra por qué.
function denegarAcceso(destino = "inventario.html") {
    sessionStorage.setItem("aviso_pendiente", "No tienes permiso para entrar a esa pantalla.");
    window.location.replace(destino);
}

// Sustituye el contenido de la página por un mensaje cuando el registro pedido no existe.
function mostrarNoEncontrado(mensaje, destino = "inventario.html", textoBoton = "Volver al inventario") {
    const contenedor = document.querySelector("main") || document.body;
    contenedor.innerHTML = `
        <div class="form-card text-center py-5">
            <i class="bi bi-search" style="font-size:3rem" aria-hidden="true"></i>
            <h2 class="h4 fw-bold mt-3">No encontrado</h2>
            <p class="mb-4">${esc(mensaje)}</p>
            <a href="${esc(destino)}" class="btn btn-pill btn-blue">${esc(textoBoton)}</a>
        </div>`;
}

const avisoPendiente = sessionStorage.getItem("aviso_pendiente");
if (avisoPendiente) {
    sessionStorage.removeItem("aviso_pendiente");
    aviso(avisoPendiente, "warning");
}
