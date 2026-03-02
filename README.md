# SurfaceTouchDeck

A Windows WPF desktop overlay for Surface devices that provides controller-style on-screen touch controls.

## Implemented

- Full-screen transparent overlay with dark-mode controls.
- Home-button hold opens centered settings menu with:
  - Layout entries (`layout1`, `layout2`, …)
  - Per-layout delete (`X`) with confirmation
  - Per-layout edit (`✎`) actions
  - Style selection opener
  - Opacity slider (0–100% 🪟)
  - Global scale slider (0–100% 🔍)
  - Exit action
- Style picker with live preview while menu stays open, plus Cancel/Confirm.
- Layout persistence as `layout*.json` next to `.exe`.
- Style folder icon lookup (`single-*.ico`, `copy*.ico`, and optional `*-pressed.ico` fallback).
- Edit modes (green/purple) with remembered last mode and required color coding.
- Basic ABXY classic-cluster visualization with crossing lines and center selector.
- System tray behavior:
  - Left click opens Home menu
  - Right click shows `Quit`

## Input backend

- Keyboard/mouse simulation service is included via `SendInput`.
- The code is structured so an Xbox 360 virtual controller backend (ViGEmBus/Nefarius) can be added behind the same dispatch points.

## Running

Build on Windows with .NET 8 SDK:

```powershell
dotnet build
```

Place style folders and `layout*.json` beside the produced `.exe`.


## Visual Studio Community (recommended)

1. Install **Visual Studio Community 2022** with the **Desktop development with .NET** workload.
2. Open `SurfaceTouchDeck.sln`.
3. In Solution Explorer, right-click **SurfaceTouchDeck** → **Set as Startup Project**.
4. Select `Debug | Any CPU` (or `Release | Any CPU`).
5. Press **F5** to run with debugger (or **Ctrl+F5** without debugger).

### Where output files are

- Build output is under:
  - `bin\Debug\net8.0-windows\`
  - or `bin\Release\net8.0-windows\`
- Put your style folders (with `.ico` files) and `layout*.json` files next to `SurfaceTouchDeck.exe` in that output directory.
