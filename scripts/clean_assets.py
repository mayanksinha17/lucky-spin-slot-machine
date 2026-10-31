import os
import numpy as np
from PIL import Image, ImageFilter

def run():
    print("Starting asset processing...")
    # 1. Load source image
    src_path = r"C:\Users\ASUS\.gemini\antigravity\brain\32ffeba4-333f-439d-8635-cf299c7134d1\.user_uploaded\media_1793475479701_fe084580.jpg"
    src = Image.open(src_path)
    src_1080 = src.resize((1920, 1080), Image.Resampling.LANCZOS)
    arr_src = np.array(src_1080.convert("RGBA"), dtype=np.float32)

    w, h = 1920, 1080
    y_coords, x_coords = np.mgrid[0:h, 0:w]

    # --- 1. CLEAN BACKGROUND (bg_casino_lounge.png) ---
    y_norm = np.linspace(0, 1, h)[:, None]
    x_norm = np.linspace(0, 1, w)[None, :]

    bg = np.zeros((h, w, 3), dtype=np.float32)
    # Luxury casino palette
    c_top = np.array([26, 12, 46], dtype=np.float32)
    c_mid = np.array([52, 18, 82], dtype=np.float32)
    c_bot = np.array([16, 8, 28], dtype=np.float32)

    for i in range(3):
        top_half = (1.0 - y_norm / 0.52) * c_top[i] + (y_norm / 0.52) * c_mid[i]
        bot_half = (1.0 - (y_norm - 0.52) / 0.48) * c_mid[i] + ((y_norm - 0.52) / 0.48) * c_bot[i]
        bg[:, :, i] = np.where(y_norm < 0.52, top_half, bot_half)

    # Ambient ceiling spotlights & crown backlighting
    r_center = np.sqrt(((x_norm - 0.5) * 1.3) ** 2 + ((y_norm - 0.22) * 2.2) ** 2)
    halo = np.clip(1.0 - r_center, 0, 1) ** 2.0
    bg[:, :, 0] += halo * 75
    bg[:, :, 1] += halo * 30
    bg[:, :, 2] += halo * 110

    # Hostess backdrop aura (left)
    r_l = np.sqrt(((x_norm - 0.22) * 1.4) ** 2 + ((y_norm - 0.45) * 1.2) ** 2)
    glow_l = np.clip(1.0 - r_l, 0, 1) ** 2.2
    bg[:, :, 0] += glow_l * 55
    bg[:, :, 1] += glow_l * 20
    bg[:, :, 2] += glow_l * 85

    # Right spade deco backdrop aura (right)
    r_r = np.sqrt(((x_norm - 0.82) * 1.4) ** 2 + ((y_norm - 0.45) * 1.2) ** 2)
    glow_r = np.clip(1.0 - r_r, 0, 1) ** 2.2
    bg[:, :, 0] += glow_r * 65
    bg[:, :, 1] += glow_r * 18
    bg[:, :, 2] += glow_r * 95

    # Reflective marble floor highlight
    floor_falloff = np.clip(1.0 - np.abs(y_norm - 0.94) / 0.18, 0, 1) ** 2.0
    floor_glow = floor_falloff * np.clip(1.0 - np.abs(x_norm - 0.5) / 0.42, 0, 1)
    bg[:, :, 0] += floor_glow * 65
    bg[:, :, 1] += floor_glow * 42
    bg[:, :, 2] += floor_glow * 18

    bg_img = Image.fromarray(np.clip(bg, 0, 255).astype(np.uint8))
    bg_img = bg_img.filter(ImageFilter.GaussianBlur(radius=2))
    bg_path = "Assets/Art/Backgrounds/bg_casino_lounge.png"
    bg_img.save(bg_path)
    print(f"Saved: {bg_path}")

    # --- 2. CLEAN CABINET FRAME (cabinet_frame_premium.png) ---
    arr_cab = arr_src.copy()
    alpha = np.ones((h, w), dtype=np.float32) * 255.0

    # A. Cut out Reel Aperture: [594, 1374] x [276, 708]
    aperture_mask = (x_coords >= 594) & (x_coords <= 1374) & (y_coords >= 276) & (y_coords <= 708)
    alpha[aperture_mask] = 0.0

    # B. Cut out left exterior (where hostess sits)
    left_top_mask = (y_coords < 760) & (x_coords < 540)
    alpha[left_top_mask] = 0.0
    left_bot_mask = (y_coords >= 760) & (x_coords < 350)
    alpha[left_bot_mask] = 0.0

    # C. Cut out right exterior (where spade girl sits)
    right_top_mask = (y_coords < 760) & (x_coords > 1460)
    alpha[right_top_mask] = 0.0
    right_bot_mask = (y_coords >= 760) & (x_coords > 1750)
    alpha[right_bot_mask] = 0.0

    # D. Cut out top corners (above crown / marquee)
    top_left_mask = (y_coords < 200) & (x_coords < 640)
    alpha[top_left_mask] = 0.0
    top_right_mask = (y_coords < 200) & (x_coords > 1320)
    alpha[top_right_mask] = 0.0
    top_crown_mask = (y_coords < 12)
    alpha[top_crown_mask] = 0.0

    # E. Inpaint status capsule interior so active text is clean
    status_mask = (x_coords >= 800) & (x_coords <= 1160) & (y_coords >= 728) & (y_coords <= 776)
    arr_cab[status_mask, 0] = 12
    arr_cab[status_mask, 1] = 42
    arr_cab[status_mask, 2] = 22

    # F. Inpaint bet amount area ('BET 10 G')
    bet_text_mask = (x_coords >= 520) & (x_coords <= 740) & (y_coords >= 885) & (y_coords <= 948)
    arr_cab[bet_text_mask, 0] = 30
    arr_cab[bet_text_mask, 1] = 14
    arr_cab[bet_text_mask, 2] = 48

    # G. Inpaint preset button text areas (10, 50, 100, MAX)
    presets_mask = (x_coords >= 890) & (x_coords <= 1420) & (y_coords >= 885) & (y_coords <= 948)
    arr_cab[presets_mask, 0] = 32
    arr_cab[presets_mask, 1] = 16
    arr_cab[presets_mask, 2] = 52

    arr_cab[:, :, 3] = np.clip(alpha, 0, 255)
    cab_img = Image.fromarray(arr_cab.astype(np.uint8))
    cab_path = "Assets/Art/UI/cabinet_frame_premium.png"
    cab_img.save(cab_path)
    print(f"Saved: {cab_path}")

    print("Asset processing completed successfully.")

if __name__ == "__main__":
    run()

