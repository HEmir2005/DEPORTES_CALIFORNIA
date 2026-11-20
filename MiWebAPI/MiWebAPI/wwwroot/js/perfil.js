const usuario = obtenerUsuario();
const modalUsuario = new bootstrap.Modal(porId("modalUsuario"));
const modalContrasena = new bootstrap.Modal(porId("modalContrasena"));

function pintarPerfil() {
    const u = obtenerUsuario();
    porId("perfilUsuario").textContent = u?.nombre ?? "";
    porId("perfilRol").textContent = u?.rol ?? "";
}


porId("formUsuario").addEventListener("submit", async (e) => {
    e.preventDefault();
    const nuevo = porId("nuevoNombre").value.trim();
    if (!nuevo) return aviso("Escribe el nuevo nombre de usuario.", "warning");
    try {
        const r = await api(API_URL + "/Auth/cambiar-usuario", jsonOpts("POST", {
            nombreActual: usuario.nombre, nuevoNombre: nuevo, contrasena: porId("confirmaContrasena").value
        }));
        guardarUsuario({ ...usuario, nombre: nuevo });
        usuario.nombre = nuevo;
        mostrarUsuario("nombreUsuario");
        pintarPerfil();
        modalUsuario.hide();
        e.target.reset();
        aviso(r.respuesta || "Nombre de usuario actualizado.");
    } catch (error) {
        aviso(error.message, "danger");
    }
});

porId("formContrasena").addEventListener("submit", async (e) => {
    e.preventDefault();
    const nueva = porId("contrasenaNueva").value;
    if (nueva.length < 6) return aviso("La nueva contraseña debe tener al menos 6 caracteres.", "warning");
    if (nueva !== porId("contrasenaRepetida").value) return aviso("Las contraseñas nuevas no coinciden.", "warning");
    try {
        const r = await api(API_URL + "/Auth/cambiar-contrasena", jsonOpts("POST", {
            nombreActual: usuario.nombre, contrasena: porId("contrasenaActual").value, nuevaContrasena: nueva
        }));
        modalContrasena.hide();
        e.target.reset();
        aviso(r.respuesta || "Contraseña actualizada.");
    } catch (error) {
        aviso(error.message, "danger");
    }
});

pintarPerfil();
