# DropLAN

<p align="center">
  <strong>Fast, private and cloud-free file transfer between Windows and your phone.</strong>
</p>

<p align="center">
  Transfer files, photos, videos and clipboard text directly over your local network.<br>
  No accounts. No cloud storage. No subscriptions. No unnecessary middleman.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-0.5.5-blue" alt="Version">
  <img src="https://img.shields.io/badge/platform-Windows-0078D4" alt="Windows">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4" alt=".NET 10">
  <img src="https://img.shields.io/badge/UI-WPF-5C2D91" alt="WPF">
  <img src="https://img.shields.io/badge/web-ASP.NET_Core-512BD4" alt="ASP.NET Core">
  <img src="https://img.shields.io/badge/PWA-supported-success" alt="PWA">
</p>

---

## About DropLAN

**DropLAN** is a lightweight Windows application for transferring files and text between a Windows PC and another device connected to the same local network.

Instead of uploading your files to a third-party cloud service, sending them through messaging apps or connecting your phone with a cable, DropLAN starts a small local web server directly on your computer.

Your phone connects to that server using a browser.

The typical workflow is simple:

1. Start DropLAN on Windows.
2. Scan the QR code displayed by the desktop application.
3. Enter the generated six-digit PIN.
4. Send or download files.
5. Transfer clipboard text between devices.
6. Everything stays inside your local network.

DropLAN is designed around one simple idea:

> **Your local files should not need to travel across the Internet just to move two metres across the room.**

---

# Features

## 🚀 Local network file transfer

Files are transferred directly between devices connected to the same LAN.

There is no external storage provider involved in the actual transfer.

This makes DropLAN useful for:

* transferring photos from a phone to a PC,
* moving videos without USB cables,
* sending documents to your phone,
* quickly sharing files between personal devices,
* transferring large files without first uploading them to the Internet,
* moving files when cloud storage is unavailable or undesirable.

---

## 📱 Phone → Windows

DropLAN allows your phone to upload files directly to the Windows computer.

The mobile interface supports selecting content from:

* 📷 Camera
* 🖼️ Photo gallery
* 📁 File picker

You can send:

* photos,
* videos,
* documents,
* archives,
* audio,
* application files,
* or practically any other file type accepted by the browser.

The server is configured to support request bodies of up to approximately:

**16 GB**

This limit is applied both to Kestrel and ASP.NET multipart form handling.

Actual usable transfer size can still depend on:

* available disk space,
* browser limitations,
* device memory,
* network stability,
* operating system limitations.

---

## 💻 Windows → Phone

Files can also be shared from the PC.

From the Windows application you can select files that should become available to the connected phone.

The phone displays a list of currently shared files and lets the user download them directly.

The list updates automatically when the desktop state changes.

No page refresh is required.

---

## 📋 Shared clipboard

DropLAN includes clipboard text transfer between Windows and the phone.

This is useful for quickly moving:

* URLs,
* passwords generated on another device,
* addresses,
* notes,
* commands,
* snippets of code,
* tracking numbers,
* messages,
* any other text.

You can:

* read clipboard text from Windows,
* publish text to the phone,
* send text from the phone,
* copy received text back into the Windows clipboard.

The clipboard state is synchronized through the local DropLAN session.

> Browser clipboard APIs, especially on Safari, may apply additional security restrictions.

---

## 🔄 Real-time updates

DropLAN uses **Server-Sent Events (SSE)** to push state changes to the mobile interface.

This means the browser can react automatically when:

* new files become available,
* files are removed,
* clipboard text changes,
* session state changes,
* transfer history changes.

The mobile interface does not need constant manual refreshing.

If the connection is temporarily interrupted, the browser attempts to reconnect automatically.

---

## 🔐 QR + PIN pairing

Opening a LAN service to every device on the network would be wonderfully convenient for approximately twelve seconds, right until someone else starts using it.

DropLAN therefore uses a pairing process.

A new session generates:

* a random cryptographic session token,
* a random six-digit PIN.

The QR code contains a temporary URL similar to:

```text
http://192.168.1.100:5050/?token=<session-token>
```

