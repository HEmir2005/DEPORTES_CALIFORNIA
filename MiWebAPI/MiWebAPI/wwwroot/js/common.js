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

// Llama a la API. Lanza Error si el servidor falla o si la respuesta trae isSuccess = false.
async function api(url, opciones = {}) {
    const respuesta = await fetch(url, opciones);
    let datos = null;
    try { datos = await respuesta.json(); } catch { /* respuesta sin JSON */ }
    if (!respuesta.ok) throw new Error(datos?.respuesta || `Error del servidor (${respuesta.status}).`);
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

function imgProducto(p, clase = "") {
    return `<img src="${esc(p.imagen || IMG_VACIA)}" alt="${esc(p.nombre)}" class="${clase}" onerror="this.onerror=null;this.src=IMG_VACIA">`;
}

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
