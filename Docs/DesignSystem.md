# Design System — Percy's Library (Comic Theme)

Breve guía y tokens para integrar el tema cómic en la aplicación.

## Tokens

- Colores:
  - background-dark: #0A0A0A
  - sidebar: #0F3A4B
  - panel-paper: #FFFDF2
  - accent-orange: #FF5A00
  - accent-yellow: #FFCC00
  - accent-green: #16C784
  - accent-blue: #5E4CFF
  - ink (stroke): #000000
  - muted-text: #BDBDBD

- Tipografías:
  - Display: Bangers, fallback Segoe UI
  - Body: Comic Neue, fallback Segoe UI

- Spacing:
  - small: 6 px
  - base: 12 px
  - large: 24 px

- Borders:
  - ink-thick: 6 px (panels)
  - ink-medium: 3 px (cards)
  - ink-thin: 1.5 px (decor)

## Component guidelines

- NavigationBadge
  - Use bold fill for active (accent-orange) and ink burst around it.
  - Size: 84×84
  - Accessibility: Provide AutomationProperties.Name and keyboard focus.

- ComicPanel
  - Paper background with heavy ink border.
  - Inner padding large to create "viñeta" feeling.

- Slider
  - Thumb should be an identifiable icon (puño) and respond to keyboard.
  - Use gradient track from green->red to imply level.

- Buttons (Action)
  - Explosive outline; primary colors represent action semantics: apply=green, save=blue, cancel=red.
  - Micro-interaction: small shake on hover and quick press animation.

## Accessibility

- Ensure contrast >= 4.5:1 for labels and interactive text.
- Provide high-contrast variants and a reduced-motion preference.
- Provide proper focus states and clear keyboard navigation.

## Patterns

- Progressive disclosure: keep advanced controls in collapsed sections.
- Live preview: any appearance change should be previewable before saving.

## Implementation notes

- Keep tokens in XAML ResourceDictionaries; reference from App.xaml.
- Convert SVG icons to DrawingImage or XAML paths for performance.
- Use BitmapCache where many vector elements animate.

---

Para expansiones: puedo generar componentes XAML más detallados (ControlTemplates), ejemplos de tokens en ResourceDictionary y una librería de iconos convertidos a DrawingImage.
