# Settings UI

Este documento describe los tokens y cómo personalizar la UI de Settings.

- Tokens: `design/tokens/theme-tokens.json`
- Resource dictionaries: `Themes/SettingsTokens.xaml`, `Styles/Controls.xaml`

Cómo añadir un nuevo theme:
1. Añadir `Themes/MyThemeTheme.xaml`.
2. Asegurarse de que el nombre de theme sea `MyTheme` (sin sufijo `Theme`).
3. Reiniciar la app o llamar a `App.ApplyTheme("MyTheme")`.

Accesibilidad:
- Asegúrate de mantener contraste suficiente y focus visible.

Próximos pasos:
- Crear thumbnails para temas en `Assets/Settings/`.
- Añadir traducciones adicionales en `Resources/`.
