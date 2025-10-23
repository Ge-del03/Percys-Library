# Asset Exports — Percy's Library (Comic Theme)

Guía para exportar assets optimizados y convertir SVG a XAML DrawingImage.

## SVG exports
- Export vector icons as SVG with strokes outlined (expand strokes) and text convertido a paths.
- Provide 1x and 2x raster PNG fallbacks for environments that don't support SVG.

## Convert SVG -> XAML DrawingImage
- Option A (recommended): Use Inkscape or Illustrator to export a XAML/WPF resource or use `svg2xaml` tools.
- Option B: Add NuGet `SharpVectors.Runtime` and `SharpVectors.Renderers.Wpf` and load SVGs at runtime.

## Ben-Day tile
- Export as SVG 128x128 tile and use it as ImageBrush TileMode=Tile or convert to DrawingBrush.

## Naming & paths
- SVGs en `Assets/ComicTheme/` y iconos simples en `Assets/Icons/`.
- Mantener nombres con guion bajo y minúsculas: pow_high.svg, pin_tack.svg, ben_day_tile.svg.

## Example: XAML TileBrush (ben day)
<pre>
&lt;ImageBrush ImageSource="/Assets/ComicTheme/ben_day_tile.svg" TileMode="Tile" Viewport="0,0,64,64" ViewportUnits="Absolute" Opacity="0.06"/&gt;
</pre>

## Nota
- Para producción, prefiero convertir a DrawingImage y empaquetarlo en un ResourceDictionary para evitar dependencias.
