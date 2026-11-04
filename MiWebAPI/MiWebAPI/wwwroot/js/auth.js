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
    window.location.replace("login.html");
}
