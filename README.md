<div align="center">

# DropLAN

### Fast, private, cloud-free file transfer over your local network.

Transfer files, photos, videos and clipboard text directly between your **Windows PC and phone**.

No account. No cloud upload. No subscription. No cable.

<br>

![Version](https://img.shields.io/badge/version-0.5.5-2563eb)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![WPF](https://img.shields.io/badge/UI-WPF-5C2D91)
![ASP.NET Core](https://img.shields.io/badge/server-ASP.NET%20Core-512BD4)
![PWA](https://img.shields.io/badge/mobile-PWA-success)
![License](https://img.shields.io/badge/status-active%20development-orange)

<br>

**Windows ↔ Phone · Local LAN · QR pairing · Clipboard · PWA**

</div>

---

## What is DropLAN?

**DropLAN** is a Windows application that lets you transfer files directly between your PC and phone using your local Wi-Fi or LAN.

Instead of uploading files to a cloud service and downloading them again on a device sitting two metres away, DropLAN turns your Windows computer into a lightweight local transfer server.

Your phone connects through its browser.

```text
Windows PC
    │
    │  Local Wi-Fi / LAN
    │
    ▼
Phone Browser
```

Everything happens directly inside your local network.

---

## Why DropLAN?

Moving a file between your own devices should not require:

* creating another account,
* uploading data to somebody else's server,
* installing a mobile app,
* sending yourself an email,
* opening a messaging app,
* finding a USB cable,
* waiting for cloud synchronization.

DropLAN keeps the process local and simple.

> **Your local files should not need to travel across the Internet just to move across the room.**

---

## Features

### 🚀 Direct LAN transfers

Files move directly between devices connected to the same local network.

No DropLAN cloud server sits between them.

---

### 📱 Phone → Windows

Send content directly from your phone to your PC.

Supported mobile inputs include:

* 📷 Camera
* 🖼️ Gallery
* 📁 Files

Send:

* photos,
* videos,
* documents,
* archives,
* audio,
* and other files supported by your browser.

The DropLAN server is currently configured for request bodies up to approximately **16 GB**.

---

### 💻 Windows → Phone

Select files on your computer and make them immediately available to your phone.

The mobile interface updates automatically when the Windows file list changes.

No manual page refresh required.

---

### 📋 Shared clipboard

Move text between Windows and your phone.

Useful for:

* links,
* notes,
* addresses,
* code snippets,
* commands,
* tracking numbers,
* messages,
* other text.

---

### 🔐 QR + PIN pairing

Connecting a phone takes seconds.

1. Start DropLAN.
2. Scan the QR code.
3. Enter the six-digit PIN displayed on the PC.
4. Start transferring.

Each session uses a randomly generated session token and PIN.

Generating a new session invalidates the previous one.

---

### 🔄 Real-time updates

DropLAN uses **Server-Sent Events (SSE)** for live state synchronization.

Changes such as:

* new files,
* removed files,
* clipboard updates,
* transfer activity,
* session changes

can appear on the mobile interface automatically.

---

### 📲 Progressive Web App

The mobile interface can be opened directly in a browser.

No native phone app is required.

On supported devices, DropLAN can also be added to the Home Screen and launched like a standalone web app.

---

### 🖥️ System tray

DropLAN can remain active in the Windows notification area.

Quick tray actions include:

* Open DropLAN
* Copy address
* Start a new session
* Open receive folder
* Exit

The local server can continue running even when the main window is closed.

---

### 🔔 Notifications

DropLAN can notify you when files arrive from another device.

---

### 🌍 Polish & English

Both the Windows interface and mobile web application support:

* 🇬🇧 English
* 🇵🇱 Polish

---

### 🔄 Automatic updates

DropLAN uses **Velopack** and **GitHub Releases** for application updates.

The application supports:

* update checks,
* update downloads,
* progress display,
* installation,
* restart into the new version.

---

# How it works

```text
┌──────────────────────────────────────┐
│              Windows PC              │
│                                      │
│   ┌──────────────────────────────┐   │
│   │       DropLAN WPF UI         │   │
│   └──────────────┬───────────────┘   │
│                  │                   │
│   ┌──────────────▼───────────────┐   │
│   │        Shared State          │   │
│   └──────────────┬───────────────┘   │
│                  │                   │
│   ┌──────────────▼───────────────┐   │
│   │ ASP.NET Core + Kestrel       │   │
│   │ http://0.0.0.0:5050          │   │
│   └──────────────┬───────────────┘   │
│                  │                   │
└──────────────────┼───────────────────┘
                   │
                   │ Local Wi-Fi / LAN
                   │
            ┌──────▼───────┐
            │    Phone     │
            │              │
            │ Browser/PWA  │
            └──────────────┘
```

The Windows application is both:

* the desktop interface,
* the local transfer server.

No central DropLAN infrastructure is required for local transfers.

---

# Quick start

## 1. Start DropLAN

Launch DropLAN on your Windows computer.

The application starts its local server on:

```text
TCP port 5050
```

---

## 2. Connect your phone

Make sure your PC and phone are connected to the same local network.

Scan the QR code shown inside DropLAN.

---

## 3. Enter the PIN

The phone will ask for the six-digit PIN displayed on your PC.

Enter it to authorize the device.

---

## 4. Transfer

You can now:

```text
Phone → Windows
Windows → Phone
Phone ↔ Windows clipboard
```

---

# Installation

## Download a release

Pre-built Windows releases are distributed through **GitHub Releases**.

Open the repository Releases section and download the latest DropLAN installer.

> Windows x64 is currently the primary target.

---

# Build from source

## Requirements

To build DropLAN yourself you need:

* Windows
* .NET 10 SDK
* Git

Visual Studio with .NET desktop development support is recommended.

---

## Clone

```powershell
git clone https://github.com/Sawickyyy/DropLAN.git
cd DropLAN
```

---

## Restore

```powershell
dotnet restore DropLAN/DropLAN.csproj
```

---

## Build

```powershell
dotnet build DropLAN/DropLAN.csproj
```

---

## Run

```powershell
dotnet run --project DropLAN/DropLAN.csproj
```

---

## Publish self-contained Windows build

```powershell
dotnet publish DropLAN/DropLAN.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o publish
```

---

# Technology stack

| Component           | Technology                       |
| ------------------- | -------------------------------- |
| Desktop application | C# / WPF                         |
| Runtime             | .NET 10                          |
| Local server        | ASP.NET Core                     |
| HTTP server         | Kestrel                          |
| Mobile client       | HTML / CSS / JavaScript          |
| Realtime updates    | Server-Sent Events               |
| QR generation       | QRCoder                          |
| Updates             | Velopack                         |
| Releases            | GitHub Actions / GitHub Releases |
| Mobile experience   | PWA                              |

---

# Project structure

```text
DropLAN/
│
├── .github/
│   └── workflows/
│       └── release.yml
│
├── DropLAN/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── AppSettings.cs
│   ├── DropLAN.csproj
│   ├── LocalServer.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── Models.cs
│   ├── NetworkHelper.cs
│   ├── RealtimeBroker.cs
│   │
│   ├── Localization/
│   │
│   ├── Routes/
│   │   ├── ClipboardRoutes.cs
│   │   ├── DownloadRoutes.cs
│   │   ├── EventRoutes.cs
│   │   ├── PairRoutes.cs
│   │   ├── PwaRoutes.cs
│   │   ├── StateRoutes.cs
│   │   └── UploadRoutes.cs
│   │
│   ├── Services/
│   │   ├── FileTransferHelpers.cs
│   │   └── PairingSession.cs
│   │
│   ├── Scripts/
│   │
│   └── WebAssets/
│       ├── index.html
│       ├── style.css
│       ├── app.js
│       ├── manifest.webmanifest
│       └── sw.js
│
└── DropLAN.slnx
```

---

# Architecture

Starting with the 0.5 architecture, DropLAN separates individual backend responsibilities into route modules.

```text
LocalServer
    │
    ├── PwaRoutes
    ├── PairRoutes
    ├── StateRoutes
    ├── EventRoutes
    ├── UploadRoutes
    ├── DownloadRoutes
    └── ClipboardRoutes
```

Reusable backend logic lives separately inside:

```text
Services/
```

The mobile frontend is stored as normal web assets inside:

```text
WebAssets/
```

rather than being embedded as a giant HTML string in C#.

Civilization survives another day.

---

# Pairing security

DropLAN uses session authorization before allowing access to transfer APIs.

A new session generates:

### Random token

```csharp
RandomNumberGenerator.GetBytes(24)
```

### Six-digit PIN

```csharp
RandomNumberGenerator.GetInt32(100000, 1000000)
```

The device must provide both the correct token and PIN.

Session token validation uses fixed-time comparison.

---

# Security notice

DropLAN is designed primarily for **trusted local networks**.

The current local transport uses:

```text
HTTP
```

rather than HTTPS.

Traffic therefore does not currently receive transport encryption from DropLAN itself.

Use DropLAN on networks you trust, such as:

* your home LAN,
* your personal hotspot,
* a private office network.

Avoid using it on hostile or unknown public networks.

Do not expose DropLAN's port directly to the public Internet.

---

# Network requirements

Both devices should normally be connected to the same LAN.

Example:

```text
Router       192.168.1.1
Windows PC   192.168.1.20
Phone        192.168.1.35
```

DropLAN listens on:

```text
0.0.0.0:5050
```

and the phone connects using the computer's local IPv4 address.

---

# Troubleshooting

### Phone cannot connect

Check that:

* both devices use the same Wi-Fi,
* Windows Firewall allows DropLAN on private networks,
* port `5050` is not blocked,
* no VPN is interfering with LAN routing,
* your Wi-Fi does not use client isolation.

---

### Guest Wi-Fi

Guest networks often prevent devices from communicating with each other.

Look for settings such as:

```text
AP Isolation
Client Isolation
Guest Isolation
Wireless Isolation
```

---

### Pairing suddenly stopped working

If a new session was generated, previous QR links and authorization cookies become invalid.

Scan the newest QR code and enter the newest PIN.

---

### iPhone clipboard restrictions

Safari may restrict direct clipboard API access.

Use the explicit clipboard buttons in DropLAN if browser permissions prevent automatic copying.

---

# PWA note

DropLAN currently serves its mobile interface through local HTTP.

Some advanced PWA capabilities and service worker functionality require HTTPS in modern browsers.

Adding local HTTPS support is therefore one of the natural directions for future development.

---

# Releases

DropLAN includes an automated GitHub Actions release workflow.

```text
.github/workflows/release.yml
```

A release build:

1. installs .NET 10,
2. restores the project,
3. publishes `win-x64`,
4. creates a self-contained build,
5. installs Velopack CLI,
6. packages DropLAN,
7. publishes it to GitHub Releases.

---

# Roadmap ideas

Possible future improvements include:

* 🔒 local HTTPS,
* 📡 automatic device discovery,
* 📁 folder transfers,
* ⚡ resumable large transfers,
* 📊 live transfer speed,
* ⏱️ ETA display,
* ⏸️ pause/resume,
* ✅ checksum verification,
* 🔐 trusted devices,
* 📋 clipboard history,
* 🍎 improved iOS integration,
* 🤖 improved Android integration,
* 🖥️ additional desktop platforms.

These are development ideas, not guaranteed features.

---

# Documentation

More detailed technical documentation is available inside:

```text
DropLAN/README.md
```

It contains additional information about:

* application architecture,
* server routes,
* pairing,
* security,
* networking,
* development,
* building,
* releases,
* troubleshooting,
* testing.

---

# Contributing

Contributions and bug reports are welcome.

When reporting a problem, include:

```text
DropLAN version:
Windows version:
Phone:
Browser:
Network type:
Steps to reproduce:
Expected result:
Actual result:
```

For code contributions:

```bash
git checkout -b feature/my-feature
git commit -m "Add my feature"
git push origin feature/my-feature
```

Then open a Pull Request.

---

# Current status

**DropLAN 0.5.5**

Active development.

Current core functionality includes:

* ✅ Windows desktop application
* ✅ local web server
* ✅ phone browser client
* ✅ QR pairing
* ✅ PIN authorization
* ✅ phone → PC transfer
* ✅ PC → phone transfer
* ✅ clipboard sharing
* ✅ real-time updates
* ✅ transfer history
* ✅ system tray
* ✅ notifications
* ✅ PL / EN localization
* ✅ PWA support
* ✅ Velopack updates
* ✅ GitHub Actions releases

---

# Author

Created and maintained by **Sawickyyy**.

https://github.com/Sawickyyy

---

<div align="center">

## DropLAN

**Fast local transfers. No cloud required.**

Windows ↔ Phone

</div>
