"""Procedurally generated sample landscapes for the PhotoGallery demo (no third-party images)."""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W, H = 1600, 1067
OUT = Path(sys.argv[1])
OUT.mkdir(parents=True, exist_ok=True)
rng = np.random.default_rng(7)


def vgrad(stops, h=H, w=W):
    """Vertical gradient from list of (pos 0..1, (r,g,b))."""
    ys = np.linspace(0, 1, h)
    out = np.zeros((h, 3))
    for c in range(3):
        out[:, c] = np.interp(ys, [s[0] for s in stops], [s[1][c] for s in stops])
    return np.repeat(out[:, None, :], w, axis=1)


def to_img(arr):
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")


def ridge(seed, base, amp, freqs, w=W):
    r = np.random.default_rng(seed)
    xs = np.arange(w)
    y = np.full(w, float(base))
    for f, a in freqs:
        y += amp * a * np.sin(xs / w * math.tau * f + r.uniform(0, math.tau))
    y += np.cumsum(r.normal(0, amp * 0.02, w))
    y -= np.linspace(0, 1, w) * (y[-1] - y[0]) * 0.5
    return y


def fill_below(img, ys, color, alpha=255):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    pts = [(x, float(ys[x])) for x in range(len(ys))] + [(W, H), (0, H)]
    d.polygon(pts, fill=(*color, alpha))
    img.alpha_composite(layer)


def glow(img, center, radius, color, strength=1.0):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for i in range(30, 0, -1):
        r = radius * i / 10
        a = int(255 * strength * (1 - i / 30) ** 2 * 0.25)
        d.ellipse([center[0] - r, center[1] - r, center[0] + r, center[1] + r], fill=(*color, a))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(radius / 3)))


def disc(img, center, r, color):
    d = ImageDraw.Draw(img)
    d.ellipse([center[0] - r, center[1] - r, center[0] + r, center[1] + r], fill=(*color, 255))


def stars(img, n, max_y, seed=1, band=None):
    r = np.random.default_rng(seed)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for _ in range(n):
        x, y = r.uniform(0, W), r.uniform(0, max_y)
        if band is not None:
            # concentrate stars around a diagonal band
            t = r.uniform(0, 1)
            x = t * W
            y = band(x) + r.normal(0, 90)
            if not 0 < y < max_y:
                continue
        s = r.choice([0.6, 0.9, 1.2, 1.8], p=[0.55, 0.3, 0.12, 0.03])
        a = int(r.uniform(120, 255))
        d.ellipse([x - s, y - s, x + s, y + s], fill=(255, 255, 255, a))
    img.alpha_composite(layer)


def save(img, name):
    img.convert("RGB").save(OUT / name, "JPEG", quality=86, optimize=True, progressive=True)
    print("wrote", name)


# 1 alpine sunrise
img = to_img(vgrad([(0, (40, 60, 120)), (0.45, (238, 140, 120)), (0.62, (255, 200, 140)), (1, (255, 220, 170))])).convert("RGBA")
glow(img, (1050, 600), 260, (255, 230, 170), 1.2)
disc(img, (1050, 600), 55, (255, 244, 214))
for i, (base, col) in enumerate([(560, (120, 100, 140)), (650, (85, 70, 110)), (760, (55, 45, 80)), (880, (30, 26, 50))]):
    ys = ridge(10 + i, base, 120 - i * 15, [(1.3, 1), (3.1, 0.45), (7, 0.15)])
    fill_below(img, ys, col)
    if i == 0:
        snow = np.minimum(ys + 40, ys + (ys.max() - ys) * 0.12)
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        for x in range(W):
            if ys[x] < base - 40:
                d.line([(x, ys[x]), (x, snow[x])], fill=(250, 235, 240, 210))
        img.alpha_composite(layer)
save(img, "alpine-sunrise.jpg")

