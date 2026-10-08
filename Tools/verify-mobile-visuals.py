#!/usr/bin/env python3
"""Compare real Android captures with the approved source; require live digits.
Requires Pillow and numpy. Writes reference / runtime / amplified-diff images.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'Assets/Resources/Art/MobileUI/approved-reference.png'
OUT = ROOT / 'Documentation/VisualVerification'
OUT.mkdir(exist_ok=True)
reference = Image.open(SOURCE).convert('RGB')
assert reference.size == (2043, 770), reference.size
pages = {
    'Pause': ('MobilePause.png', (1359,121,334,631), {}),
    'Home': ('MobileHomeReference.png', (667,121,326,631), {'best': (810,684,87,26)}),
    'Launch': ('MobileLaunch.png', (315,121,334,631), {}),
    'GameOver': ('MobileGameOverReference.png', (1711,121,320,631), {
        'score': (1803,371,134,36), 'best': (1861,429,74,23)}),
}
report = {}
failed = False
for name, (filename, page, regions) in pages.items():
    actual = Image.open(ROOT / 'Documentation' / filename).convert('RGB')
    w,h = actual.size
    px,py,pw,ph = page
    scale = w/pw if name in ('Home','Launch') else max(w/pw,h/ph)
    origin_x = px + pw/2 - w/(2*scale)
    origin_y = py + ph/2 - h/(2*scale)
    expected = reference.transform((w,h),Image.Transform.AFFINE,
        (1/scale,0,origin_x,0,1/scale,origin_y), Image.Resampling.BILINEAR)
    comparison_top,comparison_bottom = 0,h
    if name in ('Home','Launch') and h > ph*scale:
        # The reference has no pixels for a taller phone's extra scenery. Test
        # the original footprint independently and disclose coverage; do not
        # construct expected scenery by mirroring the implementation.
        comparison_top = int(round((h-ph*scale)/2))
        comparison_bottom = h-comparison_top
        expected.paste('#081326',(0,0,w,comparison_top))
        expected.paste('#081326',(0,comparison_bottom,w,h))
    difference = np.abs(np.asarray(actual,dtype=float)-np.asarray(expected,dtype=float))
    compared = difference[comparison_top:comparison_bottom]
    metrics = {'mean_absolute_error': float(compared.mean()),
        'p95_channel_error': float(np.percentile(compared,95)),
        'reference_coverage': (comparison_bottom-comparison_top)/h,
        'comparison_rows': [comparison_top,comparison_bottom],
        'dimensions': [w,h], 'scale':scale, 'regions': {}}
    if metrics['mean_absolute_error'] > 3 or metrics['p95_channel_error'] > 10:
        failed = True
    for label,(x,y,rw,rh) in regions.items():
        bounds = tuple(int(round(v)) for v in ((x-origin_x)*scale,(y-origin_y)*scale,(x+rw-origin_x)*scale,(y+rh-origin_y)*scale))
        l,t,r,b = bounds
        region = difference[t:b,l:r]
        metrics['regions'][label] = {'mean_absolute_error':float(region.mean()),'bounds':bounds}
        # Numeric shapes must be inspected in their own region; a large scenic
        # background must never hide a different typeface in a whole-page score.
        if region.mean() > 5:
            failed=True
        close = Image.new('RGB',(3*(r-l),b-t))
        close.paste(expected.crop(bounds),(0,0));close.paste(actual.crop(bounds),(r-l,0))
        close.paste(Image.fromarray(np.clip(region*4,0,255).astype('uint8')),(2*(r-l),0))
        close.resize((900, max(1,round(close.height*900/close.width)))).save(OUT/f'{name}-{label}.png')
    display_difference = difference.copy()
    display_difference[:comparison_top]=0;display_difference[comparison_bottom:]=0
    diff_image = Image.fromarray(np.clip(display_difference*4,0,255).astype('uint8'))
    panel = Image.new('RGB',(w*3,h+60),'#071322')
    panel.paste(expected,(0,60));panel.paste(actual,(w,60));panel.paste(diff_image,(2*w,60))
    draw=ImageDraw.Draw(panel)
    draw.text((20,20),'APPROVED SOURCE (adapted to phone)',fill='white')
    draw.text((w+20,20),'REAL ANDROID CAPTURE',fill='white')
    draw.text((2*w+20,20),'ABSOLUTE DIFFERENCE x4',fill='white')
    panel.resize((1350,round(panel.height*1350/panel.width))).save(OUT/f'{name}.png')
    report[name] = metrics
report['passed'] = not failed
(OUT/'metrics.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
raise SystemExit(1 if failed else 0)
