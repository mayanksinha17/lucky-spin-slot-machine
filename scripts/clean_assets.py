import os
import numpy as np
from PIL import Image, ImageFilter

def run():
    print("Starting precision asset processing...")
    src_path = r"C:\Users\ASUS\.gemini\antigravity\brain\32ffeba4-333f-439d-8635-cf299c7134d1\.user_uploaded\media_1793475479701_fe084580.jpg"
    src = Image.open(src_path)
    src_1080 = src.resize((1920, 1080), Image.Resampling.LANCZOS)
    arr_src = np.array(src_1080.convert("RGBA"), dtype=np.float32)

    w, h = 1920, 1080
    y_coords, x_coords = np.mgrid[0:h, 0:w]

    # --- 1. CLEAN BACKGROUND (bg_casino_lounge.png) ---
    # High-quality luxury purple/magenta casino atmosphere without any baked characters, coins, or UI
    y_norm = np.linspace(0, 1, h)[:, None]
    x_norm = np.linspace(0, 1, w)[None, :]

    bg = np.zeros((h, w, 3), dtype=np.float32)
    c_top = np.array([26, 12, 46], dtype=np.float32)
    c_mid = np.array([52, 18, 82], dtype=np.float32)
    c_bot = np.array([16, 8, 28], dtype=np.float32)

    for i in range(3):
        top_half = (1.0 - y_norm / 0.52) * c_top[i] + (y_norm / 0.52) * c_mid[i]
        bot_half = (1.0 - (y_norm - 0.52) / 0.48) * c_mid[i] + ((y_norm - 0.52) / 0.48) * c_bot[i]
        bg[:, :, i] = np.where(y_norm < 0.52, top_half, bot_half)

    r_center = np.sqrt(((x_norm - 0.5) * 1.3) ** 2 + ((y_norm - 0.22) * 2.2) ** 2)
    halo = np.clip(1.0 - r_center, 0, 1) ** 2.0
    bg[:, :, 0] += halo * 75
    bg[:, :, 1] += halo * 30
    bg[:, :, 2] += halo * 110

    r_l = np.sqrt(((x_norm - 0.22) * 1.4) ** 2 + ((y_norm - 0.45) * 1.2) ** 2)
    glow_l = np.clip(1.0 - r_l, 0, 1) ** 2.2
    bg[:, :, 0] += glow_l * 55
    bg[:, :, 1] += glow_l * 20
    bg[:, :, 2] += glow_l * 85

    r_r = np.sqrt(((x_norm - 0.82) * 1.4) ** 2 + ((y_norm - 0.45) * 1.2) ** 2)
    glow_r = np.clip(1.0 - r_r, 0, 1) ** 2.2
    bg[:, :, 0] += glow_r * 65
    bg[:, :, 1] += glow_r * 18
    bg[:, :, 2] += glow_r * 95

    floor_falloff = np.clip(1.0 - np.abs(y_norm - 0.94) / 0.18, 0, 1) ** 2.0
    floor_glow = floor_falloff * np.clip(1.0 - np.abs(x_norm - 0.5) / 0.42, 0, 1)
    bg[:, :, 0] += floor_glow * 65
    bg[:, :, 1] += floor_glow * 42
    bg[:, :, 2] += floor_glow * 18

    bg_img = Image.fromarray(np.clip(bg, 0, 255).astype(np.uint8))
    bg_img = bg_img.filter(ImageFilter.GaussianBlur(radius=2))
    bg_path = "Assets/Art/Backgrounds/bg_casino_lounge.png"
    bg_img.save(bg_path)
    print(f"Saved clean background: {bg_path}")

    # --- 2. PURE CABINET FRAME (cabinet_frame_premium.png) ---
    # Strict mask: only include the central cabinet bezel, crown, and shelf plinth.
    arr_cab = arr_src.copy()
    alpha = np.zeros((h, w), dtype=np.float32)

    # 1. Crown & Marquee: X in [620, 1340], Y in [15, 265]
    mask_marquee = (x_coords >= 620) & (x_coords <= 1340) & (y_coords >= 15) & (y_coords <= 265)

    # 2. Main Outer Bezel: X in [520, 1445], Y in [235, 755]
    mask_bezel = (x_coords >= 520) & (x_coords <= 1445) & (y_coords >= 235) & (y_coords <= 755)

    # 3. Lower Base Plinth & Spin Button Platform:
    # Central plinth under reels: X in [520, 1460], Y in [740, 1015]
    mask_plinth_center = (x_coords >= 520) & (x_coords <= 1460) & (y_coords >= 740) & (y_coords <= 1015)

    # Spin button platform: around (1613, 875) with radius 160
    r_spin_platform = np.sqrt((x_coords - 1613)**2 + (y_coords - 875)**2)
    mask_spin_platform = (r_spin_platform <= 160) & (y_coords <= 1015) & (x_coords >= 1445) & (x_coords <= 1750)

    cabinet_body = mask_marquee | mask_bezel | mask_plinth_center | mask_spin_platform
    alpha[cabinet_body] = 255.0

    # CUT OUT dynamic regions so active GameObjects are the sole source:
    # A. Reel aperture: [590, 1378] x [271, 713]
    alpha[(x_coords >= 590) & (x_coords <= 1378) & (y_coords >= 271) & (y_coords <= 713)] = 0.0

    # B. Status capsule interior: [740, 1220] x [745, 825]
    alpha[(x_coords >= 740) & (x_coords <= 1220) & (y_coords >= 745) & (y_coords <= 825)] = 0.0

    # C. Bet control deck interior: [405, 1465] x [840, 1000]
    alpha[(x_coords >= 405) & (x_coords <= 1465) & (y_coords >= 840) & (y_coords <= 1000)] = 0.0

    # D. Spin button socket: circle at (1613, 875), radius 135
    alpha[r_spin_platform <= 135] = 0.0

    # STRICT CLAMP: zero out all exterior areas completely
    alpha[x_coords < 520] = 0.0
    alpha[(x_coords > 1445) & (y_coords < 740)] = 0.0
    alpha[x_coords >= 1750] = 0.0
    alpha[y_coords < 15] = 0.0
    alpha[y_coords >= 1015] = 0.0

    arr_cab[:, :, 3] = np.clip(alpha, 0, 255)
    cab_img = Image.fromarray(arr_cab.astype(np.uint8))
    cab_path = "Assets/Art/UI/cabinet_frame_premium.png"
    cab_img.save(cab_path)
    print(f"Saved clean cabinet frame: {cab_path}")

    # Synchronize clean textures to dashboard public directory
    pub_dir = r"C:\Users\ASUS\Desktop\Assignment\public\games\lucky-spin\Assets"
    if os.path.exists(pub_dir):
        cab_img.save(os.path.join(pub_dir, "UI", "cabinet_frame_premium.png"))
        bg_img.save(os.path.join(pub_dir, "Backgrounds", "bg_casino_lounge.png"))
        print(f"Synchronized clean textures to {pub_dir}")

    print("Precision asset processing completed successfully.")

if __name__ == "__main__":
    run()