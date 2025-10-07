# Percy's Library - Lector Avanzado de Cómics

## Descripción
Percy's Library es una aplicación avanzada para Windows desarrollada en WPF .NET 6 que permite leer cómics en múltiples formatos con una interfaz moderna y funcionalidades profesionales.

## Características Principales

### 🎨 **Interfaz Moderna**

## Hacer Percy’s Library la app predeterminada para cómics e imágenes

Consulta `Docs/file-associations.md` para ver cómo asociar extensiones como .cbz, .cbr, .cb7, .cbt, .zip, .rar, .7z, .tar, .pdf, .epub, .jpg, .jpeg, .png, .gif, .bmp, .webp, .heic, .tif, .tiff, .avif a Percy’s Library. La aplicación ya soporta abrir un archivo pasado como argumento de línea de comandos.

### 📚 **Formatos Soportados**
- **Archivos Comprimidos**: CBZ (ZIP), CBR (RAR), CBT (TAR), CB7 (7Z)
- **Libros Electrónicos**: EPUB
- **Documentos**: PDF (soporte básico)
- **Imágenes**: JPG, PNG, GIF, BMP, TIFF, WebP, HEIC
- **Formatos Especializados**: DJVU (en desarrollo)

### ⚡ **Rendimiento Optimizado**
- Carga asíncrona de páginas
- Caché inteligente con gestión de memoria
- Precarga automática de páginas cercanas
- Ordenamiento natural de archivos
- Compresión y optimización de imágenes

### 🔖 **Gestión Avanzada**
- Sistema completo de marcadores y favoritos
- Historial de lectura con progreso
- Explorador integrado de archivos
- Miniaturas para navegación rápida
- Notas personalizadas por página

### ⚙️ **Configuración Completa**
- Modos de lectura personalizables
- Opciones de zoom y visualización
- Configuración de rendimiento
- Gestión de caché y memoria
- Personalización de interfaz

## Estructura del Proyecto

```
ComicReader/
├── App.cs                              # Aplicación principal
├── MainWindow.cs                       # Ventana principal
├── Services/
│   ├── AdvancedSettings.cs            # Sistema de configuración
│   ├── EnhancedComicPageLoader.cs     # Cargador optimizado
│   ├── CloudSyncService.cs            # Sincronización (futuro)
│   └── MetadataService.cs             # Gestión de metadatos
├── Models/
│   ├── EnhancedComicPage.cs           # Modelo avanzado de página
│   ├── BookmarkManager.cs             # Gestión de marcadores
│   └── RecentComic.cs                 # Cómics recientes
├── Views/
│   ├── FileExplorerView.cs            # Explorador de archivos
│   ├── ThumbnailGridView.cs           # Vista de miniaturas
│   ├── SettingsView.cs                # Configuración
│   └── HomeView.xml                   # Página de inicio
├── Controls/
│   └── AdvancedImageViewer.cs         # Visor avanzado de imágenes
├── Themes/
│   └── ThemeManager.cs                # Sistema de temas
├── Utils/
│   └── Logger.cs                      # Sistema de logging
└── Resources/
    ├── Images/                        # Recursos gráficos
    ├── Icons/                         # Iconos
    └── Themes/                        # Estilos XAML
```

## Tecnologías Utilizadas

- **.NET 6**: Framework moderno y eficiente
- **WPF**: Interfaz de usuario avanzada
- **SharpCompress**: Manejo de archivos comprimidos
- **VersOne.Epub**: Soporte para libros electrónicos
- **SixLabors.ImageSharp**: Procesamiento avanzado de imágenes
- **Microsoft.Extensions**: Inyección de dependencias y logging

## Instalación

### Requisitos del Sistema
- Windows 10/11 (64-bit)
- .NET 6.0 Runtime
- 100 MB de espacio libre
- 4 GB RAM recomendado

### Pasos de Instalación
1. Descargar la última versión desde Releases
2. Extraer el archivo ZIP
3. Ejecutar `ComicReader.exe`
4. (Opcional) Crear acceso directo en el escritorio

### Compilación desde Código Fuente
```bash
git clone https://github.com/usuario/ComicReader.git
cd ComicReader
dotnet build --configuration Release
dotnet run
```

