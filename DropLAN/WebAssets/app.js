const params = new URLSearchParams(location.search);
const pairToken = params.get("token") || "";

const pairView = document.getElementById("pairView");
const appView = document.getElementById("appView");

const cameraInput = document.getElementById("cameraInput");
const galleryInput = document.getElementById("galleryInput");
const filesInput = document.getElementById("filesInput");

const selectedSummary = document.getElementById("selectedSummary");
const uploadButton = document.getElementById("uploadButton");
const uploadProgressWrap = document.getElementById("uploadProgressWrap");
const uploadProgress = document.getElementById("uploadProgress");
const uploadProgressText = document.getElementById("uploadProgressText");
const uploadMessage = document.getElementById("uploadMessage");

let selectedFiles = [];
let eventSource = null;
let lastClipboardFromServer = "";

[cameraInput, galleryInput, filesInput].forEach(input => {
    input.addEventListener("change", () => {
        selectedFiles = Array.from(input.files || []);
        updateSelectionSummary();
    });
});

function updateSelectionSummary() {
    if (!selectedFiles.length) {
        selectedSummary.textContent = "Nic nie wybrano.";
        return;
    }

    const total = selectedFiles.reduce((sum, file) => sum + file.size, 0);

    selectedSummary.textContent =
        selectedFiles.length === 1
            ? `${selectedFiles[0].name} • ${formatBytes(total)}`
            : `${selectedFiles.length} plików • ${formatBytes(total)}`;
}

async function pair() {
    const pin = document.getElementById("pinInput").value.trim();
    const message = document.getElementById("pairMessage");

    message.className = "message";
    message.textContent = "Łączenie…";

    const response = await fetch("/api/pair", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            token: pairToken,
            pin
        })
    });

    if (!response.ok) {
        message.className = "message error";
        message.textContent = "Zły PIN albo nieaktualny kod QR.";
        return;
    }

    history.replaceState({}, "", "/");
    message.className = "message good";
    message.textContent = "Połączono.";

    await boot();
}

async function boot() {
    const response = await fetch("/api/state", {
        cache: "no-store"
    });

    if (response.status === 401) {
        appView.classList.add("hidden");
        pairView.classList.remove("hidden");
        return;
    }

    pairView.classList.add("hidden");
    appView.classList.remove("hidden");

    const state = await response.json();
    renderState(state);

    startRealtime();
}

function startRealtime() {
    if (eventSource)
        eventSource.close();

    eventSource = new EventSource("/events");

    const pill = document.getElementById("connectionPill");

    eventSource.onopen = () => {
        pill.className = "status-pill online";
        pill.textContent = "● Połączono";
    };

    eventSource.onmessage = async () => {
        await refreshState();
    };

    eventSource.onerror = () => {
        pill.className = "status-pill";
        pill.textContent = "Ponowne łączenie…";
    };
}

async function refreshState() {
    const response = await fetch("/api/state", {
        cache: "no-store"
    });

    if (response.status === 401) {
        location.reload();
        return;
    }

    if (!response.ok)
        return;

    const state = await response.json();
    renderState(state);
}

function renderState(state) {
    renderFiles(state.files || []);
    renderHistory(state.history || []);

    const clipboard = document.getElementById("clipboardText");

    if (document.activeElement !== clipboard &&
        state.clipboard !== lastClipboardFromServer) {
        clipboard.value = state.clipboard || "";
    }

    lastClipboardFromServer = state.clipboard || "";
}

function renderFiles(files) {
    const list = document.getElementById("downloadList");

    if (!files.length) {
        list.innerHTML =
            `<div class="message">Na komputerze nie udostępniono jeszcze plików.</div>`;
        return;
    }

    list.innerHTML = files.map(file => `
        <div class="file-row">
            <div>
                <div class="file-name">${escapeHtml(file.name)}</div>
                <div class="file-meta">${formatBytes(file.size)}</div>
                <div id="downloadProgress_${file.id}" class="progress-wrap">
                    <div class="progress-track">
                        <div id="downloadBar_${file.id}" class="progress-bar"></div>
                    </div>
                    <div id="downloadText_${file.id}" class="progress-text">0%</div>
                </div>
            </div>

            <button
                class="download-btn"
                onclick="downloadFile('${file.id}', '${escapeJs(file.name)}')">
                Pobierz
            </button>
        </div>
    `).join("");
}

function renderHistory(items) {
    const list = document.getElementById("historyList");

    if (!items.length) {
        list.innerHTML = `<div class="message">Brak transferów w tej sesji.</div>`;
        return;
    }

    list.innerHTML = items.map(item => {
        const direction =
            item.direction === "PhoneToPc"
                ? "iPhone → PC"
                : "PC → iPhone";

        return `
            <div class="history-row">
                <div class="history-main">
                    <div class="history-name">${escapeHtml(item.fileName)}</div>
                    <div>${escapeHtml(item.status)}</div>
                </div>
                <div class="history-meta">
                    ${direction} • ${formatBytes(item.size)} • ${formatTime(item.time)}
                </div>
            </div>
        `;
    }).join("");
}

