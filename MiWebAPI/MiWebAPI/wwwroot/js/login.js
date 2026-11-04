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
    btnLogin.textContent = "INGRESANDO...";

    try {
        const respuesta = await fetch("/api/Auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                NombreUsuario: nombreUsuario.value.trim(),
                Contrasena: password.value
            })
        });

        const resultado = await respuesta.json();
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
        btnLogin.textContent = "ACCEDER";
    }
});
