// Dibuja el header y la barra inferior en todas las páginas internas y protege la ruta.
// Cada página indica su sección con <body data-nav="home|inventario">.
(function () {
    protegerPagina();
    const activa = document.body.dataset.nav || "";
    const enlace = (href, icono, texto, clave) =>
        `<a href="${href}" class="nav-item ${activa === clave ? "active" : ""}"><i class="bi ${icono}"></i><span>${texto}</span></a>`;

    document.body.insertAdjacentHTML("afterbegin", `
        <header class="figma-header d-flex justify-content-between align-items-center gap-2 px-3 px-md-4">
            <a href="perfil.html" class="d-flex align-items-center gap-2" title="Mi perfil">
                <i class="bi bi-person-circle fs-3"></i>
                <span id="nombreUsuario" class="fw-bold d-none d-sm-inline"></span>
            </a>
            <h1 class="mb-0 fw-bold text-uppercase text-center">INVENTARIO DEPORTES CALIFORNIA</h1>
            <div class="header-logo-container"><img src="img/logo.png" alt="Logo" class="header-logo"></div>
        </header>`);

    document.body.insertAdjacentHTML("beforeend", `
        <nav class="bottom-nav">
            ${enlace("index.html", "bi-house-door-fill", "HOME", "home")}
            ${enlace("inventario.html", "bi-box-seam-fill", "INVENTARIO", "inventario")}
        </nav>`);

    mostrarUsuario("nombreUsuario");
})();
