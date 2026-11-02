"""Draws release/PastPresentEventSelector-thumbnail.png (1024x1024), same look as the HideoutBackport thumbnail.

Usage: python -I tools/thumbnail.py
"""
import math
import pathlib

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parent.parent
S = 4  # supersampling
W = 1024 * S
BG = (17, 17, 17)
ORANGE = (242, 170, 60)
CREAM = (239, 235, 224)
FONT_BOLD = "C:/Windows/Fonts/segoeuib.ttf"
FONT_SEMI = "C:/Windows/Fonts/seguisb.ttf"


def p(v):
    return v * S


# Shared layout for every zezinho thumbnail: same icon height, title size and gaps, so the set looks alike
ICON_H, TITLE_CAP, SUB_CAP, GAP, MAX_TEXT_W = 470, 112, 54, 46, 860


def _cap_size(path, cap):
    """Font size whose capital letters are `cap` px tall at 1x."""
    box = ImageFont.truetype(path, 200).getbbox("H")
    return 200 * cap / (box[3] - box[1])


def _text_ink(parts, spacing):
    """The text on its own transparent layer, cropped to its visible ink so it centres exactly."""
    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    width = sum(probe.textlength(ch, font=f) for ch, f, _ in parts) + spacing * (len(parts) - 1)
    layer = Image.new("RGBA", (int(width) + p(40), p(300)), (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    x = p(20)
    for ch, font, colour in parts:
        ld.text((x, p(150)), ch, font=font, fill=colour, anchor="lm")
        x += ld.textlength(ch, font=font) + spacing
    return layer.crop(layer.getbbox())


def finish(img, out, title, sub, sub_initials=()):
    """`img` holds only the icon (supersampled). Scales it, sets the title and subtitle under it and saves."""
    box = ImageChops.difference(img, Image.new("RGB", img.size, BG)).getbbox()  # the background is not black
    icon = img.crop(box)
    k = p(ICON_H) / icon.height
    icon = icon.resize((round(icon.width * k), round(icon.height * k)), Image.LANCZOS)
    # keep the icon where it was drawn horizontally, so shapes with a side part (a crown, a button) stay balanced
    icon_x = round(W / 2 + (box[0] - W / 2) * k)

    size = p(_cap_size(FONT_BOLD, TITLE_CAP))
    while True:
        font = ImageFont.truetype(FONT_BOLD, round(size))
        title_img = _text_ink([(ch, font, CREAM) for ch in title], 0)
        if title_img.width <= p(MAX_TEXT_W):
            break
        size *= 0.97

    # the subtitle keeps its wide spacing when it fits; long ones tighten the spacing first, then shrink
    size = p(_cap_size(FONT_SEMI, SUB_CAP))
    while True:
        # the subtitle is bold too: thinner strokes smear when the Forge shrinks the image to a 192px card
        regular, bold = ImageFont.truetype(FONT_BOLD, round(size)), ImageFont.truetype(FONT_BOLD, round(size))
        base = CREAM if sub_initials else ORANGE
        parts = [(ch, bold if i in sub_initials else regular, ORANGE if i in sub_initials else base) for i, ch in enumerate(sub)]
        sub_img = None
        for factor in (0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2):
            sub_img = _text_ink(parts, size * factor)
            if sub_img.width <= p(MAX_TEXT_W):
                break
        if sub_img.width <= p(MAX_TEXT_W):
            break
        size *= 0.97

    total = icon.height + title_img.height + sub_img.height + 2 * p(GAP)
    top = (W - total) / 2
    canvas = Image.new("RGB", (W, W), BG)
    canvas.paste(icon, (icon_x, int(top)))
    y = top + icon.height + p(GAP)
    canvas.paste(title_img, (int(W / 2 - title_img.width / 2), int(y)), title_img)
    y += title_img.height + p(GAP)
    canvas.paste(sub_img, (int(W / 2 - sub_img.width / 2), int(y)), sub_img)
    out.parent.mkdir(parents=True, exist_ok=True)
    result = canvas.resize((1024, 1024), Image.LANCZOS)
    result.save(out, optimize=True)
    return result


def main():
    img = Image.new("RGB", (W, W), BG)
    d = ImageDraw.Draw(img)

    # counter-clockwise arrow: the "past" half of the name
    cx, cy, r, stroke = p(512), p(400), p(228), p(26)
    gap_start, gap_end = 200, 248
    d.arc([cx - r, cy - r, cx + r, cy + r], start=gap_end, end=gap_start + 360, fill=ORANGE, width=stroke)
    for angle in (gap_start,):  # round cap only on the tail; the head gets the triangle
        a = math.radians(angle)
        x, y = cx + (r - stroke / 2) * math.cos(a), cy + (r - stroke / 2) * math.sin(a)
        d.ellipse([x - stroke / 2, y - stroke / 2, x + stroke / 2, y + stroke / 2], fill=ORANGE)
    # going counter-clockwise the body lies behind the gap_end side, so the head sits there
    a = math.radians(gap_end)
    tip_base = (cx + (r - stroke / 2) * math.cos(a), cy + (r - stroke / 2) * math.sin(a))
    tangent = (math.sin(a), -math.cos(a))  # direction of travel when going counter-clockwise
    normal = (math.cos(a), math.sin(a))
    head_len, head_half = p(84), p(50)
    tip_base = (tip_base[0] - tangent[0] * p(6), tip_base[1] - tangent[1] * p(6))  # overlap the arc end so no gap shows
    tip = (tip_base[0] + tangent[0] * head_len, tip_base[1] + tangent[1] * head_len)
    left = (tip_base[0] + normal[0] * head_half, tip_base[1] + normal[1] * head_half)
    right = (tip_base[0] - normal[0] * head_half, tip_base[1] - normal[1] * head_half)
    d.polygon([tip, left, right], fill=ORANGE)

    # calendar: the "selector" half
    line = p(22)
    x0, y0, x1, y1 = p(392), p(300), p(632), p(520)
    d.rounded_rectangle([x0, y0, x1, y1], radius=p(30), outline=CREAM, width=line)
    d.line([x0 + line / 2, p(358), x1 - line / 2, p(358)], fill=CREAM, width=line)
    for rx in (p(462), p(562)):
        d.rounded_rectangle([rx - p(12), p(268), rx + p(12), p(330)], radius=p(12), fill=CREAM)
    cols, rows = [p(452), p(512), p(572)], [p(410), p(466)]
    for ri, ry in enumerate(rows):
        for ci, rx in enumerate(cols):
            selected = ri == 1 and ci == 1
            size = p(22) if selected else p(13)
            d.rounded_rectangle([rx - size, ry - size, rx + size, ry + size], radius=p(8 if selected else 5),
                                fill=ORANGE if selected else CREAM)

    out = ROOT / "release" / "PastPresentEventSelector-thumbnail.png"
    finish(img, out, "PPES", "PAST & PRESENT EVENT SELECTOR", sub_initials={0, 7, 15, 21})
    print(out)


if __name__ == "__main__":
    main()