# 2 misty ridges
img = to_img(vgrad([(0, (198, 214, 228)), (1, (232, 236, 240))])).convert("RGBA")
for i in range(7):
    ys = ridge(30 + i, 380 + i * 95, 70, [(1.7, 1), (4.3, 0.4), (11, 0.12)])
    shade = int(170 - i * 22)
    fill_below(img, ys, (shade - 30, shade - 10, shade + 15))
    fog = Image.new("RGBA", img.size, (0, 0, 0, 0))
    fd = ImageDraw.Draw(fog)
    fd.rectangle([0, 380 + i * 95 + 40, W, H], fill=(235, 240, 245, 55))
    img.alpha_composite(fog.filter(ImageFilter.GaussianBlur(40)))
save(img, "misty-ridges.jpg")

# 3 desert dunes
img = to_img(vgrad([(0, (120, 170, 220)), (0.45, (250, 215, 170)), (1, (250, 215, 170))])).convert("RGBA")
glow(img, (380, 300), 160, (255, 245, 220), 0.9)
disc(img, (380, 300), 40, (255, 250, 235))
for i, col in enumerate([(225, 160, 100), (210, 135, 80), (190, 112, 62), (165, 92, 50)]):
    ys = ridge(50 + i, 560 + i * 120, 60, [(0.9, 1), (2.2, 0.5)])
    fill_below(img, ys, col)
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    pts = [(x, float(ys[x])) for x in range(0, W, 4)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        if y1 > y0:
            sd.polygon([(x0, y0), (x1, y1), (x1, y1 + 140), (x0, y0 + 140)], fill=(90, 40, 20, 50))
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(12)))
save(img, "desert-dunes.jpg")

# 4 calm sea
sky = vgrad([(0, (70, 120, 190)), (0.5, (250, 190, 150)), (0.52, (60, 90, 140)), (1, (20, 40, 80))])
img = to_img(sky).convert("RGBA")
glow(img, (800, 520), 200, (255, 210, 160), 1.1)
disc(img, (800, 535), 48, (255, 236, 200))
d = ImageDraw.Draw(img)
d.rectangle([0, 555, W, H], fill=(36, 62, 104, 255))
refl = Image.new("RGBA", img.size, (0, 0, 0, 0))
rd = ImageDraw.Draw(refl)
r = np.random.default_rng(3)
for y in range(560, H, 6):
    wdt = 30 + (y - 560) * 0.45 + r.uniform(-20, 20)
    a = int(220 * max(0, 1 - (y - 560) / 520))
    rd.line([(800 - wdt, y), (800 + wdt, y)], fill=(255, 210, 160, a), width=2)
for _ in range(500):
    x, y = r.uniform(0, W), r.uniform(560, H)
    rd.line([(x, y), (x + r.uniform(20, 80), y)], fill=(120, 160, 210, 60), width=1)
img.alpha_composite(refl.filter(ImageFilter.GaussianBlur(1.2)))
save(img, "calm-sea.jpg")

# 5 lighthouse at dusk
img = to_img(vgrad([(0, (40, 30, 80)), (0.5, (190, 90, 120)), (0.62, (250, 170, 120)), (0.63, (40, 50, 90)), (1, (15, 20, 45))])).convert("RGBA")
stars(img, 120, 400, seed=5)
d = ImageDraw.Draw(img)
cliff = [(900, 680), (1000, 610), (1120, 600), (1260, 560), (1400, 590), (1600, 570), (1600, H), (860, H)]
d.polygon(cliff, fill=(20, 18, 35, 255))
d.polygon([(1205, 590), (1235, 360), (1275, 360), (1305, 590)], fill=(235, 230, 225, 255))
for yy in (420, 500):
    d.polygon([(1205 + (590 - yy) * 0.03, yy), (1305 - (590 - yy) * 0.03, yy), (1305 - (590 - yy - 35) * 0.03, yy + 35), (1205 + (590 - yy - 35) * 0.03, yy + 35)], fill=(190, 40, 50, 255))
