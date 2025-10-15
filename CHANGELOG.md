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
# Changelog - Percy's Library

## [v1.0.0] - 2025-10-15
### Added
- Nueva pestaña "Completados" en la sección "Seguir leyendo" con carrusel de portadas.
- Insignia "Completado ✅" y progreso al 100% en tarjetas de completados.
- Botón "Releer" en cada tarjeta de Completados.
- Opciones por tarjeta: Eliminar, Valorar, Compartir.
- Migración automática de items a "Completados" cuando el progreso alcanza 100%.
- Persistencia de completados en archivo JSON (wrapper `ContinueStorage`).
- Notificaciones toast para confirmaciones y depuración.

### Fixed
- Correcciones de estilos XAML y recursos rotos en `RatingWindow`.
- Reajustes en `HomeView` para mostrar correctamente estado vacío y listas.

### Notes
- Se añadió instrumentación (Debug + Toast) en `ContinueReadingService.UpsertProgress` para verificar la migración automática.
- Recomendado: limpiar artifacts (`bin/`, `obj/`) del repo si se desea un historial más limpio.

---

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