After scanning the QR code, the phone must still provide the correct PIN displayed in the desktop application.

Only a valid combination of:

```text
session token + PIN
```

authorizes the device.

After successful pairing, DropLAN stores the current session token in a browser cookie.

The session token is generated using cryptographically secure random bytes.

Token comparisons use fixed-time comparison logic.

---

## 🔁 Regeneratable sessions

A session can be regenerated at any moment.

Generating a new session creates:

* a new QR token,
* a new PIN.

Previously authorized browser sessions become invalid because the expected session token changes.

This is useful when:

* another device was previously connected,
* you are using a shared Wi-Fi network,
* you want to disconnect an old browser,
* you simply want to start with a clean session.

---

# Mobile web application

DropLAN includes its own built-in responsive web interface.

No mobile application needs to be downloaded from an app store.

The interface is served directly by the Windows computer.

The web application includes:

* device pairing,
* connection status,
* file upload,
* upload progress,
* camera access,
* gallery selection,
* file selection,
* PC file browser,
* downloads,
* clipboard sharing,
* transfer history,
* automatic updates,
* Polish and English localization.

The entire frontend is stored inside:

```text
WebAssets/
```

and served by the local ASP.NET Core application.

---

# Progressive Web App

The mobile interface includes Progressive Web App support.

It contains:

```text
manifest.webmanifest
sw.js
PWA icons
```

On supported devices the DropLAN web interface can be added to the home screen.

### iPhone

After connecting to DropLAN:

1. Open DropLAN in Safari.
2. Tap **Share**.
3. Select **Add to Home Screen**.
4. Launch DropLAN from the new icon.

When opened from the Home Screen, DropLAN can behave more like a standalone application instead of a normal Safari tab.

### Important HTTP limitation

DropLAN currently runs over local HTTP:

```text
http://<computer-ip>:5050
```

Modern browser security rules require HTTPS for some advanced PWA capabilities.

As a result, Home Screen usage may work while some service-worker functionality remains restricted.

Local HTTPS support is a possible future improvement.

---

# Desktop application

The Windows application is built with:

* C#
* .NET 10
* WPF
* ASP.NET Core
* Kestrel

The desktop interface acts as both:

1. the user interface,
2. the host for the local DropLAN server.

---

# Desktop interface

The application contains several dedicated sections.

## 🏠 Home

The Home page provides:

* server status,
* pairing QR code,
* current PIN,
* local connection information,
* quick actions,
* recent transfers,
* access to the receive folder,
* session regeneration.

---

## 📤 Send

The Send page is used to make Windows files available to the connected phone.

Files can be:

* added manually,
* dragged into the application,
* removed individually,
* cleared from the current list.

The mobile device receives updated file availability automatically.

---

## 📥 Receive

The Receive section manages files uploaded from connected mobile devices.

It provides access to:

* the configured receive folder,
* recently received files,
* transfer information.

The receive folder can be opened directly from DropLAN.

---

## 📋 Clipboard

The Clipboard section handles text synchronization.

Available actions include:

* retrieve the current Windows clipboard,
* publish clipboard text to the phone,
* receive clipboard text from the phone,
* copy received text into Windows.

---

## 🕘 Transfer history

DropLAN keeps transfer history for the current application session.

History can include files:

* uploaded from the phone,
* shared from Windows,
* downloaded by another device.

The current implementation is session-oriented rather than a permanent transfer database.

---

## ⚙️ Settings

DropLAN includes application settings for behaviour and appearance.

Available options include:

* interface language,
* application theme,
* minimize to tray,
* transfer notifications,
* automatic update checks,
* receive folder configuration.

---

# 🌍 Localization

DropLAN currently contains desktop translations for:

* 🇵🇱 Polish
* 🇬🇧 English

The mobile web application also supports both Polish and English.

The browser remembers its selected language using local storage.

Desktop translation resources are located in:

```text
Localization/
├── Strings.pl.xaml
└── Strings.en.xaml
```

---

# 🎨 Appearance

DropLAN supports configurable application appearance.

The application is designed as a modern Windows utility rather than a bare developer-facing transfer server.