d.rectangle([1228, 330, 1282, 362], fill=(255, 230, 150, 255))
d.polygon([(1222, 330), (1255, 300), (1288, 330)], fill=(30, 25, 40, 255))
beam = Image.new("RGBA", img.size, (0, 0, 0, 0))
bd = ImageDraw.Draw(beam)
bd.polygon([(1255, 346), (0, 180), (0, 330)], fill=(255, 235, 170, 70))
img.alpha_composite(beam.filter(ImageFilter.GaussianBlur(18)))
glow(img, (1255, 346), 60, (255, 230, 150), 1.4)
save(img, "lighthouse-dusk.jpg")

# 6 aurora
img = to_img(vgrad([(0, (5, 10, 30)), (0.7, (15, 35, 60)), (1, (8, 15, 30))])).convert("RGBA")
stars(img, 700, 800, seed=6)
aur = Image.new("RGBA", img.size, (0, 0, 0, 0))
ad = ImageDraw.Draw(aur)
for k, (col, off) in enumerate([((60, 255, 160), 0), ((40, 220, 200), 60), ((150, 90, 255), 120)]):
    for x in range(0, W, 3):
        y0 = 300 + off + 120 * math.sin(x / W * math.tau * 1.3 + k) + 40 * math.sin(x / 90 + k)
        h = 260 + 80 * math.sin(x / 140 + k * 2)
        a = int(110 * (0.6 + 0.4 * math.sin(x / 35 + k)))
        ad.line([(x, y0 - h), (x, y0)], fill=(*col, max(0, a)), width=3)
aur = aur.filter(ImageFilter.GaussianBlur(14))
img.alpha_composite(aur)
ys = ridge(61, 860, 80, [(1.2, 1), (3.7, 0.4), (9, 0.1)])
fill_below(img, ys, (6, 10, 18))
save(img, "aurora.jpg")

# 7 milky way
img = to_img(vgrad([(0, (8, 8, 25)), (1, (25, 30, 60))])).convert("RGBA")
band = lambda x: 900 - x * 0.55
haze = Image.new("RGBA", img.size, (0, 0, 0, 0))
hd = ImageDraw.Draw(haze)
for x in range(-200, W + 200, 8):
    y = band(x)
    hd.ellipse([x - 130, y - 70, x + 130, y + 70], fill=(170, 150, 210, 10))
    hd.ellipse([x - 60, y - 25, x + 60, y + 25], fill=(255, 220, 200, 10))
img.alpha_composite(haze.filter(ImageFilter.GaussianBlur(35)))
stars(img, 900, H, seed=7)
stars(img, 3500, H, seed=8, band=band)
d = ImageDraw.Draw(img)
r = np.random.default_rng(9)
x = 0
while x < W:
    h = r.uniform(120, 300)
    wdt = h * 0.28
    base = H - 30
    for layer in range(6):
        t = layer / 6
        d.polygon([(x - wdt * (1 - t), base - h * t), (x + wdt * (1 - t), base - h * t), (x, base - h * (t + 0.22))], fill=(4, 6, 12, 255))
    x += r.uniform(25, 70)
d.rectangle([0, H - 40, W, H], fill=(4, 6, 12, 255))
save(img, "milky-way.jpg")

# 8 forest lake
sky = vgrad([(0, (120, 180, 230)), (0.5, (220, 235, 240)), (1, (220, 235, 240))])
img = to_img(sky).convert("RGBA")
d = ImageDraw.Draw(img)
r = np.random.default_rng(11)
shore = 600
for row, (col, scale) in enumerate([((90, 130, 120), 0.6), ((50, 95, 75), 0.85), ((25, 60, 45), 1.1)]):
    x = r.uniform(-20, 20)
    while x < W + 40:
        h = r.uniform(120, 260) * scale
        wdt = h * 0.3
        for layer in range(5):
            t = layer / 5
            d.polygon([(x - wdt * (1 - t), shore - h * t), (x + wdt * (1 - t), shore - h * t), (x, shore - h * (t + 0.28))], fill=(*col, 255))
        x += r.uniform(18, 45) * scale
