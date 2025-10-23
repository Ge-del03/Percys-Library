Lista de assets recomendados para tema "Percy's Library - Comic"

Carpeta sugerida: Assets/ComicTheme/

1) Icons (SVG) - trazo grueso, 24–128px:
   - pow.svg (onomatopeya) - usado en botones de acción
   - fist_thumb.svg (slider thumb) - 28x28
   - gear.svg (ico general) - 40x40
   - pin.svg (tachuela) - 16x16
   - explosion_outline.svg (bordes dentados) - 200x200

2) Textures:
   - ben_day_dot_1x.png (tile) - 256x256, opacidad baja
   - paper_noise_2x.png - 1024x1024, baja opacidad

3) Backdrops / Banner:
   - header_ribbon.svg - escalable, con tachuelas en los extremos
   - stamp_seal.svg - circular para confirmaciones

4) Export guidelines:
   - Mantener trazos en 3–6 px equivalentes a 72dpi
   - Exportar al menos versiones 1x, 2x
   - Preferir SVG para elementos de UI; raster para texturas y ruido

5) Uso en WPF:
   - Convertir iconos SVG a DrawingImage mediante SharpVectors o a XAML Path para mejor rendimiento.
   - Texturas usar TileBrush con baja opacidad.

6) Licencias y fuentes:
   - Fuentes sugeridas: Bangers (Google Fonts), Comic Neue
   - Verificar licencias para redistribución si el proyecto distribuye instaladores.