The desktop interface includes:

* navigation sidebar,
* dedicated functional views,
* transfer cards,
* status information,
* responsive layouts,
* configurable theme,
* localized labels.

---

# 🖥️ System tray

DropLAN can continue running after the main window is closed.

When minimize-to-tray behaviour is enabled, closing the window does not necessarily terminate the local server.

The tray menu provides quick access to commonly used operations such as:

* opening DropLAN,
* copying the local address,
* generating a new session,
* opening the receive folder,
* exiting the application.

This allows DropLAN to behave like a background local transfer service without keeping the main window open all the time.

---

# 🔔 Notifications

DropLAN can display Windows notifications after receiving files.

Notifications can be controlled from application settings.

This makes it possible to leave DropLAN running in the background and immediately know when another device has finished sending something.

---

# 🔄 Automatic updates

DropLAN uses **Velopack** for application installation and updates.

The update system is designed around GitHub Releases.

DropLAN can:

* check for available updates,
* download new application versions,
* display download progress,
* install an update,
* restart into the updated version.

Automatic update checking can also be controlled through application settings.

---

# Architecture

DropLAN uses a deliberately simple architecture.

```text
┌───────────────────────────────┐
│          Windows PC           │
│                               │
│  ┌─────────────────────────┐  │
│  │      WPF Desktop UI     │  │
│  └────────────┬────────────┘  │
│               │               │
│  ┌────────────▼────────────┐  │
│  │      Shared State       │  │
│  └────────────┬────────────┘  │
│               │               │
│  ┌────────────▼────────────┐  │
│  │    ASP.NET Core Host    │  │
│  │       + Kestrel         │  │
│  │       Port 5050         │  │
│  └────────────┬────────────┘  │
│               │               │
└───────────────┼───────────────┘
                │
                │ Local Wi-Fi / LAN
                │
        ┌───────▼─────────┐
        │  Phone Browser  │
        │                 │
        │ HTML / CSS / JS │
        │ PWA             │
        └─────────────────┘
```

No central DropLAN server is required.

The Windows machine itself becomes the server.

---

# Backend structure

Starting with the 0.5 architecture, server responsibilities are split into separate route and service classes instead of placing the entire web backend inside one large `LocalServer.cs` file.

```text
DropLAN/
├── LocalServer.cs
├── RealtimeBroker.cs
│
├── Routes/
│   ├── PwaRoutes.cs
│   ├── PairRoutes.cs
│   ├── StateRoutes.cs
│   ├── EventRoutes.cs
│   ├── UploadRoutes.cs
│   ├── DownloadRoutes.cs
│   └── ClipboardRoutes.cs
│
├── Services/
│   ├── PairingSession.cs
│   └── FileTransferHelpers.cs
│
└── WebAssets/
    ├── index.html
    ├── style.css
    ├── app.js
    ├── manifest.webmanifest
    ├── sw.js
    └── icons/
```

This separation keeps routing, session handling, file operations and frontend resources easier to maintain.

---

# Main server

The local server is implemented in:

```text
LocalServer.cs
```

It uses ASP.NET Core's embedded web host and listens on:

```text
0.0.0.0:5050
```

This allows other devices on the local network to reach the application through the computer's LAN IPv4 address.

Example:

```text
http://192.168.1.42:5050
```

---

# Routes

DropLAN separates API responsibilities into multiple route modules.

## PWA routes

```text
Routes/PwaRoutes.cs
```

Responsible for serving the web application and its static resources.

---

## Pairing routes

```text
Routes/PairRoutes.cs
```

Responsible for device authorization and session pairing.

Typical flow:

```text
QR token
   ↓
PIN entry
   ↓
POST /api/pair
   ↓
Token + PIN validation
   ↓
Authorized session cookie
```

---

## State routes

```text
Routes/StateRoutes.cs
```

Expose the current DropLAN state to the mobile client.

The mobile frontend reads state using endpoints such as:

```text
GET /api/state
```

---

## Event routes

```text
Routes/EventRoutes.cs
```

Provide the Server-Sent Events connection used for live updates.

