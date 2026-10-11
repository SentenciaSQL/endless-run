# Publicación en Google Play

## Antes de abrir Play Console

1. Cuenta de desarrollador en https://play.google.com/console (tiene una tarifa de registro).
2. Política de privacidad en una URL pública. Explique que la app usa Google AdMob, el identificador de publicidad y el consentimiento del User Messaging Platform, y que el progreso se guarda en el dispositivo. Pega esa URL en `privacyPolicyUrl` de `Assets/LuminaRush/Resources/AdUnitIds.json`.
3. App de AdMob creada, App ID de producción aplicado con **Lumina Rush > Aplicar App ID de producción**, y `useTestAds` en false.
4. AAB firmado con tu keystore, como se describe en [ANDROID.md](ANDROID.md).
5. Icono de aplicación de 512×512 y gráfico destacado de 1024×500. Este proyecto no trae icono de tienda: el juego dibuja su personaje en código. Exporta una captura o diseña el icono e impórtalo en **Player Settings > Icon**.
6. Capturas de la app en teléfono, en vertical. Hazlas en un dispositivo o en el simulador de Game View a 1080×1920.

## Ficha de la tienda

1. **Crear app**. Nombre visible: Lumina Rush. Tipo: juego. Gratuita.
2. Idioma predeterminado: español, si esa es la ficha que vas a publicar. La interfaz del juego ya está en español.
3. Completa la ficha: descripción corta, descripción completa, icono, gráfico y capturas.
4. Categoría: Carreras o Casual.
5. Correo de contacto.

No uses nombres, personajes ni capturas de otros runners.

## Política y anuncios

En el cuestionario de contenido y en la sección de anuncios:

- Declara que la app contiene anuncios.
- Completa el formulario de seguridad de datos. Los datos que este proyecto trata son:
  - progreso de juego guardado solo en el dispositivo;
  - identificadores publicitarios y datos de consentimiento que recoge el SDK de Google cuando el jugador puede solicitar anuncios;
  - el registro de analítica local, que no sale del teléfono salvo que actives Firebase como se explica en [ARQUITECTURA.md](ARQUITECTURA.md).
- No declares ingresos inventados. AdMob informa las impresiones en su propio panel, no dentro del juego.
- Si el público incluye niños, revisa la política de familias antes de activar anuncios. Este proyecto no marca la app como dirigida a niños.

Enlaza la app de Play con AdMob cuando ambas existan, desde el panel de AdMob, para que las unidades de producción correspondan a la ficha publicada.

## Subir el paquete

1. Prueba interna o cerrada.
2. Crea una versión y sube el `.aab`.
3. Escribe las notas de la versión.
4. Revisa los avisos de target API, permisos y firma.
5. Publica primero en una pista de prueba. Instala desde Play, no solo el APK sideload, y comprueba consentimiento, recompensa y que el juego sigue usable sin red.

El identificador de paquete `com.luminarush.game` no se puede cambiar después de la primera publicación. Si ese nombre ya está ocupado en tu cuenta o prefieres otro, cámbialo en Player Settings **antes** de subir el primer AAB y usa el mismo en AdMob.

## Actualizaciones

Sube el **Bundle Version Code**. No reutilices un code ya publicado. Conserva el keystore de subida.
