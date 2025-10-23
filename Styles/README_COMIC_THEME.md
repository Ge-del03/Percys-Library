Uso rápido - Comic Theme

Archivos añadidos:
- Styles/ComicTheme.xaml: Tokens de color, estilos base (NavigationBadgeButton, ExplosionActionButton, ComicPanelStyle)
- Styles/ComicControls.xaml: ControlTemplates básicos para Slider y Toggle usando assets en Assets/Icons/
- Assets/Icons/pow.svg: Onomatopeya 'POW' vector
- Assets/Icons/fist_thumb.svg: Thumb de slider (puño)

Cómo incluir en App.xaml:

1) Abrir App.xaml y mezclar los resource dictionaries:
   <Application.Resources>
     <ResourceDictionary>
       <ResourceDictionary.MergedDictionaries>
         <ResourceDictionary Source="/Styles/ComicTheme.xaml" />
         <ResourceDictionary Source="/Styles/ComicControls.xaml" />
       </ResourceDictionary.MergedDictionaries>
     </ResourceDictionary>
   </Application.Resources>

2) En tus Views, usar las keys: ComicPanelStyle, NavigationBadgeButton, ExplosionActionButton, ComicSlider, ComicToggle.

Notas:
- Los assets SVG se referencian directamente en Image.Source; WPF trata ciertos SVGs si usas un paquete (SharpVectors) o conviertes a XAML DrawingImage. Si hay problemas al cargar SVG, convierte a PNG o usa SharpVectors.
- Para textura Ben-Day y bordes irregulares, prefiere PNGs tiling o máscaras vectoriales. Mantén versiones 2x/3x para pantallas de alta DPI.
- Respeta SystemParameters.ClientAreaAnimation para reducir animaciones cuando el usuario lo solicita.

Siguientes pasos sugeridos:
- Convertir los SVG a DrawingImage para soporte nativo en WPF (o agregar dependencia SharpVectors).
- Añadir un pequeño ViewModel para enlazar la navegación y estados de botones.