The frontend creates:

```javascript
new EventSource("/events");
```

When DropLAN publishes a state change, the web interface refreshes the relevant data.

---

## Upload routes

```text
Routes/UploadRoutes.cs
```

Handle files transferred from the phone to Windows.

---

## Download routes

```text
Routes/DownloadRoutes.cs
```

Handle downloads of files shared from the Windows application.

---

## Clipboard routes

```text
Routes/ClipboardRoutes.cs
```

Handle synchronized clipboard content between Windows and the connected browser.

---

# Session security

DropLAN is intended for trusted local networks, but the application still implements session authorization to avoid exposing transfers blindly to every LAN client.

When a pairing session is generated, DropLAN creates:

### Session token

A 24-byte random value generated using:

```csharp
RandomNumberGenerator.GetBytes(24)
```

It is converted to hexadecimal representation before being placed in the pairing URL.

### PIN

A six-digit value generated using:

```csharp
RandomNumberGenerator.GetInt32(100000, 1000000)
```

### Validation

A device must provide both:

```text
correct session token
+
correct six-digit PIN
```

The token comparison uses:

```csharp
CryptographicOperations.FixedTimeEquals(...)
```

to avoid ordinary variable-time equality comparison.

---

# Security considerations

DropLAN is designed for **local trusted networks**.

It is important to understand what this means.

## Current transport

Communication currently uses:

```text
HTTP
```

not HTTPS.

Therefore network traffic is **not transport-encrypted by DropLAN itself**.

You should avoid using DropLAN on hostile or untrusted networks such as:

* public airport Wi-Fi,
* hotel guest networks,
* cafés,
* shared public hotspots,
* networks where unknown users may inspect traffic.

DropLAN is best used on:

* your home Wi-Fi,
* a private hotspot,
* a trusted office LAN,
* another private local network.

### Recommended future improvement

Local TLS / HTTPS support would provide encrypted browser-to-PC communication and enable more complete PWA functionality.

---

# Network requirements

Both devices must normally be connected to the same local network.

Example:

```text
Windows PC
192.168.1.50

Phone
192.168.1.71

Router
192.168.1.1
```

Both devices must be able to communicate directly over the LAN.

DropLAN uses:

```text
TCP port 5050
```

If the phone cannot connect, Windows Firewall or router client isolation may be blocking local communication.

---

# Common network issues

## Phone cannot open the QR address

Check:

1. Both devices are connected to the same Wi-Fi network.
2. The PC is not using a VPN that changes LAN routing.
3. The phone is not using a separate cellular connection.
4. Windows Firewall allows DropLAN.
5. Port `5050` is not blocked.
6. The router does not have AP/client isolation enabled.
7. The PC's local IP address has not changed.

---

## QR code opens but pairing fails

Generate a new session and try again.

The QR token and PIN belong to the same pairing session.

If a new session was generated after scanning the QR code, the old QR token becomes invalid.

---

## Windows Firewall

On first use, Windows may ask whether DropLAN should be allowed to communicate over the network.

For normal home usage, allow it on:

```text
Private networks
```

Exposing it to public networks is generally unnecessary.

---

# Requirements

## Running a packaged release

A self-contained release includes the required .NET runtime.

Typical requirements:

* Windows 10 or Windows 11
* x64 processor
* local network connection
* modern web browser on the second device

A mobile device does not need .NET or any DropLAN-specific native application.

---

## Development

To build DropLAN from source you need:

* Windows
* .NET 10 SDK
* Git
* Visual Studio 2022/2026 with .NET desktop development support

or another compatible .NET development environment.

---

# Clone the repository

```powershell
git clone https://github.com/Sawickyyy/DropLAN.git
cd DropLAN
```

---

# Restore dependencies

```powershell
dotnet restore DropLAN/DropLAN.csproj
```

---

# Build

```powershell
dotnet build DropLAN/DropLAN.csproj
```

For a clean build:

```powershell
dotnet clean DropLAN/DropLAN.csproj
dotnet restore DropLAN/DropLAN.csproj
dotnet build DropLAN/DropLAN.csproj
```

