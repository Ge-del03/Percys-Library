# Percy's Library - Lector Avanzado de Cómics 📚✨

## Descripción
Percy's Library es una aplicación **profesional y completa** para Windows desarrollada en WPF .NET 8 que permite leer cómics en múltiples formatos con una interfaz moderna y funcionalidades avanzadas. La versión 2.0 incluye características innovadoras como anotaciones, colecciones, sistema de logros y exportación avanzada.

> **🆕 Versión 2.0**: ¡Ahora con anotaciones, colecciones, logros y mucho más! Ver [NUEVAS_CARACTERISTICAS.md](NUEVAS_CARACTERISTICAS.md)

## 🌟 Características Principales

### 🎨 **Interfaz Moderna y Personalizable**
- 30+ temas predefinidos (Batman, Spider-Man, Cosmic Marvel, Cyberpunk, etc.)
- Modo nocturno e inmersivo
- Interfaz adaptable y responsive
- Animaciones fluidas

### 📝 **Sistema de Anotaciones** ⭐ NUEVO
- Notas de texto, resaltados, flechas
- Dibujo libre sobre las páginas
- Formas geométricas (rectángulos, círculos)
- Colores y opacidad personalizables
- Búsqueda y organización con tags

### 📚 **Gestión de Colecciones** ⭐ NUEVO
- Crea colecciones personalizadas
- Organiza por series, géneros o autores
- Portadas y colores distintivos
- Estadísticas detalladas por colección

### 🏆 **Sistema de Logros** ⭐ NUEVO
- 25+ logros desbloqueables
- Categorías: Lectura, Colección, Organización, Exploración, Tiempo
- Logros secretos por descubrir
- Sistema de puntos acumulativos

### 📤 **Exportador Avanzado de Páginas** ⭐ NUEVO
- Exporta a PNG, JPEG, WebP
- Control de calidad y redimensionamiento
- Exportación por lotes
- Estimación de tamaño

### 🔍 **Comparador de Versiones** ⭐ NUEVO
- Compara dos versiones del mismo cómic
- Análisis página por página
- Detección automática de diferencias
- Reportes detallados

## Hacer Percy's Library la app predeterminada

Consulta `Docs/file-associations.md` para ver cómo asociar extensiones como .cbz, .cbr, .cb7, .cbt, .zip, .rar, .7z, .tar, .pdf, .epub, .jpg, .jpeg, .png, .gif, .bmp, .webp, .heic, .tif, .tiff, .avif a Percy's Library.

### 📁 **Formatos Soportados**
- **Archivos Comprimidos**: CBZ (ZIP), CBR (RAR), CBT (TAR), CB7 (7Z)
- **Libros Electrónicos**: EPUB
- **Documentos**: PDF (soporte completo)
- **Imágenes**: JPG, PNG, GIF, BMP, TIFF, WebP, HEIC, AVIF
- **Formatos Especializados**: DJVU (en desarrollo)

### ⚡ **Rendimiento Optimizado**
- Carga asíncrona de páginas con precarga inteligente
- Caché multinivel con gestión de memoria
- Precarga automática de páginas cercanas
- Ordenamiento natural de archivos
- Compresión y optimización de imágenes
- Renderizado acelerado por GPU

### 🔖 **Gestión Completa**
- Sistema completo de marcadores y favoritos
- Historial de lectura con progreso automático
- Explorador integrado de archivos
- Miniaturas de alta calidad
- Estadísticas de lectura detalladas
- Sincronización en la nube (próximamente)

### ⚙️ **Configuración Avanzada**
- Múltiples modos de lectura (página simple, doble página, continua)
- Dirección de lectura (izquierda-derecha, derecha-izquierda, vertical)
- Control completo de zoom y ajuste
- Configuración de rendimiento y caché
- Personalización total de la interfaz
- Atajos de teclado personalizables

## Estructura del Proyecto

