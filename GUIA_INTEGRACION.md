# 🔧 GUÍA DE INTEGRACIÓN - Percy's Library v2.0

## Instrucciones para Integrar las Nuevas Características

Esta guía te ayudará a integrar todas las nuevas características en el MainWindow existente de Percy's Library.

---

## 📋 Resumen de Cambios Necesarios

### Archivos a Modificar
1. `MainWindow.cs` - Agregar métodos y comandos
2. `MainWindow.xaml` - Agregar botones y menús
3. `Commands/ReaderCommands.cs` - Agregar nuevos comandos (si existe)

---

## 1️⃣ Agregar Referencias en MainWindow.cs

### Paso 1.1: Agregar campos privados

Agrega estos campos al inicio de la clase `MainWindow`:

```csharp
// Managers para las nuevas características
private Models.AnnotationManager? _annotationManager;
private Models.CollectionManager? _collectionManager;
private Models.AchievementManager? _achievementManager;

// Referencias a ventanas (para evitar múltiples instancias)
private Views.AnnotationEditorWindow? _annotationWindow;
private Views.CollectionsWindow? _collectionsWindow;
private Views.AchievementsWindow? _achievementsWindow;
```

### Paso 1.2: Inicializar managers en el constructor

En el constructor de `MainWindow`, después de `InitializeComponent()`:

```csharp
// Inicializar managers de nuevas características
_annotationManager = new Models.AnnotationManager();
_collectionManager = new Models.CollectionManager();
_achievementManager = new Models.AchievementManager();

// Suscribirse a eventos de logros
_achievementManager.AchievementUnlocked += OnAchievementUnlocked;
```

---

## 2️⃣ Agregar Métodos de Apertura de Ventanas

### Método para Anotaciones