## Uso Básico

### Abrir un Cómic
- **Drag & Drop**: Arrastra archivos directamente a la ventana
- **Menú Archivo**: Usar Ctrl+O para abrir archivo
- **Explorador Integrado**: Navegar desde la pestaña de archivos

### Navegación
- **Flechas**: ← → para páginas anterior/siguiente
- **Ratón**: Click izquierdo/derecho para navegar
- **Rueda del Ratón**: Zoom in/out
- **Doble Click**: Ajustar zoom automático

### Funciones Avanzadas
- **F11**: Modo pantalla completa
- **Ctrl + B**: Agregar/quitar marcador
- **Ctrl + T**: Mostrar miniaturas
- **Ctrl + E**: Abrir explorador de archivos
- **Ctrl + ,**: Configuración

## Configuración Avanzada

### Modos de Lectura
- **Página Simple**: Una página por vez
- **Página Doble**: Dos páginas lado a lado
- **Scroll Vertical**: Desplazamiento continuo
- **Ajuste Automático**: Adapta según el contenido

### Optimización de Rendimiento
- Tamaño de caché configurable (10-100 páginas)
- Precarga automática (1-10 páginas)
- Compresión de imágenes (50-100% calidad)
- Gestión inteligente de memoria

### Personalización Visual
- 5 temas incluidos + personalización
- Opciones de zoom (25% - 500%)
- Filtros de imagen (brillo, contraste, saturación)
- Modo de pantalla completa inmersivo

## Desarrollo y Contribución

### Arquitectura
- **MVVM Pattern**: Separación clara de responsabilidades
- **Dependency Injection**: Gestión profesional de dependencias
- **Async/Await**: Operaciones no bloqueantes
- **Memory Management**: Optimización automática de recursos

### Añadir Nuevos Formatos
1. Implementar en `EnhancedComicPageLoader`
2. Añadir extensión a `_supportedFormats`
3. Crear método `LoadXXXAsync()`
4. Actualizar documentación

### Testing
```bash
dotnet test
dotnet test --configuration Release --verbosity normal
```

## Solución de Problemas

### Problemas Comunes

**Error al abrir archivos RAR:**
- Instalar Visual C++ Redistributable
- Verificar que el archivo no esté corrupto

**Rendimiento lento:**
- Reducir tamaño de caché en Configuración
- Verificar espacio libre en disco
- Cerrar otras aplicaciones que consuman memoria

**Imágenes no se muestran:**
- Verificar formatos soportados
- Comprobar permisos de archivo
- Revisar logs en la carpeta de aplicación

### Logs del Sistema
Los logs se guardan en:
`%AppData%\ComicReader\Logs\`

### Restaurar Configuración
Eliminar carpeta: `%AppData%\ComicReader\Settings\`

## Roadmap

### Versión 2.1
- [ ] Soporte completo para PDF con renderizado nativo
- [ ] Integración con bibliotecas online
- [ ] Sincronización en la nube (OneDrive, Google Drive)
- [ ] Modo de lectura nocturno mejorado

### Versión 2.2
- [ ] Plugin system para formatos personalizados
- [ ] Reconocimiento OCR para texto en cómics
- [ ] Estadísticas avanzadas de lectura
- [ ] Importación/exportación de colección

### Versión 3.0
- [ ] Versión para Android/iOS
- [ ] Biblioteca compartida en red
- [ ] Inteligencia artificial para recomendaciones
- [ ] Realidad aumentada para experiencia inmersiva

## Licencia
Este proyecto está bajo la Licencia MIT. Ver archivo `LICENSE` para más detalles.

## Soporte
- **Issues**: GitHub Issues para reportar problemas
- **Discussions**: GitHub Discussions para preguntas
- **Email**: soporte@comicreader.dev
- **Discord**: [Servidor de la Comunidad](https://discord.gg/comicreader)

## Créditos
- Desarrollado por el equipo de Percy's Library
- Iconos por [Feather Icons](https://feathericons.com/)
- Bibliotecas de código abierto utilizadas (ver LICENSES.md)

---

**¡Disfruta leyendo tus cómics favoritos con Percy's Library!** 📚✨