```
ComicReader/
├── App.cs                              # Aplicación principal
├── MainWindow.cs                       # Ventana principal
├── Services/
│   ├── AdvancedSettings.cs            # Sistema de configuración
│   ├── ComicPageLoader.cs             # Cargador optimizado
│   ├── PageExporter.cs                # Exportador de páginas ⭐
│   ├── ComicComparator.cs             # Comparador de versiones ⭐
│   ├── CloudSyncService.cs            # Sincronización
│   ├── ReadingStatsService.cs         # Estadísticas de lectura
│   ├── ImageCacheService.cs           # Caché de imágenes
│   └── MetadataService.cs             # Gestión de metadatos
├── Models/
│   ├── ComicPage.cs                   # Modelo de página
│   ├── BookmarkManager.cs             # Gestión de marcadores
│   ├── Annotation.cs                  # Sistema de anotaciones ⭐
│   ├── Collection.cs                  # Gestor de colecciones ⭐
│   ├── Achievement.cs                 # Sistema de logros ⭐
│   ├── FavoritesModels.cs             # Modelos de favoritos
│   ├── LibraryModels.cs               # Modelos de biblioteca
│   └── RecentComic.cs                 # Cómics recientes
├── Views/
│   ├── HomeView.xaml                  # Página de inicio
│   ├── AnnotationEditorWindow.xaml    # Editor de anotaciones ⭐
│   ├── CollectionsWindow.xaml         # Gestor de colecciones ⭐
│   ├── AchievementsWindow.xaml        # Ventana de logros ⭐
│   ├── ExportWindow.xaml              # Exportador de páginas ⭐
│   ├── SettingsView.xaml              # Configuración
│   └── ThumbnailPanelWindow.xaml      # Panel de miniaturas
├── Controls/
│   └── Advanced controls              # Controles personalizados
├── Themes/
│   ├── DarkTheme.xaml                 # Tema oscuro
│   ├── ComicTheme.xaml                # Temas de superhéroes
│   ├── CyberpunkTheme.xaml            # Tema cyberpunk
│   └── 27+ more themes...             # Más temas
├── Utils/
│   └── Logger.cs                      # Sistema de logging
└── Resources/
    ├── Images/                        # Recursos gráficos
    ├── Icons/                         # Iconos
    └── Themes/                        # Estilos XAML
```

## Tecnologías Utilizadas

- **.NET 8**: Framework moderno y eficiente
- **WPF**: Interfaz de usuario avanzada
- **SharpCompress**: Manejo de archivos comprimidos
- **SixLabors.ImageSharp**: Procesamiento avanzado de imágenes
- **Docnet.Core**: Soporte completo para PDF
- **System.Drawing.Common**: Renderizado de imágenes
- **Microsoft.Extensions**: Inyección de dependencias y logging

## Instalación

### Requisitos del Sistema
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- 200 MB de espacio libre
- 4 GB RAM recomendado
- GPU con soporte DirectX 11+ (recomendado)

### Pasos de Instalación
1. Descargar la última versión desde Releases
2. Extraer el archivo ZIP
3. Ejecutar `PercysLibrary.exe`
4. (Opcional) Configurar asociaciones de archivos

### Compilación desde Código Fuente
```bash
git clone https://github.com/usuario/ComicReader.git
cd ComicReader
dotnet restore
dotnet build -c Release
```

## Uso Rápido

### Abrir un Cómic
- **Método 1**: Arrastra y suelta un archivo sobre la ventana
- **Método 2**: Usa Ctrl+O o el botón "Abrir"
- **Método 3**: Haz doble clic en un archivo asociado

### Navegación
- **Siguiente página**: Flecha derecha, Click derecho, Rueda del ratón
- **Página anterior**: Flecha izquierda, Click izquierdo
- **Ir a página**: Ctrl+G
- **Zoom**: Ctrl++ / Ctrl+- / Ctrl+0

### Anotaciones ⭐
1. Abre un cómic
2. Haz clic en el botón "📝 Anotaciones"
3. Selecciona una herramienta (texto, dibujo, etc.)
4. Dibuja o escribe sobre la página
5. Tus anotaciones se guardan automáticamente

### Colecciones ⭐
1. Ve a "Colecciones" en el menú
2. Crea una nueva colección con "+"
3. Agrega cómics arrastrándolos o usando "Agregar"
4. Organiza y gestiona tus colecciones

### Logros ⭐
- Ve a "🏆 Logros" para ver tu progreso
- Los logros se desbloquean automáticamente
- Algunos logros son secretos, ¡descúbrelos!

### Exportar Páginas ⭐
1. Abre un cómic
2. Ve a "Herramientas" → "Exportar Páginas"
3. Selecciona formato y opciones
4. Elige carpeta de destino
5. Haz clic en "Exportar"

## Atajos de Teclado

### Navegación
- `→` / `Space`: Siguiente página
- `←` / `Backspace`: Página anterior
- `Home`: Primera página
- `End`: Última página
- `Ctrl+G`: Ir a página específica

### Visualización
- `Ctrl++`: Acercar zoom
- `Ctrl+-`: Alejar zoom
- `Ctrl+0`: Zoom 100%
- `F11`: Modo pantalla completa
- `F12`: Modo inmersivo
- `Ctrl+T`: Mostrar/ocultar miniaturas
- `Ctrl+N`: Modo nocturno

