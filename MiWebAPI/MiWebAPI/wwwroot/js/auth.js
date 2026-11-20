function obtenerUsuario() {
    const valor = sessionStorage.getItem(SESSION_KEY);
    try {
        return valor ? JSON.parse(valor) : null;
    } catch {
        sessionStorage.removeItem(SESSION_KEY);
        return null;
    }
}

function guardarUsuario(usuario) {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(usuario));
}

// Solo organiza lo que se muestra: el servidor es quien realmente permite o niega cada acción.
function esAdmin() {
    return obtenerUsuario()?.rol === "Administrador";
}

function protegerPagina() {
    if (!obtenerUsuario()) window.location.replace("login.html");
}

function mostrarUsuario(elementId) {
    const usuario = obtenerUsuario();
    const elemento = document.getElementById(elementId);
    if (usuario && elemento) elemento.textContent = usuario.nombre;
}

function cerrarSesion() {
    sessionStorage.removeItem(SESSION_KEY);
    sessionStorage.removeItem(CARRITO_KEY);
    // También se cierra la sesión del servidor (la cookie).
    fetch(`${API_URL}/Auth/logout`, { method: "POST" })
        .catch(() => { /* aunque falle la red, se sale de la pantalla */ })
        .finally(() => window.location.replace("login.html"));
}
