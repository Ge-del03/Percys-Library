# Sistema de Colores Dinámicos para Botones

## Descripción General

Este sistema permite que los botones de la aplicación cambien automáticamente de color según el tema seleccionado, asegurando un contraste visual adecuado y una experiencia de usuario coherente.

## Cómo Funciona

### 1. **Recursos Dinámicos en BaseStyles.xaml**

Los estilos de botones utilizan `DynamicResource` para los colores:

```xaml
<Style x:Key="PrimaryButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{DynamicResource ButtonPrimaryBrush}"/>
    <Setter Property="Foreground" Value="{DynamicResource ButtonPrimaryTextBrush}"/>
</Style>

<Style x:Key="SecondaryButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{DynamicResource ButtonSecondaryBrush}"/>
    <Setter Property="Foreground" Value="{DynamicResource ButtonSecondaryTextBrush}"/>
</Style>

<Style x:Key="DangerButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{DynamicResource ButtonDangerBrush}"/>
    <Setter Property="Foreground" Value="{DynamicResource ButtonDangerTextBrush}"/>
</Style>
```

### 2. **Aplicación Automática al Cambiar Tema**

Cuando cambias de tema en `Configuración → Apariencia`, el método `ApplyDynamicButtonColors()` en `App.cs` actualiza automáticamente los colores de todos los botones.

## Tipos de Botones

### 🔵 **Primary Buttons** (Botones Principales)
- **Uso**: Acciones principales como "Abrir", "Guardar", "Aceptar"
- **Recursos**: `ButtonPrimaryBrush`, `ButtonPrimaryTextBrush`
- **Ejemplos**: 
  - 📂 ABRIR
  - ✅ ACEPTAR
  - 💾 GUARDAR

### ⚪ **Secondary Buttons** (Botones Secundarios)
- **Uso**: Acciones secundarias, navegación, opciones
- **Recursos**: `ButtonSecondaryBrush`, `ButtonSecondaryTextBrush`
- **Ejemplos**:
  - ⟨ ANTERIOR / SIGUIENTE ⟩
  - ⚙️ CONFIGURACIÓN
  - 📚 BIBLIOTECA

### 🔴 **Danger Buttons** (Botones de Peligro)
- **Uso**: Acciones destructivas o de advertencia
- **Recursos**: `ButtonDangerBrush`, `ButtonDangerTextBrush`
- **Ejemplos**:
  - ❌ CERRAR
  - 🗑️ ELIMINAR
  - ⚠️ CANCELAR

## Paleta de Colores por Tema

### 🦇 Temas DC Comics

#### **Batman** (Dorado y Negro)
- **Primary**: `#FFD700` (Oro) con texto negro
- **Secondary**: `#1A1A1A` (Negro) con texto dorado
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Superman** (Rojo y Azul)
- **Primary**: `#E62020` (Rojo) con texto blanco
- **Secondary**: `#0052A5` (Azul) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Green Lantern** (Verde Brillante)
- **Primary**: `#00FF00` (Verde neón) con texto negro
- **Secondary**: `#0C4C0C` (Verde oscuro) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Wonder Woman** (Rojo y Azul + Dorado)
- **Primary**: `#DC143C` (Carmesí) con texto blanco
- **Secondary**: `#003087` (Azul) con texto dorado
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Flash** (Rojo y Amarillo)
- **Primary**: `#FF0000` (Rojo) con texto blanco
- **Secondary**: `#FFD700` (Dorado) con texto negro
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Aquaman** (Naranja y Azul Marino)
- **Primary**: `#FF8C00` (Naranja) con texto negro
- **Secondary**: `#006994` (Azul marino) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Joker** (Morado y Verde)
- **Primary**: `#9B59B6` (Morado) con texto blanco
- **Secondary**: `#2ECC71` (Verde) con texto negro
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Harley Quinn** (Rosa y Azul Cielo)
- **Primary**: `#FF1493` (Rosa intenso) con texto blanco
- **Secondary**: `#00BFFF` (Azul cielo) con texto negro
- **Danger**: `#8B0000` (Rojo oscuro)

### 🕷️ Temas Marvel

#### **Spider-Man** (Rojo y Azul)
- **Primary**: `#E62020` (Rojo) con texto blanco
- **Secondary**: `#0052A5` (Azul) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Iron Man** (Dorado y Rojo)
- **Primary**: `#FFD700` (Dorado) con texto negro
- **Secondary**: `#DC143C` (Carmesí) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Captain America** (Azul y Rojo)
- **Primary**: `#0052A5` (Azul) con texto blanco
- **Secondary**: `#E62020` (Rojo) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Hulk** (Verde y Morado)
- **Primary**: `#2ECC71` (Verde) con texto negro
- **Secondary**: `#8B008B` (Morado oscuro) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Thor** (Dorado y Azul)
- **Primary**: `#FFD700` (Dorado) con texto negro
- **Secondary**: `#0052A5` (Azul) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Black Panther** (Morado y Negro)
- **Primary**: `#9B59B6` (Morado) con texto blanco
- **Secondary**: `#1A1A1A` (Negro) con texto morado
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Deadpool** (Rojo y Negro)
- **Primary**: `#E62020` (Rojo) con texto blanco
- **Secondary**: `#1A1A1A` (Negro) con texto rojo
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Daredevil** (Carmesí)
- **Primary**: `#DC143C` (Carmesí) con texto blanco
- **Secondary**: `#8B0000` (Rojo oscuro) con texto blanco
- **Danger**: `#8B0000` (Rojo oscuro)

### ✨ Temas Especiales

