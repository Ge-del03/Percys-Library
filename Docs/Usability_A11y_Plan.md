# Plan de Usabilidad & Accesibilidad — Percy's Library

## Objetivo
Validar que la nueva interfaz de configuración sea intuitiva, rápida de aprender y accesible para personas con diversas capacidades.

## Usuarios objetivo (personas)
- Lector casual: usa la app para lectura, necesita opciones sencillas.
- Power user: personaliza apariencia y comportamiento de lectura.
- Usuario con baja visión: requiere alto contraste y fuentes grandes.
- Usuario con movilidad reducida: navegación por teclado y tiempos de respuesta largos.

## Métricas clave
- Eficacia: % tareas completadas sin ayuda.
- Eficiencia: tiempo medio para cambiar tema/aplicar configuración.
- Satisfacción: SUS (System Usability Scale) / NPS corto.
- Accesibilidad: cumplimiento con checklist A11y (WCAG 2.1 AA para texto y controles).

## Tareas de prueba (moderadas)
1. Cambiar idioma a Inglés y aplicar cambios.
2. Ajustar volumen de efectos al 30% y verificar monitor de pruebas.
3. Cambiar tema y ver preview en tiempo real.
4. Restaurar la configuración por defecto.
5. Navegar y activar/desactivar interruptores usando solo teclado.

## Método
- 6–8 participantes representativos.
- Sesiones 45–60 min: introduction (5), tasks (30), post-test SUS (10), entrevista (10).
- Medir tiempo, éxito y observaciones.

## Checklist de accesibilidad (WCAG-related)
- [ ] Texto legible (ratio >= 4.5:1 para normal text)
- [ ] Tamaño táctil mínimo 44×44 px para controles touch
- [ ] Foco visible para todos los controles (outline de tinta)
- [ ] Soporte para teclado: Tab order claro, Enter/Space actúan en botones
- [ ] Animaciones: provide reduced-motion option
- [ ] Aria/AutomationProperties: nombres y descripciones para controles personalizados
- [ ] Etiquetas y ayuda contextual (ToolTip/HelpText) para controles avanzados

## Errores comunes a observar
- Usuarios no ven el botón Apply/Save porque está fuera de la pantalla (scrolled)
- Slider no responde a teclado
- Textos con fuentes decorativas imposibilitan lectura en tamaños pequeños

## Recomendaciones post-test
- Ajustar orden visual y agrupar controles por tareas.
- Añadir inline help y confirmaciones no intrusivas.
- Simplificar wording en botones (usar verbos claros).

---

Si quieres, puedo generar un script de prueba de usuario (sheet imprimible) y una plantilla de resultados en CSV para registrar tareas y tiempos durante las sesiones.