### Gestión
- `Ctrl+O`: Abrir archivo
- `Ctrl+B`: Agregar marcador
- `Ctrl+F`: Agregar a favoritos
- `Ctrl+E`: Exportar páginas ⭐
- `Ctrl+Shift+A`: Editor de anotaciones ⭐
- `Ctrl+Shift+C`: Gestor de colecciones ⭐
- `Ctrl+Shift+L`: Ver logros ⭐

### Otros
- `F1`: Ayuda
- `Ctrl+,`: Configuración
- `Esc`: Salir de pantalla completa
- `Ctrl+Q`: Salir

## Configuración Avanzada

### Modos de Lectura
1. **Página Simple**: Una página a la vez (predeterminado)
2. **Doble Página**: Dos páginas lado a lado (estilo manga)
3. **Lectura Continua**: Scroll vertical continuo

### Dirección de Lectura
- **Izquierda a Derecha**: Para cómics occidentales
- **Derecha a Izquierda**: Para manga japonés
- **Vertical**: Para webtoons y manhwa

### Optimización de Rendimiento
- Ajusta el tamaño de caché en Configuración
- Reduce la calidad de miniaturas si tienes poco RAM
- Desactiva precarga en discos lentos

### Temas
Percy's Library incluye 30+ temas inspirados en:
- **Superhéroes**: Batman, Superman, Spider-Man, Iron Man, etc.
- **Cómics**: Marvel Cosmic, DC Classic, Golden Age
- **Géneros**: Cyberpunk, Steampunk, Horror, Neon
- **Clásicos**: Dark, Light, High Contrast
- **Especiales**: Matrix, Dracula, Joker, Wonder Woman

## Preguntas Frecuentes

**P: ¿Funciona con manga?**
R: ¡Sí! Soporta dirección de lectura derecha-izquierda y modo doble página.

**P: ¿Puedo usar esto en una tablet?**
R: Actualmente solo Windows, pero planeamos soporte táctil mejorado.

**P: ¿Las anotaciones modifican el archivo original?**
R: No, se guardan en una base de datos separada sin tocar los archivos.

**P: ¿Cuánto espacio ocupan las anotaciones?**
R: Muy poco, aproximadamente 1-5 KB por anotación.

**P: ¿Puedo sincronizar entre dispositivos?**
R: Próximamente mediante sincronización en la nube.

**P: ¿Soporta archivos RAR?**
R: Sí, mediante la biblioteca SharpCompress.

**P: ¿Hay límite en el tamaño de los archivos?**
R: No hay límite, pero archivos muy grandes (>1GB) pueden ser lentos.

**P: ¿Los logros se pueden resetear?**
R: Los logros son permanentes para mantener tu progreso.

## Contribuir

Percy's Library es un proyecto en constante evolución. Las contribuciones son bienvenidas:

1. Fork el repositorio
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

### Áreas de Contribución
- Nuevos formatos de archivo
- Más temas visuales
- Traducciones (i18n)
- Optimizaciones de rendimiento
- Corrección de bugs
- Documentación

## Roadmap

### Versión 2.1 (Próximamente)
- [ ] OCR para extracción de texto
- [ ] Búsqueda de texto en páginas
- [ ] Exportación a PDF multipágina
- [ ] Más formatos de anotación
- [ ] Modo colaborativo de anotaciones

### Versión 2.2
- [ ] Sincronización en la nube completa
- [ ] Aplicación móvil complementaria
- [ ] API pública para extensiones
- [ ] Integración con tiendas digitales
- [ ] Lector web

### Versión 3.0
- [ ] Soporte para VR/AR
- [ ] IA para recomendaciones
- [ ] Comunidad integrada
- [ ] Streaming de cómics
- [ ] Editor de metadatos visual

## Licencia

Este proyecto está bajo la Licencia MIT. Ver el archivo `LICENSE` para más detalles.

## Créditos

### Desarrollador Principal
- Tu nombre

### Bibliotecas de Terceros
- [SharpCompress](https://github.com/adamhathcock/sharpcompress)
- [ImageSharp](https://github.com/SixLabors/ImageSharp)
- [Docnet.Core](https://github.com/GowenGit/docnet)

### Inspiración
Gracias a la comunidad de lectores de cómics y manga por su feedback.

## Soporte

- **Documentación**: Ver carpeta `Docs/`
- **Issues**: [GitHub Issues](https://github.com/usuario/ComicReader/issues)
- **Discusiones**: [GitHub Discussions](https://github.com/usuario/ComicReader/discussions)
- **Email**: support@percyslibrary.com

---

**¡Disfruta leyendo con Percy's Library!** 📚✨

*"No todos los superhéroes usan capas, algunos simplemente leen cómics."*