---

# Run from source

```powershell
dotnet run --project DropLAN/DropLAN.csproj
```

Because DropLAN is a Windows desktop application, running it requires Windows.

---

# Publish

To create a self-contained Windows x64 build:

```powershell
dotnet publish DropLAN/DropLAN.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true
```

You can also specify the output directory:

```powershell
dotnet publish DropLAN/DropLAN.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o publish
```

The resulting executable will be available in the publish output.

---

# Dependencies

DropLAN intentionally keeps its external dependency list relatively small.

## ASP.NET Core

Used for:

* local HTTP server,
* API routes,
* static web application hosting,
* uploads and downloads,
* Server-Sent Events.

The project references:

```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

---

## QRCoder

Package:

```text
QRCoder 1.6.0
```

Used to generate the pairing QR code displayed by the Windows application.

---

## Velopack

Package:

```text
Velopack 1.2.0
```

Used for:

* packaging,
* installer generation,
* application updates,
* GitHub Release integration.

---

# Release system

DropLAN includes a GitHub Actions workflow:

```text
.github/workflows/release.yml
```

The workflow:

1. checks out the repository,
2. installs .NET 10,
3. restores dependencies,
4. publishes a self-contained `win-x64` build,
5. installs the Velopack CLI,
6. downloads information about the previous release,
7. generates Velopack packages,
8. uploads a new GitHub Release.

---

# Creating a release with GitHub Actions

Open:

```text
GitHub
→ Actions
→ Build DropLAN Release
→ Run workflow
```

Enter a version such as:

```text
0.5.6
```

The workflow publishes using:

```powershell
dotnet publish DropLAN/DropLAN.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:Version=0.5.6 `
  -o publish
```

and then packages the application using Velopack.

The resulting release receives a tag such as:

```text
v0.5.6
```

and a release title such as:

```text
DropLAN 0.5.6
```

---

# Manual Velopack packaging

The repository also contains release scripts inside:

```text
Scripts/
```

including:

```text
build-release.ps1
publish-github.ps1
```

These can be used when releases need to be generated outside GitHub Actions.

---

# Repository structure

A simplified view of the project:

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
│   │   ├── Strings.en.xaml
│   │   └── Strings.pl.xaml
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
│   │   ├── build-release.ps1
│   │   └── publish-github.ps1
│   │
│   └── WebAssets/
│       ├── index.html
│       ├── style.css
│       ├── app.js
│       ├── manifest.webmanifest
│       ├── sw.js
│       └── icons/
│
└── DropLAN.slnx
```

---

# How DropLAN works

A complete transfer session looks roughly like this:

```text
1. DropLAN starts
        ↓
2. ASP.NET Core/Kestrel starts on port 5050
        ↓
3. Local IPv4 address is detected
        ↓
4. Secure random session token is generated
        ↓
5. Six-digit PIN is generated
        ↓
6. QR code containing local pairing URL is displayed
        ↓
7. Phone scans QR
        ↓
8. Browser loads DropLAN web UI from the PC
        ↓
9. User enters PIN
        ↓
10. Token + PIN are validated
        ↓
11. Browser receives authorized session
        ↓
12. SSE connection is established
        ↓
13. Files and clipboard data can move in either direction
```

---

# Data flow

## Phone → PC

```text
Phone
  │
  │ multipart upload
  ▼
ASP.NET Core
  │
  ▼
UploadRoutes
  │
  ▼
Receive folder
  │
  ▼
Shared state
  │
  ▼
Realtime event
  │
  ▼
Windows UI updates
```

---

## PC → Phone

```text
Windows file
  │
  ▼
Shared file list
  │
  ▼
Shared state
  │
  ▼
SSE notification
  │
  ▼
Phone UI refreshes
  │
  ▼
User presses Download
  │
  ▼
DownloadRoutes
  │
  ▼
