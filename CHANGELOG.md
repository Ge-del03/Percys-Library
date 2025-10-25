# Changelog - Percy's Library

## [Versión 2.0.0] - 2024-09-17

### ✨ Nuevas Características

#### 🎨 **Sistema de Temas Avanzado**
- Agregado ThemeManager con 5 temas profesionales
- Temas incluidos: Claro, Oscuro, Cómic, Sepia, Alto Contraste
- Cambio dinámico de temas sin reinicio
- Persistencia automática de preferencias de tema

#### 📚 **Soporte Extendido de Formatos**
- EnhancedComicPageLoader con optimizaciones de rendimiento
- Soporte mejorado para CBZ, CBR, CBT, CB7
- Compatibilidad básica con EPUB y PDF
- Ordenamiento natural inteligente de páginas
- Gestión avanzada de memoria y caché

#### 🔍 **Controles de Visualización Mejorados**
- AdvancedImageViewer con zoom profesional
- Múltiples modos de ajuste (Ancho, Alto, Página completa)
- Pan suave y controles de interacción optimizados
- Zoom con punto focal inteligente

#### 🔖 **Sistema de Marcadores Completo**
- BookmarkManager con persistencia XML
- Soporte para miniaturas de marcadores
- Seguimiento de progreso de lectura
- Gestión de favoritos con metadatos

#### 📁 **Explorador de Archivos Integrado**
- FileExplorerView con navegación avanzada
- Filtros por tipo de archivo de cómic
- Historial de carpetas recientes
- Búsqueda integrada de archivos

#### 🖼️ **Vista de Miniaturas**
- ThumbnailGridView con carga asíncrona
- Navegación rápida entre páginas
- Indicadores visuales de páginas marcadas
- Interfaz responsiva y optimizada

#### ⚙️ **Configuración Avanzada**
- AdvancedSettings con opciones profesionales
- Modos de lectura personalizables
- Configuraciones de rendimiento
- Optimizaciones de memoria configurables

### 🚀 **Mejoras de Rendimiento**

#### 💾 **Gestión de Memoria**
- Caché inteligente con limpieza automática
- Precarga optimizada de páginas cercanas
- Gestión eficiente de recursos de imagen
- Configuración de tamaño de caché personalizable

#### ⚡ **Carga Asíncrona**
- Todas las operaciones de E/O son no bloqueantes
- Indicadores de progreso durante la carga
- Cancelación de operaciones largas
- Manejo robusto de errores

### 🎯 **Mejoras de Usabilidad**

#### 🖱️ **Navegación Mejorada**
- Soporte completo para navegación por teclado
- Controles de ratón optimizados
- Atajos de teclado profesionales
- Navegación contextual intuitiva

#### 📱 **Interfaz Responsiva**
- Diseño adaptativo a diferentes tamaños de pantalla
- Elementos UI escalables
- Tooltips informativos
- Estados visuales claros

### 🔧 **Mejoras Técnicas**

#### 🏗️ **Arquitectura**
- Patrón MVVM implementado correctamente
- Separación clara de responsabilidades
- Inyección de dependencias
- Sistema de logging profesional

#### 🛠️ **Calidad de Código**
- Manejo robusto de excepciones
- Validaciones de entrada
- Documentación completa
- Tests unitarios preparados

### 🐛 **Correcciones**

#### 🔍 **Estabilidad**
- Corregido problema de restauración de ventana
- Mejorado manejo de archivos corruptos
- Solucionados memory leaks en carga de imágenes
- Estabilidad mejorada con archivos grandes

#### 💻 **Compatibilidad**
- Mejor soporte para diferentes formatos de archivo
- Manejo mejorado de rutas de archivo largas
- Compatibilidad con diferentes versiones de Windows
- Soporte para caracteres especiales en nombres de archivo