function uploadSelected() {
    if (!selectedFiles.length) {
        uploadMessage.className = "message error";
        uploadMessage.textContent = "Najpierw wybierz pliki.";
        return;
    }

    const formData = new FormData();

    for (const file of selectedFiles)
        formData.append("files", file);

    const xhr = new XMLHttpRequest();
    xhr.open("POST", "/upload");

    uploadButton.disabled = true;
    uploadProgressWrap.style.display = "block";
    uploadProgress.style.width = "0%";
    uploadProgressText.textContent = "0%";

    uploadMessage.className = "message";
    uploadMessage.textContent = "Wysyłanie…";

    xhr.upload.onprogress = event => {
        if (!event.lengthComputable)
            return;

        const percent = Math.round(event.loaded / event.total * 100);

        uploadProgress.style.width = `${percent}%`;
        uploadProgressText.textContent =
            `${percent}% • ${formatBytes(event.loaded)} / ${formatBytes(event.total)}`;
    };

    xhr.onload = () => {
        uploadButton.disabled = false;

        if (xhr.status >= 200 && xhr.status < 300) {
            uploadProgress.style.width = "100%";
            uploadProgressText.textContent = "100%";

            uploadMessage.className = "message good";
            uploadMessage.textContent = "Transfer zakończony.";

            selectedFiles = [];
            cameraInput.value = "";
            galleryInput.value = "";
            filesInput.value = "";
            updateSelectionSummary();
        }
        else if (xhr.status === 401) {
            location.reload();
        }
        else {
            uploadMessage.className = "message error";
            uploadMessage.textContent = "Nie udało się wysłać plików.";
        }
    };

    xhr.onerror = () => {
        uploadButton.disabled = false;
        uploadMessage.className = "message error";
        uploadMessage.textContent = "Utracono połączenie z komputerem.";
    };

    xhr.send(formData);
}

function downloadFile(id, fileName) {
    const wrap = document.getElementById(`downloadProgress_${id}`);
    const bar = document.getElementById(`downloadBar_${id}`);
    const text = document.getElementById(`downloadText_${id}`);

    wrap.style.display = "block";

    const xhr = new XMLHttpRequest();
    xhr.open("GET", `/download/${id}`);
    xhr.responseType = "blob";

    xhr.onprogress = event => {
        if (!event.lengthComputable) {
            text.textContent = formatBytes(event.loaded);
            return;
        }

        const percent = Math.round(event.loaded / event.total * 100);

        bar.style.width = `${percent}%`;
        text.textContent =
            `${percent}% • ${formatBytes(event.loaded)} / ${formatBytes(event.total)}`;
    };

    xhr.onload = () => {
        if (xhr.status === 200) {
            bar.style.width = "100%";
            text.textContent = "100%";

            const url = URL.createObjectURL(xhr.response);
            const link = document.createElement("a");

            link.href = url;
            link.download = fileName;

            document.body.appendChild(link);
            link.click();
            link.remove();

            setTimeout(() => URL.revokeObjectURL(url), 30000);
        }
        else if (xhr.status === 401) {
            location.reload();
        }
        else {
            text.textContent = "Błąd pobierania";
        }
    };

    xhr.onerror = () => {
        text.textContent = "Utracono połączenie";
    };

    xhr.send();
}

async function saveClipboard() {
    const text = document.getElementById("clipboardText").value;
    const message = document.getElementById("clipboardMessage");

    const response = await fetch("/api/clipboard", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text })
    });

    if (response.ok) {
        message.className = "message good";
        message.textContent = "Schowek zaktualizowany.";
    }
    else {
        message.className = "message error";
        message.textContent = "Nie udało się zaktualizować schowka.";
    }
}

async function copyClipboard() {
    const textarea = document.getElementById("clipboardText");
    const message = document.getElementById("clipboardMessage");

    try {
        await navigator.clipboard.writeText(textarea.value);
        message.className = "message good";
        message.textContent = "Skopiowano.";
    }
    catch {
        textarea.focus();
        textarea.select();

        try {
            document.execCommand("copy");
            message.className = "message good";
            message.textContent = "Skopiowano.";
        }
        catch {
            message.className = "message error";
            message.textContent = "Safari zablokowało dostęp do schowka.";
        }
    }
}

function formatBytes(bytes) {
    if (!bytes)
        return "0 B";

    const units = ["B", "KB", "MB", "GB", "TB"];
    const index = Math.min(
        Math.floor(Math.log(bytes) / Math.log(1024)),
        units.length - 1);

    const value = bytes / Math.pow(1024, index);

    return `${value.toFixed(index === 0 ? 0 : 1)} ${units[index]}`;
}

function formatTime(value) {
    const date = new Date(value);

    return date.toLocaleTimeString([], {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit"
    });
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function escapeJs(value) {
    return String(value)
        .replaceAll("\\", "\\\\")
        .replaceAll("'", "\\'");
}


function configureInstallExperience() {
    const standalone =
        window.matchMedia("(display-mode: standalone)").matches ||
        window.navigator.standalone === true;

    const isIos =
        /iphone|ipad|ipod/i.test(navigator.userAgent);

    if (isIos && !standalone) {
        document.getElementById("installHint").style.display = "block";
    }

    if ("serviceWorker" in navigator && window.isSecureContext) {
        navigator.serviceWorker
            .register("/sw.js")
            .catch(() => {});
    }
}

configureInstallExperience();

boot();