Phone receives file
```

---

# Why DropLAN?

There are already many ways to send a file.

Most of them involve some combination of:

* logging into another service,
* uploading your file to somebody else's server,
* waiting for cloud synchronization,
* sending yourself an email,
* messaging yourself like a technologically defeated human being,
* plugging in a USB cable that inexplicably only works when rotated three times despite USB-C being reversible.

DropLAN aims to make the local case simple.

If both devices are already next to each other and connected to the same network, the network itself should be enough.

---

# DropLAN vs cloud transfer

| Feature                             | DropLAN | Typical cloud service |
| ----------------------------------- | ------- | --------------------- |
| Account required                    | ❌       | Often ✅               |
| Internet required for transfer      | ❌       | ✅                     |
| Cloud upload                        | ❌       | ✅                     |
| Local LAN transfer                  | ✅       | Usually ❌             |
| Phone app required                  | ❌       | Often ✅               |
| Browser interface                   | ✅       | ✅                     |
| Direct PC ↔ Phone                   | ✅       | Usually ❌             |
| Clipboard sharing                   | ✅       | Varies                |
| Self-hosted session                 | ✅       | ❌                     |
| Transfer data stored by third party | ❌       | Usually ✅             |

---

# DropLAN vs USB

USB remains useful for very large transfers, but DropLAN removes the cable requirement.

DropLAN is especially convenient when:

* the phone is across the room,
* you want to send only a few files,
* the cable is charging something else,
* the phone uses awkward USB file access,
* you only need to transfer clipboard text,
* you want quick cross-device access without installing mobile software.

---

# Privacy

DropLAN's architecture is intentionally local.

The transfer server runs on your own Windows computer.

Files do not need to be uploaded to a DropLAN-operated external server.

There is currently no DropLAN cloud account system.

There is currently no DropLAN cloud storage layer.

The primary transfer path is:

```text
your device
     ↓
your local network
     ↓
