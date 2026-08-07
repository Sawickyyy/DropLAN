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
const languageSelect = document.getElementById("languageSelect");

const translations = {
    pl: {
        pairTitle: "Połącz z DropLAN",
        pairHint: "Wpisz 6-cyfrowy PIN wyświetlany na komputerze.",
        pairButton: "Połącz urządzenie",
        brandSubtitle: "Transfer bez chmury, prosto po LAN",
        connecting: "Łączenie…",
        connected: "● Połączono",
        reconnecting: "Ponowne łączenie…",
        installTitle: "Dodaj DropLAN do ekranu początkowego",
        installHint: "Na iPhonie: Udostępnij → Dodaj do ekranu początkowego. Potem DropLAN otwiera się jak osobna aplikacja.",
        sendToComputer: "Wyślij na komputer",
        sendHint: "Zdjęcia, filmy, dokumenty albo dowolne pliki.",
        camera: "📷 Aparat",
        gallery: "🖼️ Galeria",
        files: "📁 Pliki",
        nothingSelected: "Nic nie wybrano.",
        oneFile: "1 plik",
        manyFiles: "plików",
        sendButton: "Wyślij",
        clipboardTitle: "Schowek",
        clipboardHint: "Przerzuć tekst między iPhonem i Windowsem.",
        clipboardPlaceholder: "Wklej tekst…",
        shareClipboard: "Udostępnij",
        copyClipboard: "Kopiuj",
        computerFiles: "Pliki z komputera",
        computerFilesHint: "Lista aktualizuje się automatycznie, bez odświeżania strony.",
        recentTransfers: "Ostatnie transfery",
        recentTransfersHint: "Historia bieżącej sesji.",
        badPin: "Zły PIN albo nieaktualny kod QR.",
        paired: "Połączono.",
        noComputerFiles: "Na komputerze nie udostępniono jeszcze plików.",
        download: "Pobierz",
        noTransfers: "Brak transferów w tej sesji.",
        chooseFilesFirst: "Najpierw wybierz pliki.",
        uploading: "Wysyłanie…",
        uploadComplete: "Transfer zakończony.",
        uploadFailed: "Nie udało się wysłać plików.",
        connectionLost: "Utracono połączenie z komputerem.",
        downloadError: "Błąd pobierania",
        connectionLostShort: "Utracono połączenie",
        clipboardUpdated: "Schowek zaktualizowany.",
        clipboardUpdateFailed: "Nie udało się zaktualizować schowka.",
        copied: "Skopiowano.",
        safariClipboardBlocked: "Safari zablokowało dostęp do schowka."
    },
    en: {
        pairTitle: "Connect to DropLAN",
        pairHint: "Enter the 6-digit PIN shown on your computer.",
        pairButton: "Connect device",
        brandSubtitle: "Cloud-free transfer over your local network",
        connecting: "Connecting…",
        connected: "● Connected",
        reconnecting: "Reconnecting…",
        installTitle: "Add DropLAN to your Home Screen",
        installHint: "On iPhone: Share → Add to Home Screen. DropLAN will then open like a standalone app.",
        sendToComputer: "Send to computer",
        sendHint: "Photos, videos, documents or any other files.",
        camera: "📷 Camera",
        gallery: "🖼️ Gallery",
        files: "📁 Files",
        nothingSelected: "Nothing selected.",
        oneFile: "1 file",
        manyFiles: "files",
        sendButton: "Send",
        clipboardTitle: "Clipboard",
        clipboardHint: "Move text between your iPhone and Windows.",
        clipboardPlaceholder: "Paste text…",
        shareClipboard: "Share",
        copyClipboard: "Copy",
        computerFiles: "Files from computer",
        computerFilesHint: "The list updates automatically without refreshing the page.",
        recentTransfers: "Recent transfers",
        recentTransfersHint: "History for the current session.",
        badPin: "Incorrect PIN or expired QR code.",
        paired: "Connected.",
        noComputerFiles: "No files have been shared from the computer yet.",
        download: "Download",
        noTransfers: "No transfers in this session.",
        chooseFilesFirst: "Choose files first.",
        uploading: "Uploading…",
        uploadComplete: "Transfer complete.",
        uploadFailed: "Could not upload the files.",
        connectionLost: "Connection to the computer was lost.",
        downloadError: "Download error",
        connectionLostShort: "Connection lost",
        clipboardUpdated: "Clipboard updated.",
        clipboardUpdateFailed: "Could not update the clipboard.",
        copied: "Copied.",
        safariClipboardBlocked: "Safari blocked clipboard access."
    }
};

let currentLanguage = localStorage.getItem("droplan_lang") || "pl";
if (!translations[currentLanguage])
    currentLanguage = "pl";