```csharp
private async void OpenAnnotationEditor()
{
    if (!_isComicOpen || _comicPageLoader == null)
    {
        MessageBox.Show("Abre un cómic primero", "Información", 
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }

    try
    {
        // Cerrar ventana anterior si existe
        _annotationWindow?.Close();

        // Obtener imagen de la página actual
        var imageData = await _comicPageLoader.GetPageImageAsync(_currentPageIndex);
        if (imageData == null)
        {
            MessageBox.Show("No se pudo cargar la página actual", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Abrir editor de anotaciones
        _annotationWindow = new Views.AnnotationEditorWindow();
        _annotationWindow.LoadPage(
            _comicPageLoader.FilePath, 
            _currentPageIndex + 1, 
            imageData
        );
        _annotationWindow.Owner = this;
        _annotationWindow.Show();

        // Registrar logro si es la primera anotación
        _achievementManager?.IncrementStat("first_annotation");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error al abrir editor de anotaciones: {ex.Message}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### Método para Colecciones

```csharp
private void OpenCollectionsWindow()
{
    try
    {
        // Cerrar ventana anterior si existe
        _collectionsWindow?.Close();

        // Abrir gestor de colecciones
        _collectionsWindow = new Views.CollectionsWindow();
        _collectionsWindow.Owner = this;
        _collectionsWindow.Show();

        // Registrar logro si es la primera vez
        _achievementManager?.IncrementStat("first_collection");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error al abrir colecciones: {ex.Message}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### Método para Logros

```csharp
private void OpenAchievementsWindow()
{
    try
    {
        // Cerrar ventana anterior si existe
        _achievementsWindow?.Close();

        // Abrir ventana de logros
        _achievementsWindow = new Views.AchievementsWindow();
        _achievementsWindow.Owner = this;
        _achievementsWindow.ShowDialog();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error al abrir logros: {ex.Message}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### Método para Exportación

```csharp
private void OpenExportDialog()
{
    if (!_isComicOpen || _comicPageLoader == null)
    {
        MessageBox.Show("Abre un cómic primero", "Información", 
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }

    try
    {
        var exportWindow = new Views.ExportWindow(_comicPageLoader, _currentPageIndex);
        exportWindow.Owner = this;
        
        if (exportWindow.ShowDialog() == true)
        {
            MessageBox.Show("Páginas exportadas correctamente", "Éxito", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error al exportar: {ex.Message}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### Método para Comparar Versiones

```csharp
private async void CompareComicVersions()
{
    if (_comicPageLoader == null)
    {
        MessageBox.Show("Abre un cómic primero", "Información", 
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }

    try
    {
        // Seleccionar segundo cómic
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Archivos de Cómic|*.cbz;*.cbr;*.cb7;*.cbt;*.zip;*.rar;*.7z;*.pdf;*.epub|Todos los archivos|*.*",
            Title = "Selecciona el cómic a comparar"
        };

        if (dialog.ShowDialog() != true)
            return;

        // Cargar segundo cómic
        var loader2 = new Services.ComicPageLoader();
        await loader2.LoadComicAsync(dialog.FileName);

        // Crear comparador
        var comparator = new Services.ComicComparator();

        // Mostrar progreso
        var progress = new Progress<double>(value =>
        {
            // Actualizar UI con progreso
            // Puedes agregar una barra de progreso en el StatusBar
        });

        // Realizar comparación
        var result = await comparator.CompareComicsAsync(_comicPageLoader, loader2, progress);

        // Mostrar resultados
        var report = comparator.GenerateComparisonReport(result);
        
        var reportWindow = new Window
        {
            Title = "Resultado de Comparación",
            Width = 600,
            Height = 500,
            Owner = this,
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = report,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    Padding = new Thickness(20),
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
        reportWindow.ShowDialog();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error al comparar: {ex.Message}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

---

## 3️⃣ Manejar Evento de Logros Desbloqueados

```csharp
private void OnAchievementUnlocked(object? sender, Models.Achievement achievement)
{
    // Mostrar notificación toast
    Dispatcher.Invoke(() =>
    {
        // Puedes usar un control Toast o simplemente mostrar un mensaje
        var notification = new Window
        {
            Title = "🏆 Logro Desbloqueado",
            Width = 350,
            Height = 150,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(46, 125, 50)),
            Topmost = true
        };

        var grid = new Grid { Margin = new Thickness(20) };
        
        var titleText = new TextBlock
        {
            Text = "🏆 Logro Desbloqueado",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var nameText = new TextBlock
        {
            Text = $"{achievement.IconEmoji} {achievement.Name}",
            FontSize = 14,
            Foreground = System.Windows.Media.Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 30, 0, 0)
        };
        
        var pointsText = new TextBlock
        {
            Text = $"+{achievement.Points} puntos",
            FontSize = 12,
            Foreground = System.Windows.Media.Brushes.LightGreen,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 55, 0, 0)
        };

        grid.Children.Add(titleText);
        grid.Children.Add(nameText);
        grid.Children.Add(pointsText);
        
        notification.Content = grid;
        
        // Auto-cerrar después de 3 segundos
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            notification.Close();
        };
        timer.Start();
        
        notification.Show();
    });
}
```

---

## 4️⃣ Registrar Logros en Acciones Existentes

Busca estos métodos existentes y agrega las líneas indicadas:

### En el método de abrir cómic
```csharp
// Después de cargar el cómic exitosamente
_achievementManager?.IncrementStat("first_comic");
```

### En el método de agregar marcador
```csharp
// Después de agregar el marcador
_achievementManager?.IncrementStat("first_bookmark");
_achievementManager?.IncrementStat("bookmark_master");
```

### En el método de cambiar de página
```csharp
// Al cambiar de página exitosamente
_achievementManager?.IncrementStat("read_1000_pages");
```

### En el método de cambiar tema
```csharp
// Al cambiar de tema
_achievementManager?.IncrementStat("theme_switcher");
```

---

## 5️⃣ Agregar Botones en MainWindow.xaml

### En la barra de herramientas principal

Busca la sección de botones (probablemente un `ToolBar` o `StackPanel`) y agrega:

```xaml
<!-- Botón de Anotaciones -->
<Button Name="AnnotationsButton" 
        Content="📝 Anotaciones"
        Width="120" Height="35"
        Margin="5,0"
        Style="{StaticResource ModernButtonStyle}"
        Click="AnnotationsButton_Click"
        ToolTip="Agregar anotaciones a la página (Ctrl+Shift+A)"/>

<!-- Botón de Colecciones -->
<Button Name="CollectionsButton" 
        Content="📚 Colecciones"
        Width="120" Height="35"
        Margin="5,0"
        Style="{StaticResource ModernButtonStyle}"
        Click="CollectionsButton_Click"
        ToolTip="Gestionar colecciones (Ctrl+Shift+C)"/>

<!-- Botón de Logros -->
<Button Name="AchievementsButton" 
        Content="🏆 Logros"
        Width="100" Height="35"
        Margin="5,0"
        Style="{StaticResource ModernButtonStyle}"
        Click="AchievementsButton_Click"
        ToolTip="Ver tus logros (Ctrl+Shift+L)"/>
```

### Agregar handlers en code-behind

```csharp
private void AnnotationsButton_Click(object sender, RoutedEventArgs e)
{
    OpenAnnotationEditor();
}

private void CollectionsButton_Click(object sender, RoutedEventArgs e)
{
    OpenCollectionsWindow();
}

private void AchievementsButton_Click(object sender, RoutedEventArgs e)
{
    OpenAchievementsWindow();
}
```

---

## 6️⃣ Agregar Menús en MainWindow.xaml

Si tienes un menú (probablemente `<Menu>` en la parte superior), agrega estos items:

```xaml
<!-- Menú Herramientas -->
<MenuItem Header="Herramientas">
    <MenuItem Header="📝 Anotaciones" 
              Click="AnnotationsButton_Click"
              InputGestureText="Ctrl+Shift+A"/>
    <MenuItem Header="📤 Exportar Páginas" 
              Click="ExportButton_Click"
              InputGestureText="Ctrl+E"/>
    <MenuItem Header="🔍 Comparar Versiones" 
              Click="CompareButton_Click"/>
    <Separator/>
    <MenuItem Header="⚙️ Configuración" 
              Click="SettingsButton_Click"
              InputGestureText="Ctrl+,"/>
</MenuItem>

<!-- Menú Biblioteca -->
<MenuItem Header="Biblioteca">
    <MenuItem Header="📚 Colecciones" 
              Click="CollectionsButton_Click"
              InputGestureText="Ctrl+Shift+C"/>
    <MenuItem Header="🏆 Logros" 
              Click="AchievementsButton_Click"
              InputGestureText="Ctrl+Shift+L"/>
    <MenuItem Header="⭐ Favoritos" 
              Click="FavoritesButton_Click"/>
    <MenuItem Header="📊 Estadísticas" 
              Click="StatsButton_Click"/>
</MenuItem>
```

### Agregar handlers

```csharp
private void ExportButton_Click(object sender, RoutedEventArgs e)
{
    OpenExportDialog();
}

private void CompareButton_Click(object sender, RoutedEventArgs e)
{
    CompareComicVersions();
}
```

---

## 7️⃣ Agregar Atajos de Teclado

En el constructor de `MainWindow`, después de los `CommandBindings` existentes:

```csharp
// Nuevos atajos de teclado
this.CommandBindings.Add(new CommandBinding(
    new RoutedCommand("OpenAnnotations", typeof(MainWindow), 
        new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift) }),
    (s, e) => OpenAnnotationEditor()));

this.CommandBindings.Add(new CommandBinding(
    new RoutedCommand("OpenCollections", typeof(MainWindow), 
        new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift) }),
    (s, e) => OpenCollectionsWindow()));

