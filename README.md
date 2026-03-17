# PixelsorterApp

A cross-platform .NET MAUI application for generating pixel sorting effects on images. 

This project serves as the graphical user interface for the core pixel sorting algorithms provided by the [PixelsorterClassLib](https://github.com/h43lb1t0/PixelsorterClassLib) library.

## Features

- **Cross-Platform Interface:** Built with .NET MAUI targeting .NET 10, supporting multiple platforms (Windows, Android, iOS, macOS).
- **Core Processing Engine:** All the heavy lifting for image processing is delegated to `PixelsorterClassLib`.
- **Load Images:** Select photos from your device gallery.
- **Share Implementation *(Android Only)*:** Directly receive and open images exported/shared from other apps.
- **Customizable Sorting:** 
  - Choose sorting criteria (e.g., light, color intensity, hue).
  - Select sorting directions (e.g., Row Right-to-Left, Top-to-Bottom).
- **Subject Masking:** Automatically mask the main subject, optionally invert the mask, and adjust mask padding to control the sorting boundaries.
- **Save to Gallery *(Android & Windows Only)*:** Save your newly generated pixel-sorted artworks directly back to your device's photo gallery or Pictures folder.

> **Note:** Due to platform-specific API requirements, the **Share** feature is currently only implemented for Android, and **Save to Gallery** is implemented for Android and Windows. The core pixel sorting functionality works across all supported platforms.


### Android Screenshots


<img height="800" alt="Screenshot_20260308-232428" src="https://github.com/user-attachments/assets/32daa09f-42e0-4736-abef-ebe558e5c752" /> <img height="800" alt="Screenshot_20260308-233136" src="https://github.com/user-attachments/assets/390bc7a5-b40a-454f-9168-5f91b3b806e1" />


## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 (with the .NET MAUI workload installed) or Visual Studio Code with the .NET MAUI extension.

## Getting Started

Because this app relies on a separate class library for its core logic, you will need to clone and add it to your solution before building.

1. **Clone the repositories:**
   It is recommended to put both repositories in the same parent directory:
   ```bash
   git clone https://github.com/h43lb1t0/PixelsorterClassLib.git
   git clone https://github.com/h43lb1t0/PixelsorterApp.git
   ```

2. **Add the Class Library to the Solution:**
   - Open the `PixelsorterApp` solution in your IDE.
   - Right-click the solution, select **Add > Existing Project**, and choose the `PixelsorterClassLib.csproj` file from the cloned library directory.
   - Verify that the `PixelsorterApp` project has a project reference pointing to the `PixelsorterClassLib` project.

3. **Build and Run:**
   Select your target device or emulator (e.g., Windows Machine, Android Emulator), then build and launch the application.

## Related Projects

- **[PixelsorterClassLib](https://github.com/h43lb1t0/PixelsorterClassLib):** The underlying library containing the pixel sorting algorithms used by this app.


# License
With commit `81e05075` the license of this project changed from MIT to EUPL 1.2
All changes, releases etc. before this commit stay under the MIT license.