### 📋 **Dependencias Actualizadas**
- SharpCompress 0.35.0
- VersOne.Epub 3.3.1
- SixLabors.ImageSharp 3.1.0
- Microsoft.Extensions.* 8.0.0

---

## [Unreleased] - 2025-10-20

### ✅ Correcciones rápidas
- Corregidos errores de compilación debido a archivos faltantes y firmas de interfaz. (Stubs agregados para `IComicSource` y `PrioritizedRenderer`)
- Eliminada advertencia en `MainWindow.cs` y alineada la implementación de `ComicPageLoader` con su interfaz.

### 🛠️ Mejoras internas
- Añadido `continuousReader.CacheManager` (implemetación ligera) para gestión de caché en memoria.
- Añadido `continuousReader.PerformanceLogger` y métricas en `ComicPageLoader` para medir latencias de carga y decodificación.
 - Ajustado límite de concurrencia para prefetch en `ComicPageLoader` a un máximo de 4 tareas concurrentes por defecto. Esto ayuda a reducir picos de CPU durante la decodificación de imágenes en discos rápidos.
 - Compat shim temporal para `CacheManager` añadido para minimizar cambios en call-sites; se recomienda refactorizar `ComicPageLoader` para usar la API explícita (`Set`, `TryGet`, `TryRemove`).

### 🎨 Interfaz y Animaciones (Unreleased)

- Añadidos controles para gestionar animaciones desde la UI (`SettingsWindow`) y persistencia en `AppSettings`:
	- `EnableAnimations` (master switch)
	- `EnableAnimationsReaderTopBar`, `EnableAnimationsReaderOverlay`, `EnableAnimationsButtons`, `EnableAnimationsPageTurn`
	- `KeepReaderOverlayVisible` (mantener overlay/topbar visible)

- Reemplazados varios contenidos emoji en botones del `ReaderTopBar` por iconos vectoriales (`Path`) y se mejoró `ReaderIconButtonStyle` con sombra y transformaciones para hover/press.

- Storyboards del modo lectura ahora se declaran como recursos en `Styles/ReadingMode.xaml` y se inician desde `MainWindow.cs` sólo cuando las flags correspondientes están activas. Esto evita errores por orden de carga y permite deshabilitar animaciones en tiempo de ejecución.

- Se añadieron convertidores y bindings defensivos para controlar micro-animaciones (hover/press) mediante `MultiBinding` y `AppSettings` expuesto en recursos de la aplicación.

Cambios relevantes en el código:
- `Styles/ReadingMode.xaml` — estilos y storyboards centralizados.
- `MainWindow.xaml` / `MainWindow.cs` — iconos vectoriales, control de overlay y arranque condicional de storyboards.
- `Views/SettingsWindow.xaml` — checkboxes para toggles de animación y persistencia de `KeepReaderOverlayVisible`.
- `Docs/ReadingMode_Customization.md` — documentación actualizada sobre el nuevo patrón de animaciones.


## [Versión 1.0.0] - 2024-08-15

### 🎉 **Lanzamiento Inicial**
- Funcionalidad básica de lectura de cómics
- Soporte para formatos CBZ y CBR
- Interfaz básica de navegación
- Configuraciones simples

---

## 🚀 **Próximas Versiones**

### [Versión 2.1] - Planificado
- [ ] Soporte completo para PDF con renderizado nativo
- [ ] Modo de lectura nocturno mejorado
- [ ] Sincronización básica en la nube
- [ ] Plugin system inicial

### [Versión 2.2] - Planificado
- [ ] Estadísticas de lectura
- [ ] Importación/exportación de biblioteca
- [ ] OCR básico para texto en cómics
- [ ] Mejoras de accesibilidad

### [Versión 3.0] - Futuro
- [ ] Aplicación móvil complementaria
- [ ] IA para recomendaciones
- [ ] Biblioteca compartida en red
- [ ] Realidad aumentada experimental

---

**¡Gracias por usar Percy's Library!** 📚✨