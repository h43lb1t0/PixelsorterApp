Pixelsorter loads an image, sorts its pixels in a chosen direction using a chosen color criterion, and lets you preview and save results.

The main screen is organized like this:

- **Image preview** (tap to load an image)
- **Sort settings** (what to sort by + sorting direction)
- **Masking options** (optional: limit which pixels are allowed to move)
- **Sort** button (creates a new sorted result)
- **Save** button (saves the currently displayed result)

---


## Loading an image

### Load by tapping the preview

1. On the main screen, **tap the image preview**.
2. Pick an image.
3. After loading, the caption below the preview will show **“Original image”**, and the app will announce that it’s ready to sort.

Notes:

- Once an image is loaded, the **Sort** button becomes enabled.
- The preview area can show multiple images (original + results). See Previewing results (swipe).

### Android: share / “Send to” Pixelsorter

On Android, Pixelsorter can receive an image from other apps:

1. In your gallery, file manager, or another app, use **Share**.
2. Choose **Pixelsorter**.

Pixelsorter will open and load the shared image automatically.

---

## Previewing results (swipe)

Every time you press **Sort**, Pixelsorter generates a new processed image and adds it to the preview.

**Android only!**
You can **swipe left/right** on the preview to switch between:

- the **original image**, and
- one or more **sorted results**.

The text under the preview describes what you’re currently seeing (for example, “Original image” or the options used).

---

## Saving

The **Save** button saves the **currently displayed image**.

Important behavior:

- **Save is disabled for the original image**. It becomes enabled only when you swipe to a generated result.
- If you have multiple results, swipe to the one you want and then tap **Save**.

---

## Sorting options

Pixel sorting works like this:

- Pixels are processed in **runs** along the chosen direction (rows or columns).
- Within each run, pixels are reordered by a **Sort by** criterion.
- If a **mask** is enabled, only pixels inside the mask are considered sortable; pixels outside keep their original positions.

### Sort by

**Sort by** controls the value used to compare pixels. The app uses HSL color space (Hue/Saturation/Lightness) and offers these criteria:

- **Hue** — sorts by hue angle (0–360). Produces strong color banding / rainbow-like transitions.
- **Saturation** — sorts by saturation (how “colorful” a pixel is).
- **Lightness** — sorts by brightness.
- **Warmth** — favors hues around warm colors (near orange/red) and boosts saturated, mid-lightness pixels.
- **Coolness** — favors hues around cool colors (near blue) and boosts saturated, mid-lightness pixels.
- **Chroma** — measures vividness (high saturation + not too close to pure black/white).
- **PerceivedVibrancy** — similar to chroma, but weighted by hue to better match perception.

If you want a quick starting point:

- try **Lightness** for classic “glitchy” streaks,
- try **Hue** for bold color separation,
- try **PerceivedVibrancy** for more “natural” vividness-driven effects.

### Direction

**Direction** controls in which order pixels are sorted:

- **Row Left To Right** — sorts within each row.
- **Row Right To Left** — same, but writes results reversed.
- **Column Top To Bottom** — sorts within each column.
- **Column Bottom To Top** — same, but writes results reversed.
- **Into Mask** — special mode that sorts pixels *radially into the mask* (requires a mask).

Mask-related detail:

- When **no masks** are enabled, only the basic row/column directions are relevant.
- When a mask is enabled, sorting happens in contiguous masked segments:
  - for row sorting: masked stretches in each row are sorted independently
  - for column sorting: masked stretches in each column are sorted independently
- **Into Mask** is only available/meaningful when you have a mask, because it uses the mask shape to guide sorting.

---

## Masks (what they are)

A **mask** is a black/white image (internally a thresholded grayscale mask).

- **White (inside mask):** pixels are allowed to move/sort.
- **Black (outside mask):** pixels stay in place.

Masks are useful when you want a pixel-sorting effect only in certain regions (foreground/background, edges, etc.).

### Subject mask

**Use subject masking** tries to separate the main subject from the background using a machine-learning model.

- The **first time** you enable it, the model may need to be downloaded (internet required).
- Afterwards it runs locally using the cached model.

Options:

- **Padding** — expands the subject area by the given number of pixels. Use this when the cutout is too tight.
- **What to sort** (only visible when *subject mask is on* and *Canny is off*):
  - **Background** — sorts the background and keeps the subject mostly stable.
  - **Foreground** — sorts the subject and keeps the background mostly stable.

### Canny mask (edge mask)

**Use Canny masking** creates an additional mask from image edges (edge detection).

- This is processed on-device.

Option:

- **Threshold** — controls how strong edge detection is.
  - lower values → more edges included
  - higher values → fewer edges

This mask is handy when you want the sorting to “stick” to outlines and structures.

### When both masks are enabled: combining

When **subject masking** and **Canny masking** are both enabled, Pixelsorter combines them.

You can choose:

- **Subtract** — subtracts the Canny mask from the subject mask.
  - practical effect: reduces sorting around/at detected edges (keeps borders cleaner)
- **Add** — adds the Canny mask and the subject mask.
  - practical effect: expands the sortable area to include both the subject region and strong edges

---

## Presets

Presets allow you to save and load your favorite sorting and masking configurations.

- **Load a preset:** On the main screen, use the preset selector to quickly apply a saved configuration.
- **Create or edit presets:** Choose the **"new preset"** option or navigate to the Presets page. Here you can write custom presets using TOML syntax.
- **Preset options:** You can view the TOML Map on the Presets page to guide you through the available configuration keys (such as `sort_settings`, `masking_options`, etc.).
- **Default preset:** You can assign any preset as your default, so Pixelsorter always starts with your preferred settings.

---

## Tips & troubleshooting

- If **Sort** is disabled, make sure you loaded an image first.
- If **Save** is disabled, you are probably viewing the **original image**. Swipe to a generated result.
- If enabling **subject masking** does nothing the first time, check your internet connection (model download is required once).
- For faster experimentation, start without masks and with **Row Left To Right + Lightness**, then add masks when you know the direction/criterion you like.