top = img.crop((0, 0, W, shore))
refl = top.transpose(Image.FLIP_TOP_BOTTOM).filter(ImageFilter.GaussianBlur(3))
water = Image.new("RGBA", (W, H - shore), (40, 80, 110, 255))
water.paste(refl.crop((0, 0, W, H - shore)), (0, 0))
tint = Image.new("RGBA", water.size, (30, 70, 100, 110))
water.alpha_composite(tint)
img.paste(water, (0, shore))
rip = Image.new("RGBA", img.size, (0, 0, 0, 0))
rd = ImageDraw.Draw(rip)
for _ in range(400):
    x, y = r.uniform(0, W), r.uniform(shore + 5, H)
    rd.line([(x, y), (x + r.uniform(30, 120), y)], fill=(230, 240, 245, 45), width=1)
img.alpha_composite(rip)
save(img, "forest-lake.jpg")

# 9 autumn hills
img = to_img(vgrad([(0, (110, 160, 210)), (0.55, (235, 220, 200)), (1, (235, 220, 200))])).convert("RGBA")
for i, col in enumerate([(205, 150, 90), (200, 110, 50), (175, 70, 40), (140, 50, 35), (95, 40, 30)]):
    ys = ridge(80 + i, 470 + i * 120, 55, [(1.1, 1), (2.6, 0.5), (6, 0.15)])
    fill_below(img, ys, col)
    dots = Image.new("RGBA", img.size, (0, 0, 0, 0))
    dd = ImageDraw.Draw(dots)
    r = np.random.default_rng(90 + i)
    for _ in range(260):
        x = r.uniform(0, W)
        y = ys[int(x) % W] + r.uniform(5, 110)
        s = r.uniform(4, 10) * (1 + i * 0.25)
        c = [(230, 170, 60), (210, 90, 40), (160, 50, 30), (120, 120, 50)][int(r.integers(0, 4))]
        dd.ellipse([x - s, y - s, x + s, y + s], fill=(*c, 160))
    img.alpha_composite(dots.filter(ImageFilter.GaussianBlur(1)))
save(img, "autumn-hills.jpg")

# 10 city lights
img = to_img(vgrad([(0, (10, 12, 35)), (0.6, (50, 40, 90)), (0.62, (10, 12, 30)), (1, (5, 6, 18))])).convert("RGBA")
stars(img, 150, 350, seed=12)
d = ImageDraw.Draw(img)
r = np.random.default_rng(13)
horizon = 660
x = 0
buildings = []
while x < W:
    bw, bh = r.uniform(40, 120), r.uniform(80, 420)
    buildings.append((x, bw, bh))
    x += bw + r.uniform(2, 10)
for x, bw, bh in buildings:
    d.rectangle([x, horizon - bh, x + bw, horizon], fill=(15, 16, 32, 255))
    for wy in range(int(horizon - bh + 10), horizon - 8, 14):
        for wx in range(int(x + 6), int(x + bw - 6), 12):
            if r.random() < 0.38:
                c = [(255, 215, 130), (255, 240, 200), (150, 200, 255)][int(r.integers(0, 3))]
                d.rectangle([wx, wy, wx + 5, wy + 7], fill=(*c, 255))
city = img.crop((0, 0, W, horizon))
refl = city.transpose(Image.FLIP_TOP_BOTTOM).filter(ImageFilter.GaussianBlur(4)).crop((0, 0, W, H - horizon))
water = Image.new("RGBA", (W, H - horizon), (8, 10, 24, 255))
water.paste(refl, (0, 0))
water.alpha_composite(Image.new("RGBA", water.size, (5, 8, 25, 130)))
img.paste(water, (0, horizon))
save(img, "city-lights.jpg")
