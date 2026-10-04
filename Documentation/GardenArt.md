# Brick Garden artwork

The macOS game uses a 2.5D presentation: a painted environment plate and transparent crystal sprites, with live tile picking, selection tint, merge animation, particles, score and rules. The reference is the original Crystal Garden concept, saved as `Assets/Resources/Art/HubBrickGarden.png`.

Final assets generated with the built-in `image_gen` tool:

- `/Users/espitman/Documents/Game/iTetris/Assets/Resources/Art/GardenPlate.png`
- `/Users/espitman/Documents/Game/iTetris/Assets/Resources/Art/GardenCrystals.png`

The atlas is sliced into eight sprites at runtime. The playable board follows the 4 × 4 pedestal arrangement in the resulting reference plate. The selected stone receives a translucent turquoise tint; there is no outline.

## Environment prompt

Use case: precise-object-edit. This reference is the exact visual design for a playable crystal garden game. Create its clean game background plate. Preserve the camera, framing, 16:10 composition, aurora, lake, snowy mountains, floating rocky garden island, foliage, polished stone pedestals and brass pedestal rims. Remove ALL crystals from the tops of the pedestals, restoring clean dark polished stone surfaces. Remove ALL interface, all lettering, title, score, next preview panel, slogans, selection glow outlines, glowing selection arrows and icons. Crucially make the playable pedestals a precise complete 5 by 5 isometric grid, 25 equal distinct square stone pedestal tops, diamond projected into the image. The grid's topmost center should be at 50% image width and 25% image height; bottommost center at 50% width and 75% image height; leftmost center at 21% width and 50% image height; rightmost center at 79% width and 50% image height. Keep the same lush rocky platform, ferns and tiny white flowers around the perimeter, reflecting in the water below. Do not flatten the 3D rocky island into a geometric block. Keep high visual fidelity, rich dark glossy stone, slender gold edge highlights. No crystals atop any tile, no text anywhere.

The image returned a 4 × 4 grid, matching the original concept; the game geometry and rules use that grid.

## Crystal atlas prompt

Use case: stylized-concept. Game-ready sprite atlas on genuinely transparent background, 4 equal columns by 2 equal rows, eight distinct crystal growth stages, each centered within its own equally sized rectangular cell with generous transparent margins and no overlap across cells. No lettering, no labels, no frames, no pedestals, no shadow outside the cell. Front-facing three-quarter isometric camera with slight top visibility. Premium realistic faceted translucent mineral glass crystals matching a magical alpine aurora garden, glossy sparkling polygonal facets, visible inner refraction, sharp brilliant white edge highlights, elegant and natural, NOT flat cartoon pillars. TOP ROW left to right: (1) a single small cyan crystal shard, (2) a violet cluster with three medium pointed crystals, (3) a sapphire blue cluster with five pointed crystals, (4) a bright emerald green crystal bloom with six shards. BOTTOM ROW left to right: (5) a golden amber crystal bloom, (6) a rose pink crystal bloom, (7) a silver icy blue elaborate crystal bloom, (8) a pale warm white luminous ultimate crystal bloom. Every crystal cluster must have its base at the same lower-middle position within each cell and rise upward, and be fully contained within its cell. Consistent camera, lighting, scale and centered cell alignment. Eight sprites in exactly 4 by 2 aligned grid. Background entirely transparent.

## Final transparency edit prompt

Remove the ENTIRE background behind these eight crystals, including ALL black areas, colored gradients, haze, glow clouds, ground planes and shadows. Make the background genuinely completely transparent alpha=0 around the actual sharply faceted crystal silhouettes, including between shards. Preserve the eight crystal shapes, colors, lighting and exact 4 columns by 2 rows atlas arrangement and each crystal's position and size. Do not replace the background with a checkerboard or any opaque color. No glow outside silhouettes. This is a game sprite sheet requiring real transparent cutouts. Every non-crystal pixel must be transparent.