#### **Cyberpunk** (Magenta y Cian)
- **Primary**: `#FF00FF` (Magenta) con texto negro
- **Secondary**: `#00FFFF` (Cian) con texto negro
- **Danger**: `#FF0000` (Rojo)

#### **Retro** (Naranja y Amarillo)
- **Primary**: `#FF6B35` (Naranja) con texto negro
- **Secondary**: `#F7B801` (Amarillo) con texto negro
- **Danger**: `#8B0000` (Rojo oscuro)

#### **Neon** (Rosa y Verde Neón)
- **Primary**: `#FF1493` (Rosa neón) con texto negro
- **Secondary**: `#00FF00` (Verde neón) con texto negro
- **Danger**: `#FF0000` (Rojo)

#### **Dracula** (Morados y Rosas)
- **Primary**: `#BD93F9` (Morado claro) con texto oscuro
- **Secondary**: `#FF79C6` (Rosa) con texto oscuro
- **Danger**: `#FF5555` (Rojo)

#### **Nord** (Azules Nórdicos)
- **Primary**: `#88C0D0` (Azul claro) con texto oscuro
- **Secondary**: `#5E81AC` (Azul) con texto claro
- **Danger**: `#BF616A` (Rojo nórdico)

#### **Solarized Dark** (Azules Solarized)
- **Primary**: `#268BD2` (Azul) con texto claro
- **Secondary**: `#2AA198` (Cian) con texto claro
- **Danger**: `#DC322F` (Rojo)

#### **Solarized Light** (Luminoso)
- **Primary**: `#268BD2` (Azul) con texto claro
- **Secondary**: `#2AA198` (Cian) con texto oscuro
- **Danger**: `#DC322F` (Rojo)

### 🎨 Temas Clásicos

#### **Dark** (Azul Moderno)
- **Primary**: `#3B82F6` (Azul) con texto blanco
- **Secondary**: `#6B7280` (Gris) con texto blanco
- **Danger**: `#EF4444` (Rojo)

#### **Light** (Azul Profesional)
- **Primary**: `#2563EB` (Azul) con texto blanco
- **Secondary**: `#6B7280` (Gris) con texto oscuro
- **Danger**: `#DC2626` (Rojo)

#### **Comic** (Naranja y Turquesa)
- **Primary**: `#FF6B35` (Naranja) con texto negro
- **Secondary**: `#4ECDC4` (Turquesa) con texto negro
- **Danger**: `#E63946` (Rojo)

#### **Sepia** (Tonos Marrones)
- **Primary**: `#D2691E` (Chocolate) con texto sepia claro
- **Secondary**: `#8B4513` (Marrón silla) con texto sepia claro
- **Danger**: `#A0522D` (Siena)

#### **High Contrast** (Alto Contraste)
- **Primary**: `#FFFF00` (Amarillo) con texto negro
- **Secondary**: `#00FF00` (Verde) con texto negro
- **Danger**: `#FF0000` (Rojo)

## Cómo Probar

1. **Abre la aplicación**
2. Ve a **⚙️ CONFIGURACIÓN → 🎨 Apariencia**
3. **Cambia entre diferentes temas**
4. Observa cómo los botones cambian de color automáticamente
5. Verifica que:
   - ✅ Los colores contrastan bien con el fondo
   - ✅ El texto es legible
   - ✅ Los botones son visualmente distinguibles

## Agregar Nuevos Temas

Si deseas agregar un nuevo tema con colores personalizados para botones:

1. Abre `App.cs`
2. Localiza el método `ApplyDynamicButtonColors()`
3. Agrega una nueva entrada al diccionario `buttonColors`:

```csharp
{ "miTema", ("#PrimaryColor", "#SecondaryColor", "#DangerColor", "#PrimaryText", "#SecondaryText") },
```

4. Los colores se aplicarán automáticamente cuando el usuario seleccione ese tema

## Recursos Aplicados

El sistema actualiza 9 recursos dinámicos:

| Recurso | Descripción |
|---------|-------------|
| `ButtonPrimaryBrush` | Color de fondo del botón principal |
| `ButtonPrimaryTextBrush` | Color del texto del botón principal |
| `ButtonPrimaryBorderBrush` | Color del borde del botón principal |
| `ButtonSecondaryBrush` | Color de fondo del botón secundario |
| `ButtonSecondaryTextBrush` | Color del texto del botón secundario |
| `ButtonSecondaryBorderBrush` | Color del borde del botón secundario |
| `ButtonDangerBrush` | Color de fondo del botón de peligro |
| `ButtonDangerTextBrush` | Color del texto del botón de peligro |
| `ButtonDangerBorderBrush` | Color del borde del botón de peligro |

## Notas Técnicas

- **DynamicResource vs StaticResource**: Se usa `DynamicResource` para permitir cambios en tiempo de ejecución
- **Conversión de colores**: Los colores hexadecimales se convierten a `SolidColorBrush` usando `ColorConverter`
- **Aplicación automática**: Los colores se aplican inmediatamente después de cargar el tema
- **Logging**: Cada aplicación de colores se registra en el log para debugging

## Ventajas del Sistema

✅ **Consistencia visual**: Todos los botones siguen la paleta del tema  
✅ **Contraste adecuado**: Colores diseñados para contrastar con cada tema  
✅ **Actualización instantánea**: Los cambios se aplican sin reiniciar  
✅ **Fácil mantenimiento**: Todos los colores en un solo lugar  
✅ **Extensible**: Agregar nuevos temas es simple  

---

**Última actualización**: Octubre 2025  
**Sistema implementado en**: `App.cs`, `BaseStyles.xaml`
