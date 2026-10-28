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

    # title and subtitle are drawn on their own layer and centred by their visible ink, since glyph boxes
    # include side bearings that push text off centre
    def paste_centred(layer, y_top):
        box = layer.getbbox()
        piece = layer.crop(box)
        img.paste(piece, (int(W / 2 - piece.width / 2), int(y_top)), piece)

    def line_layer(parts, spacing):
        probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
        width = sum(probe.textlength(ch, font=f) for ch, f, _ in parts) + spacing * (len(parts) - 1)
        layer = Image.new("RGBA", (int(width) + p(40), p(300)), (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        x = p(20)
        for ch, font, colour in parts:
            ld.text((x, p(150)), ch, font=font, fill=colour, anchor="lm")
            x += ld.textlength(ch, font=font) + spacing
        return layer

    title_font = ImageFont.truetype(FONT_BOLD, p(170))
    title_layer = line_layer([(ch, title_font, CREAM) for ch in "PPES"], 0)

    # full name under the acronym, its initials in the accent colour so the reader sees where PPES comes from
    text = "PAST & PRESENT EVENT SELECTOR"
    initials = {0, 7, 15, 21}
    size, max_w = p(46), p(900)
    while True:
        regular, bold = ImageFont.truetype(FONT_SEMI, size), ImageFont.truetype(FONT_BOLD, size)
        parts = [(ch, bold if i in initials else regular, ORANGE if i in initials else CREAM) for i, ch in enumerate(text)]
        spacing = size * 0.2
        layer = line_layer(parts, spacing)
        if layer.getbbox()[2] - layer.getbbox()[0] <= max_w:
            break
        size -= p(1)
    # one gap between icon, title and subtitle, chosen so the top and bottom margins match
    icon_box = ImageChops.difference(img, Image.new("RGB", img.size, BG)).getbbox()  # the background is not black
    title_h = title_layer.getbbox()[3] - title_layer.getbbox()[1]
    sub_h = layer.getbbox()[3] - layer.getbbox()[1]
    gap = (W - icon_box[1] - icon_box[3] - title_h - sub_h) / 2
    title_top = icon_box[3] + gap
    paste_centred(title_layer, title_top)
    paste_centred(layer, title_top + title_h + gap)

    out = ROOT / "release" / "PastPresentEventSelector-thumbnail.png"
    out.parent.mkdir(exist_ok=True)
    img.resize((1024, 1024), Image.LANCZOS).save(out, optimize=True)
    print(out)


if __name__ == "__main__":
    main()