let selectedFiles = [];
let eventSource = null;
let lastClipboardFromServer = "";

function t(key) {
    return translations[currentLanguage][key] || translations.pl[key] || key;
}

function applyLanguage(language) {
    currentLanguage = translations[language] ? language : "pl";
    localStorage.setItem("droplan_lang", currentLanguage);
    document.documentElement.lang = currentLanguage;

    document.querySelectorAll("[data-i18n]").forEach(element => {
        const key = element.dataset.i18n;
        element.textContent = t(key);
    });

    document.querySelectorAll("[data-i18n-placeholder]").forEach(element => {
        const key = element.dataset.i18nPlaceholder;
        element.placeholder = t(key);
    });

    if (languageSelect)
        languageSelect.value = currentLanguage;

    updateSelectionSummary();

    const pill = document.getElementById("connectionPill");
    if (pill) {
        pill.textContent = pill.classList.contains("online")
            ? t("connected")
            : t("connecting");
    }
}

if (languageSelect) {
    languageSelect.addEventListener("change", () => {
        applyLanguage(languageSelect.value);
        refreshState();
    });
}

[cameraInput, galleryInput, filesInput].forEach(input => {
    input.addEventListener("change", () => {
        selectedFiles = Array.from(input.files || []);
        updateSelectionSummary();
    });
});

function updateSelectionSummary() {
    if (!selectedSummary)
        return;

    if (!selectedFiles.length) {
        selectedSummary.textContent = t("nothingSelected");
        return;
    }

    const total = selectedFiles.reduce((sum, file) => sum + file.size, 0);

    selectedSummary.textContent = selectedFiles.length === 1
        ? `${selectedFiles[0].name} • ${formatBytes(total)}`
        : `${selectedFiles.length} ${t("manyFiles")} • ${formatBytes(total)}`;
}

async function pair() {
    const pin = document.getElementById("pinInput").value.trim();
    const message = document.getElementById("pairMessage");

    message.className = "message";
    message.textContent = t("connecting");

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
        message.textContent = t("badPin");
        return;
    }

    history.replaceState({}, "", "/");
    message.className = "message good";
    message.textContent = t("paired");

    await boot();
}

async function boot() {
    applyLanguage(currentLanguage);

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
        pill.textContent = t("connected");
    };

    eventSource.onmessage = async () => {
        await refreshState();
    };

    eventSource.onerror = () => {
        pill.className = "status-pill";
        pill.textContent = t("reconnecting");
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
        list.innerHTML = `<div class="message">${escapeHtml(t("noComputerFiles"))}</div>`;
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
                ${escapeHtml(t("download"))}
            </button>
        </div>
    `).join("");
}

function renderHistory(items) {
    const list = document.getElementById("historyList");

    if (!items.length) {
        list.innerHTML = `<div class="message">${escapeHtml(t("noTransfers"))}</div>`;
        return;
    }

    list.innerHTML = items.map(item => {
        const direction = item.direction === "PhoneToPc"
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
        uploadMessage.textContent = t("chooseFilesFirst");
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
    uploadMessage.textContent = t("uploading");

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
            uploadMessage.textContent = t("uploadComplete");

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
            uploadMessage.textContent = t("uploadFailed");
        }
    };

    xhr.onerror = () => {
        uploadButton.disabled = false;
        uploadMessage.className = "message error";
        uploadMessage.textContent = t("connectionLost");
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
            text.textContent = t("downloadError");
        }
    };

    xhr.onerror = () => {
        text.textContent = t("connectionLostShort");
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
        message.textContent = t("clipboardUpdated");
    }
    else {
        message.className = "message error";
        message.textContent = t("clipboardUpdateFailed");
    }
}

async function copyClipboard() {
    const textarea = document.getElementById("clipboardText");
    const message = document.getElementById("clipboardMessage");

    try {
        await navigator.clipboard.writeText(textarea.value);
        message.className = "message good";
        message.textContent = t("copied");
    }
    catch {
        textarea.focus();
        textarea.select();

        try {
            document.execCommand("copy");
            message.className = "message good";
            message.textContent = t("copied");
        }
        catch {
            message.className = "message error";
            message.textContent = t("safariClipboardBlocked");
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

    return date.toLocaleTimeString(currentLanguage === "pl" ? "pl-PL" : "en-GB", {
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

    const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);

    if (isIos && !standalone) {
        document.getElementById("installHint").style.display = "block";
    }

    if ("serviceWorker" in navigator && window.isSecureContext) {
        navigator.serviceWorker
            .register("/sw.js")
            .catch(() => {});
    }
}

applyLanguage(currentLanguage);
configureInstallExperience();
boot();