your computer
```

This does not mean every local network is automatically secure.

Use a trusted LAN.

---

# Troubleshooting

## Server starts but phone cannot connect

Check your local IP address and firewall configuration.

The server uses:

```text
5050
```

Make sure another application is not already occupying the port.

---

## Different subnet

If the PC is connected to:

```text
192.168.0.x
```

and the phone is connected to an isolated network such as:

```text
192.168.50.x
```

the devices may not be able to communicate.

Connect them to the same normal LAN.

---

## VPN

Some VPN software modifies routing or firewall rules.

Temporarily disable the VPN if DropLAN cannot detect or reach the correct local interface.

---

## Guest Wi-Fi

Many routers intentionally prevent guest devices from talking to other clients.

This feature may be called:

* Client Isolation
* AP Isolation
* Wireless Isolation
* Guest Isolation

Disable isolation or use the normal private Wi-Fi network.

---

## File upload fails

Possible causes include:

* network connection interrupted,
* insufficient free disk space,
* browser upload limitations,
* phone entering sleep mode,
* very large upload,
* server stopped,
* pairing session changed.

Generate a new session and retry if necessary.

---

## Clipboard does not copy automatically on iPhone

Safari limits clipboard access unless certain actions happen directly after user interaction.

Use the explicit **Copy** action in the DropLAN interface when automatic clipboard access is blocked.

---

## Old QR code no longer works

QR pairing links are session-specific.

Generating a new session invalidates the previous token.

Scan the latest QR code displayed by DropLAN.

---

# Development notes

## Backend

Backend functionality should remain separated by responsibility.

Avoid rebuilding `LocalServer.cs` into one giant file.

New APIs should generally be placed under:

```text
Routes/
```

Reusable backend logic should generally live under:

```text
Services/
```

---

## Frontend

The mobile frontend is intentionally stored as normal web assets rather than a giant C# string.

Modify:

```text
WebAssets/index.html
WebAssets/style.css
WebAssets/app.js
```

when working on the phone interface.

This provides proper syntax highlighting, formatting and maintainability.

---

# Testing checklist

Before publishing a release, verify at minimum:

### Pairing

* [ ] QR code is displayed correctly
* [ ] QR opens from phone
* [ ] valid PIN pairs successfully
* [ ] invalid PIN is rejected
* [ ] regenerating session invalidates old session

### Phone → Windows

* [ ] photo upload
* [ ] video upload
* [ ] document upload
* [ ] multiple file upload
* [ ] upload progress works
* [ ] received files appear in Windows
* [ ] transfer notification works

### Windows → Phone

* [ ] add file from Windows
* [ ] file appears automatically on phone
* [ ] download works
* [ ] file removal updates phone automatically

### Clipboard

* [ ] Windows clipboard → phone
* [ ] phone clipboard text → Windows
* [ ] Polish characters work
* [ ] emoji work
* [ ] multi-line text works

### Realtime

* [ ] SSE connects
* [ ] state updates without manual refresh
* [ ] reconnect works after temporary interruption

### Application

* [ ] tray icon works
* [ ] closing window respects minimize-to-tray setting
* [ ] Open action works
* [ ] New Session works
* [ ] Open Folder works
* [ ] Exit stops application cleanly

### Localization

* [ ] Polish desktop UI
* [ ] English desktop UI
* [ ] Polish mobile UI
* [ ] English mobile UI

### Updates

* [ ] update check works
* [ ] release is detected
* [ ] update download starts
* [ ] progress is displayed
* [ ] application installs update
* [ ] application restarts successfully

---

# Possible future improvements

Potential directions for future versions include:

* 🔒 local HTTPS / TLS support,
* 📡 automatic device discovery,
* 🔗 Windows Share integration,
* 📤 drag-and-drop directly from Explorer,
* 📁 folder transfer,
* 🗜️ automatic multi-file archive transfer,
* 🔐 optional persistent trusted devices,
* 📱 Android-specific enhancements,
* 🍎 improved iOS PWA integration,
* 🖥️ additional desktop platforms,
* 📊 transfer speed display,
* ⏱️ ETA calculation,
* ⏸️ pause/resume transfers,
* 🔄 resumable large uploads,
* ✅ checksums and integrity verification,
* 🔎 richer transfer history,
* 🧹 automatic history cleanup,
* 📋 advanced clipboard history,
* 📷 direct QR scanner support on additional devices,
* 🌐 optional manually enabled remote mode.

These are ideas rather than guarantees or committed roadmap items.

---

# Current project status

DropLAN is under active development.

The current project version is:

```text
0.5.5
```

The application already provides the core local transfer workflow:

* Windows desktop application,
* mobile browser interface,
* QR pairing,
* PIN authorization,
* file transfers in both directions,
* clipboard sharing,
* real-time synchronization,
* transfer history,
* PWA support,
* tray integration,
* localization,
* update infrastructure.

---

# Contributing

Contributions, bug reports and feature suggestions are welcome.

If you find a problem, open a GitHub Issue and include:

* DropLAN version,
* Windows version,
* phone/device model,
* browser,
* network setup,
* steps to reproduce,
* expected behaviour,
* actual behaviour,
* screenshots or logs if useful.

For code contributions:

1. Fork the repository.
2. Create a branch.
3. Implement the change.
4. Test the desktop and mobile flows.
5. Open a Pull Request.
6. Describe what changed and why.

Example:

```bash
git checkout -b feature/my-improvement
git commit -m "Add my improvement"
git push origin feature/my-improvement
```

---

# Bug reports

A useful bug report should answer:

```text
DropLAN version:
Windows version:
Phone:
Browser:
Connection type:
What happened:
What should have happened:
Can it be reproduced:
```

For networking bugs, also include whether:

* both devices were on the same Wi-Fi,
* a VPN was active,
* Windows Firewall was enabled,
* a guest network was being used.

---

# Disclaimer

DropLAN is intended for transferring files between devices on a trusted local network.

The current local web server uses HTTP rather than HTTPS.

Do not expose port `5050` directly to the public Internet.

Do not create router port-forwarding rules for DropLAN unless you fully understand the security implications.

DropLAN should not be treated as an Internet-facing file server.

---

# Author

Created and maintained by **Sawickyyy**.

GitHub:

```text
https://github.com/Sawickyyy
```

Repository:

```text
https://github.com/Sawickyyy/DropLAN
```

---

<p align="center">
  <strong>DropLAN</strong><br>
  Fast local transfers. No cloud required.
</p>
