const formLogin = document.getElementById("formLogin");
const nombreUsuario = document.getElementById("nombreUsuario");
const password = document.getElementById("password");
const btnLogin = document.getElementById("btnLogin");
const mensajeLogin = document.getElementById("mensajeLogin");

if (obtenerUsuario()) window.location.replace("index.html");

formLogin.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    mensajeLogin.classList.add("d-none");
    btnLogin.disabled = true;
    btnLogin.textContent = "Ingresando...";

    try {
        let respuesta;
        try {
            respuesta = await fetch(API_URL + "/Auth/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    NombreUsuario: nombreUsuario.value.trim(),
                    Contrasena: password.value
                })
            });
        } catch {
            throw new Error("No se pudo conectar con el servidor. Revisa tu conexión a internet e inténtalo de nuevo.");
        }

        let resultado = null;
        try { resultado = await respuesta.json(); } catch { /* sin JSON */ }
        if (!resultado) throw new Error("El servidor no está disponible en este momento. Inténtalo de nuevo en unos minutos.");
        if (!respuesta.ok || !resultado.isSuccess) {
            throw new Error(resultado.respuesta || "No fue posible iniciar sesión.");
        }

        guardarUsuario(resultado.usuario);
        window.location.replace("index.html");
    } catch (error) {
        mensajeLogin.textContent = error.message || "Ocurrió un error al conectar con la API.";
        mensajeLogin.classList.remove("d-none");
    } finally {
        btnLogin.disabled = false;
        btnLogin.textContent = "Acceder";
    }
});
