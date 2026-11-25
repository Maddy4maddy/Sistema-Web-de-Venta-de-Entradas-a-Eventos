(() => {
    const areaMovil = document.getElementById("lectorQrMovil");
    if (!areaMovil) return;

    const pantallaMovil = window.matchMedia("(max-width: 767.98px) and (pointer: coarse)");
    const iniciar = document.getElementById("iniciarLectorQr");
    const detener = document.getElementById("detenerLectorQr");
    const leerQr = document.getElementById("leerQrEnfocado");
    const camara = document.getElementById("camaraQr");
    const video = document.getElementById("videoQr");
    const estado = document.getElementById("estadoLectorQr");
    const formulario = document.getElementById("formularioValidacionEntrada");
    const codigo = document.getElementById("Codigo");
    const confirmacion = document.getElementById("confirmacionValidacion");
    const codigoConfirmacion = document.getElementById("codigoConfirmacion");
    const confirmar = document.getElementById("confirmarValidacion");
    const cancelar = document.getElementById("cancelarValidacion");
    let flujo = null;
    let detector = null;
    let intento = 0;
    let confirmada = false;

    function mostrarEstado(mensaje, error = false) {
        estado.textContent = mensaje;
        estado.className = error ? "mt-2 mb-0 text-danger" : "mt-2 mb-0 text-muted";
        estado.hidden = !mensaje;
    }

    function cerrarCamara() {
        intento++;
        if (flujo) {
            flujo.getTracks().forEach(track => track.stop());
            flujo = null;
        }
        detector = null;
        video.srcObject = null;
        camara.hidden = true;
        leerQr.disabled = true;
        iniciar.disabled = false;
        iniciar.hidden = false;
    }

    iniciar.addEventListener("click", async () => {
        if (!pantallaMovil.matches) return;

        mostrarEstado("");
        if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) {
            mostrarEstado("La cámara requiere abrir esta página con HTTPS. Puedes escribir el código manualmente.", true);
            return;
        }

        if (!window.BarcodeDetector) {
            mostrarEstado("Este navegador no permite leer códigos QR. Puedes escribir el código manualmente.", true);
            return;
        }

        iniciar.disabled = true;
        const idIntento = ++intento;

        try {
            const formatos = await BarcodeDetector.getSupportedFormats();
            if (!formatos.includes("qr_code")) {
                throw new Error("formato_no_disponible");
            }

            detector = new BarcodeDetector({ formats: ["qr_code"] });
            const nuevoFlujo = await navigator.mediaDevices.getUserMedia({
                audio: false,
                video: { facingMode: { ideal: "environment" } }
            });

            if (idIntento !== intento || !pantallaMovil.matches) {
                nuevoFlujo.getTracks().forEach(track => track.stop());
                return;
            }

            flujo = nuevoFlujo;
            video.srcObject = flujo;
            camara.hidden = false;
            iniciar.hidden = true;
            await video.play();
            if (idIntento !== intento) return;

            leerQr.disabled = false;
            mostrarEstado("Encuadra el QR de la entrada del asistente y pulsa «Leer QR enfocado» cuando esté listo.");
        } catch (error) {
            if (idIntento !== intento) return;
            cerrarCamara();
            const mensaje = error.name === "NotAllowedError"
                ? "No se autorizó la cámara. Permite el acceso o escribe el código manualmente."
                : "No se pudo abrir o utilizar el lector QR en este navegador. Escribe el código manualmente.";
            mostrarEstado(mensaje, true);
        }
    });

    leerQr.addEventListener("click", async () => {
        if (!flujo || !detector || !pantallaMovil.matches) return;

        const idIntento = intento;
        leerQr.disabled = true;
        codigo.value = "";
        codigo.dispatchEvent(new Event("input", { bubbles: true }));
        mostrarEstado("Leyendo el QR enfocado...");

        try {
            const resultados = await detector.detect(video);
            if (idIntento !== intento) return;

            if (resultados.length === 0) {
                mostrarEstado("No se encontró un QR. Enfócalo y vuelve a pulsar «Leer QR enfocado».", true);
                return;
            }

            if (resultados.length > 1) {
                mostrarEstado("Hay varios QR visibles. Deja solo la entrada que quieres leer y vuelve a pulsar el botón.", true);
                return;
            }

            const valor = resultados[0].rawValue.trim().toUpperCase();
            if (!/^ENT-[A-F0-9]{32}$/.test(valor)) {
                mostrarEstado("Ese QR no contiene un código de entrada válido. Enfoca otra entrada y vuelve a intentarlo.", true);
                return;
            }

            cerrarCamara();
            codigo.value = valor;
            codigo.dispatchEvent(new Event("input", { bubbles: true }));
            mostrarEstado(`Código copiado: ${valor}. Revísalo y pulsa «Validar entrada» cuando estés listo.`);
        } catch {
            if (idIntento !== intento) {
                return;
            }
            cerrarCamara();
            mostrarEstado("No se pudo leer el QR. Puedes escribir el código manualmente.", true);
        } finally {
            if (idIntento === intento) leerQr.disabled = false;
        }
    });

    detener.addEventListener("click", () => {
        cerrarCamara();
        mostrarEstado("");
    });

    codigo.addEventListener("input", () => {
        confirmacion.hidden = true;
        confirmada = false;
    });

    formulario.addEventListener("submit", evento => {
        if (confirmada) return;

        evento.preventDefault();
        if (window.jQuery && !window.jQuery(formulario).valid()) return;

        const valor = codigo.value.trim().toUpperCase();
        if (!/^ENT-[A-F0-9]{32}$/.test(valor)) {
            const mensaje = formulario.querySelector('[data-valmsg-for="Codigo"]');
            if (mensaje) mensaje.textContent = "Ingresa un código de entrada válido.";
            return;
        }

        codigo.value = valor;
        codigoConfirmacion.textContent = valor;
        confirmacion.hidden = false;
        confirmar.focus();
    });

    confirmar.addEventListener("click", () => {
        if (codigo.value.trim().toUpperCase() !== codigoConfirmacion.textContent) {
            confirmacion.hidden = true;
            return;
        }

        confirmada = true;
        confirmar.disabled = true;
        formulario.requestSubmit();
    });

    cancelar.addEventListener("click", () => {
        confirmacion.hidden = true;
        codigo.focus();
    });

    pantallaMovil.addEventListener("change", () => {
        if (!pantallaMovil.matches) cerrarCamara();
    });
    document.addEventListener("visibilitychange", () => {
        if (document.hidden) cerrarCamara();
    });
    window.addEventListener("pagehide", cerrarCamara);
})();
