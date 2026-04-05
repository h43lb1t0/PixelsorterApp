# Code-Behind to MVVM Refactor Summary

> **Transparency note:** This refactor was performed with assistance from **GitHub Copilot** in iterative steps, including code edits, verification builds, and follow-up fixes based on runtime feedback.

## Goal

Refactor the app from a code-behind-heavy `MainPage` toward an MVVM-oriented structure while preserving behavior.

## What was changed

## 1) Dependency injection and app composition

- Registered key app services and MVVM types in `MauiProgram`:
  - `AppShell`
  - `MainPage`
  - `MainPageViewModel`
  - `IImageProcessingService` / `ImageProcessingService`
- Updated app startup flow so `AppShell`/`MainPage` are created in a safe order.
  - This fixed the Android startup issue caused by resource timing (`StaticResource not found...`) during early page creation.

## 2) Introduced MVVM base and main view model

- Added `ViewModels/BaseViewModel.cs` and migrated it to `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
- Added and expanded `ViewModels/MainPageViewModel.cs` to hold:
  - UI state (`IsSortEnabled`, `IsSaveVisible`, `IsSaveEnabled`, `IsInteractionEnabled`, `CurrentCaption`)
  - Sort settings and selected options
  - Mask settings (subject/canny toggles, threshold/padding, combine mode)
  - Derived/visibility properties for XAML sections
  - Commands and request events for UI actions

## 2.1) CommunityToolkit.Mvvm adoption

- Added/confirmed package reference:
  - `CommunityToolkit.Mvvm`
- Migrated commands to toolkit commands:
  - `RelayCommand`
  - `IRelayCommand`
  - `NotifyCanExecuteChanged()` usage
- Migrated view model properties to toolkit source generators:
  - `[ObservableProperty]`
  - Converted to **partial property** form (recommended by analyzer `MVVMTK0042`)
  - Used `[NotifyPropertyChangedFor(...)]` for dependent/computed properties
- Moved side effects into generator hooks (`partial void OnXChanged(...)`) where needed.

## 3) Moved XAML from event/state-driven to binding-driven

- Updated `MainPage.xaml` bindings for:
  - sort pickers and selected indices
  - masking toggles, sliders, and labels
  - radio button selections
  - section visibility logic
  - control enabled states
  - sort/save button states
- Converted most button/menu `Clicked` handlers to command bindings.

## 4) Extracted processing logic to service layer

- Added `Services/IImageProcessingService.cs` and `Services/ImageProcessingService.cs`.
- Moved processing concerns from page code-behind into service:
  - subject/canny mask generation
  - mask composition (`add`/`subtract`/invert)
  - mask caching for active image/settings
  - image sorting and output file generation
  - gallery save integration

## 5) Reduced code-behind responsibilities

`MainPage.xaml.cs` now mainly hosts:

- platform/UI interactions that are reasonable in view layer:
  - navigation (`Licenses`, `Privacy`, help action sheet)
  - alerts and accessibility announcements
  - media picker invocation
  - custom `ImageViewer` presentation updates
- bridge wiring between view model request events and async page methods
- page lifecycle subscriptions for shared image handling

### Event bridge note

- The current design uses a pragmatic bridge:
  - view model commands trigger request events
  - `MainPage` handles platform/UI-only operations (navigation, action sheet, media picker, alerts)
- This keeps non-UI state/behavior in MVVM while preserving MAUI/platform integration points in the view.

## 6) Subject-mask enable flow modernized

- Removed direct switch toggle logic from XAML event dependency.
- Added property-change-driven handling for `UseSubjectMask` enable flow:
  - internet check
  - license acceptance prompt
  - model download
  - safe rollback when user declines/fails

## 7) Documentation updates

- Added/updated XML documentation comments across:
  - `MainPage`
  - `MainPageViewModel`
  - `BaseViewModel`
  - `IImageProcessingService`
  - `ImageProcessingService`
  - `App`, `AppShell`, and `MauiProgram`

## Important fixes made during refactor

- Fixed Android startup crash related to resource dictionary timing.
- Reverted `ImageViewer` tap behavior from `EventToCommandBehavior` back to direct `ImageTapped` handler because that behavior broke image loading with the custom control.
- Fixed analyzer warning `MVVMTK0042` by converting generated members to partial-property `[ObservableProperty]` syntax.

## Current architecture status

### Mostly MVVM

- State lives in `MainPageViewModel`
- `CommunityToolkit.Mvvm` drives state/command implementation
- Commands/events drive user actions
- Processing logic is in service layer

### Still intentionally in code-behind

- Platform/UI-specific concerns (navigation, action sheet, alerts, media picker, custom control rendering)

This is a pragmatic MAUI MVVM end-state and keeps behavior stable.

## Verification

- The workspace was built after each major step.
- Final state compiles successfully.