this.CommandBindings.Add(new CommandBinding(
    new RoutedCommand("OpenAchievements", typeof(MainWindow), 
        new InputGestureCollection { new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Shift) }),
    (s, e) => OpenAchievementsWindow()));

this.CommandBindings.Add(new CommandBinding(
    new RoutedCommand("ExportPages", typeof(MainWindow), 
        new InputGestureCollection { new KeyGesture(Key.E, ModifierKeys.Control) }),
    (s, e) => OpenExportDialog()));
```

---

## 8️⃣ Actualizar StatusBar (Opcional)

Si tienes un `StatusBar`, agrega indicadores:

```xaml
<StatusBar Grid.Row="2">
    <StatusBarItem>
        <TextBlock Name="PageStatusText" Text="Página 1/1"/>
    </StatusBarItem>
    <Separator/>
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="📝" ToolTip="Anotaciones"/>
            <TextBlock Name="AnnotationCountText" Text="0" Margin="3,0"/>
        </StackPanel>
    </StatusBarItem>
    <Separator/>
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🏆" ToolTip="Puntos de logros"/>
            <TextBlock Name="AchievementPointsText" Text="0" Margin="3,0"/>
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

### Actualizar contadores

```csharp
private void UpdateStatusBar()
{
    if (_comicPageLoader != null && _annotationManager != null)
    {
        // Actualizar contador de anotaciones
        var annotationCount = _annotationManager.GetAnnotationCount(_comicPageLoader.FilePath);
        AnnotationCountText.Text = annotationCount.ToString();
    }

    if (_achievementManager != null)
    {
        // Actualizar puntos de logros
        var points = _achievementManager.GetTotalPoints();
        AchievementPointsText.Text = points.ToString();
    }
}
```

