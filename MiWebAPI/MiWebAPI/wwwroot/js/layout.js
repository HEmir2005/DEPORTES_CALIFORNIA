// Dibuja el menú superior (navbar de Bootstrap) en todas las páginas internas y protege la ruta.
// En celular y tablet se pliega en un botón de hamburguesa.
// Cada página indica su sección con <body data-nav="home|inventario|ventas|perfil">.
(function () {
    protegerPagina();
    const activa = document.body.dataset.nav || "";
    const enlace = (href, texto, clave) =>
        `<li class="nav-item"><a class="nav-link ${activa === clave ? "active" : ""}" href="${href}"${activa === clave ? ' aria-current="page"' : ""}>${texto}</a></li>`;

    document.body.insertAdjacentHTML("afterbegin", `
        <header class="site-header">
            <nav class="navbar navbar-expand-xl" aria-label="Menú principal">
                <div class="container-xl">
                    <a class="navbar-brand site-brand" href="index.html" title="Ir al inicio">
                        <img src="img/logo.png" alt="Deportes California" class="site-logo">
                        <span class="d-none d-xxl-inline">Sistema de inventario</span>
                    </a>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#menuPrincipal"
                            aria-controls="menuPrincipal" aria-expanded="false" aria-label="Abrir o cerrar el menú">
                        <span class="navbar-toggler-icon"></span>
                    </button>
                    <div class="collapse navbar-collapse" id="menuPrincipal">
                        <ul class="navbar-nav site-nav me-auto mb-2 mb-xl-0">
                            ${enlace("index.html", "Inicio", "home")}
                            ${enlace("inventario.html", "Inventario", "inventario")}
                            ${enlace("registrar-venta.html", "Registrar venta", "ventas")}
                            ${enlace("ventas.html", "Historial de ventas", "historial")}
                            ${enlace("perfil.html", "Mi perfil", "perfil")}
                        </ul>
                        <div class="site-user d-flex flex-column flex-xl-row align-items-xl-center gap-2 gap-xl-3 pb-2 pb-xl-0">
                            <span><i class="bi bi-person-circle" aria-hidden="true"></i> <span id="nombreUsuario"></span> <span class="badge text-bg-secondary ms-1" id="rolUsuario"></span></span>
                            <button type="button" id="btnSalir" class="btn-salir">Cerrar sesión</button>
                        </div>
                    </div>
                </div>
            </nav>
        </header>`);

    document.getElementById("btnSalir").addEventListener("click", cerrarSesion);
    mostrarUsuario("nombreUsuario");
    document.getElementById("rolUsuario").textContent = obtenerUsuario()?.rol ?? "";

    // Al volver con el botón "Atrás" el navegador puede mostrar una página guardada: se vuelve a revisar la sesión.
    window.addEventListener("pageshow", (e) => { if (e.persisted) protegerPagina(); });
})();
