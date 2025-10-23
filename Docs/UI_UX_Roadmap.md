# UI/UX Roadmap — Percy's Library

Objetivo: modernizar y unificar la experiencia de usuario a través de todas las vistas, aplicando el tema cómic y buenas prácticas de accesibilidad.

## Fases y prioridades

### Fase 1 — Interfaces críticas (entregable: wireframes, controles, estilos)
- MainWindow (shell): mejorar header, navegación, y accesibilidad.
- HomeView: reorganizar contenido, cards y acciones rápidas.
- LibraryManagerWindow / FileExplorer: mejorar lectura en listas y filtros.
- ComicView / ContinuousComicView: actualizar chrome de lectura, controles flotantes y comportamiento de pantalla completa.
- SettingsHub / SettingsView / SettingsWindow: terminar diseño cómic completo e interacciones.

### Fase 2 — Componentes y patrones (entregable: ResourceDictionaries, ControlTemplates, icon set)
- ThumbnailGridView: mejorar rendimiento visual y focus states.
- Controls: SliderComic, ToggleComic, SettingsNavItem, SettingsCard.
- ToastWindow, RatingWindow, ReopenCompletedDialog: pulir micro‑interacciones.

### Fase 3 — Modo presentación y performance
- PresentationModeWindow, PreloadProgress: mejorar animaciones y feedback.
- Optimizar uso de DrawingBrush y BitmapCache para animaciones.

### Fase 4 — QA, accesibilidad y pruebas de usuario
- Tests de usabilidad, auditoría A11y, ajuste de contraste y soporte reduced-motion.

## Entregables por vista
- Wireframe de baja + alta fidelidad (SVG + MD) — 1 por vista.
- Componentes XAML reutilizables y ResourceDictionary actualizado.
- Lista de assets y conversiones XAML para iconos.
- Checklist A11y y tests (cuando aplique).

## Cronograma estimado (por sprint)
- Sprint 1 (3 días): MainWindow, HomeView, LibraryManager (wireframes y prototipo mínimo)
- Sprint 2 (3 días): ComicView y ContinuousComicView (lectura, controles)
- Sprint 3 (4 días): SettingsHub/View (finalización del tema) + ThumbnailGrid optimizaciones
- Sprint 4 (2 días): Dialogs, Toasts y polish general
- Sprint 5 (1–2 días): QA y tests de usabilidad

## Priorización recomendada
Empieza por `ComicView` y `SettingsHub` (alta visibilidad) y `ThumbnailGridView` (rendimiento). Luego atacar `MainWindow` según feedback.

---

Dime qué vista quieres priorizar ahora o si quieres que empiece por el Sprint 1 completo. Haré un plan detallado para cada vista y crearé wireframes + implementaciones incrementales en el repo.