Llama a `UpdateStatusBar()` después de cargar un cómic y después de cambiar de página.

---

## 9️⃣ Limpieza al Cerrar la Aplicación

En el evento `Window_Closing` o `OnClosed`:

```csharp
protected override void OnClosed(EventArgs e)
{
    // Cerrar ventanas secundarias
    _annotationWindow?.Close();
    _collectionsWindow?.Close();
    _achievementsWindow?.Close();

    // Guardar datos pendientes
    _annotationManager?.SaveAnnotations();
    _collectionManager?.SaveCollections();
    _achievementManager?.SaveProgress();

    base.OnClosed(e);
}
```

---

## 🎨 Estilos Necesarios

Asegúrate de tener estos estilos en tu `App.xaml` o en un archivo de recursos:

```xaml
<Style x:Key="ModernButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="#3498db"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="Padding" Value="15,8"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        CornerRadius="5"
                        Padding="{TemplateBinding Padding}">
                    <ContentPresenter HorizontalAlignment="Center"
                                    VerticalAlignment="Center"/>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#2980b9"/>
        </Trigger>
    </Style.Triggers>
</Style>

<Style x:Key="AccentButtonStyle" TargetType="Button" BasedOn="{StaticResource ModernButtonStyle}">
    <Setter Property="Background" Value="#27ae60"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#229954"/>
        </Trigger>
    </Style.Triggers>
</Style>

<Style x:Key="DangerButtonStyle" TargetType="Button" BasedOn="{StaticResource ModernButtonStyle}">
    <Setter Property="Background" Value="#e74c3c"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#c0392b"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

---

## ✅ Lista de Verificación Final

Antes de compilar, verifica que:

- [ ] Todos los archivos nuevos están incluidos en el proyecto
- [ ] Las referencias a System.Windows.Forms están agregadas (para FolderBrowserDialog)
- [ ] Los estilos están definidos en recursos
- [ ] Los event handlers están conectados
- [ ] Los managers están inicializados
- [ ] Los atajos de teclado están registrados
- [ ] La limpieza al cerrar está implementada
- [ ] Los logros se registran en acciones clave

---

## 🚀 Compilar y Probar

1. **Compilar el proyecto**: `dotnet build`
2. **Verificar errores**: Revisar la salida de compilación
3. **Ejecutar**: `dotnet run`
4. **Probar cada característica**:
   - ✅ Abrir cómic
   - ✅ Crear anotación
   - ✅ Crear colección
   - ✅ Ver logros
   - ✅ Exportar páginas
   - ✅ Comparar versiones

---

## 📞 Solución de Problemas

### Error: No se encuentra la clase
- Verifica que el namespace sea correcto
- Asegúrate de que el archivo esté incluido en el proyecto

### Error: Style no encontrado
- Verifica que los estilos estén en App.xaml
- Usa `x:Key` correcto

### Error: Evento no existe
- Verifica el nombre del método handler
- Asegúrate de que la firma sea correcta: `(object sender, RoutedEventArgs e)`

### Ventanas no se muestran
- Verifica que `Owner = this` esté configurado
- Usa `Show()` para ventanas modeless o `ShowDialog()` para modales

---

## 🎉 ¡Listo!

Una vez completados estos pasos, Percy's Library v2.0 estará completamente integrado y funcionando con todas las nuevas características.

**¡Disfruta tu aplicación mejorada!** 📚✨
