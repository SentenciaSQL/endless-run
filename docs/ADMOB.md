# Google AdMob

El juego llama al SDK oficial **Google Mobile Ads Unity Plugin 11.5.0** (`com.google.ads.mobile`). No hay un anuncio simulado dentro de Lumina Rush. La recompensa solo se entrega en el callback `RewardedAd.Show(Action<Reward>)`, y un segundo aviso del mismo anuncio no vuelve a concederla.

En el editor, el propio plugin de Google muestra su marcador de anuncio recompensado. La recompensa de ese marcador también llega por el callback del SDK, cuando termina su cuenta atrás. En un teléfono Android la petición usa los identificadores configurados.

## 1. Dejar que Unity importe el plugin

`Packages/manifest.json` ya declara:

```json
"com.google.ads.mobile": "11.5.0"
```

y el registro OpenUPM con los ámbitos `com.google.ads.mobile` y `com.google.external-dependency-manager`.

Documentación de instalación de Google: https://developers.google.com/admob/unity/quick-start

Si Package Manager no lo resuelve, en la ventana del Package Manager usa **Add package by name** con el nombre `com.google.ads.mobile` y la versión `11.5.0`.

## 2. App ID y ad units son cosas distintas

- El **App ID** (`ca-app-pub-xxxxxxxxxxxxxxxx~yyyyyyyyyy`) va en **Assets > Google Mobile Ads > Settings**. El plugin lo escribe en el manifiesto de Android al compilar.
- El **ad unit ID** (`ca-app-pub-xxxxxxxxxxxxxxxx/zzzzzzzzzz`) lo lee el juego en ejecución desde `Assets/LuminaRush/Resources/AdUnitIds.json`.

Los dos tienen que pertenecer a la misma app de AdMob. Mezclar el App ID de prueba con unidades de producción, o al revés, hace que la carga falle.

## 3. Identificadores de prueba

Con `"useTestAds": true` el juego usa los identificadores de prueba publicados por Google:

| Uso | Android |
| --- | --- |
| App ID | `ca-app-pub-3940256099942544~3347511713` |
| Recompensado | `ca-app-pub-3940256099942544/5224354917` |
| Intersticial | `ca-app-pub-3940256099942544/1033173712` |

En iOS, si algún día compilas para ese sistema, el JSON también trae los identificadores de prueba de Google para recompensado e intersticial.

Después de importar el plugin, ejecuta **Lumina Rush > Aplicar App ID de prueba**.

Añade tu dispositivo de prueba en AdMob y pega su identificador con hash en `testDeviceHashedIds`, separado por comas. Guía de Google: https://developers.google.com/admob/unity/test-ads

No publiques una build de tienda con `useTestAds` en true. El juego escribe una advertencia en la consola si detecta identificadores de prueba fuera del editor y fuera de una development build.

## 4. Identificadores de producción

1. Entra en https://admob.google.com y crea una app Android.
2. Copia el App ID a `androidAppIdProduction`.
3. Crea **cuatro** bloques de anuncio:
   - recompensado para revivir;
   - recompensado para duplicar monedas;
   - recompensado para bonus de menú y, si quieres otro, para el extra de misiones;
   - intersticial para las transiciones de fin de carrera.
4. Pega cada ad unit en el campo `...Production` correspondiente de `AdUnitIds.json`.
5. Si `rewardedMissionProduction` queda vacío, el extra de misión reutiliza la unidad de bonus.
6. Pon `"useTestAds": false`.
7. Ejecuta **Lumina Rush > Aplicar App ID de producción**. El menú no aplica nada si los App ID de producción están vacíos.
8. Vuelve a compilar. El valor del paso 7 es el que entra en el manifiesto; el JSON es el que elige cada anuncio.

## 5. Dónde aparece cada anuncio

| Situación | Formato | Obligatorio |
| --- | --- | --- |
| Revivir, una vez por carrera | Recompensado | No. Si no hay anuncio, la carrera sigue terminada y se puede reintentar. |
| Duplicar las monedas de esa carrera | Recompensado | No. Las monedas base se guardan igual. |
| Bonus de monedas en el menú, máximo 5 al día | Recompensado | No. |
| Repetir la recompensa de una misión ya reclamada | Recompensado | No. La reclamación normal no pide anuncio. |
| Menú o reintento después de terminar una carrera | Intersticial | No. Si no está cargado, la transición continúa. |

El intersticial no se muestra durante la carrera, ni en la primera sesión de forma inmediata. Hacen falta al menos 3 carreras terminadas, 90 segundos desde el intersticial anterior y 45 segundos desde el último recompensado. Esos números están en el JSON.

Jugar, comprar con monedas del juego, las misiones base, los logros, el guardado y los ajustes funcionan sin anuncios.

## 6. Consentimiento y privacidad

Al arrancar, el juego llama a `ConsentInformation.Update` y a `ConsentForm.LoadAndShowConsentFormIfRequired` del User Messaging Platform incluido en el plugin. Los anuncios solo se inicializan si `ConsentInformation.CanRequestAds()` es verdadero.

En AdMob debes crear el mensaje de privacidad para el EEE, Reino Unido y Suiza: https://support.google.com/admob/answer/10113207

`umpDebugGeography` solo se aplica en el editor o en development builds. Déjalo en `Disabled` para las builds de tienda. Para probar el formulario, pon `EEA` y el hash del dispositivo de prueba. Valores válidos del SDK: `Disabled`, `EEA`, `RegulatedUSState`, `Other`.

Si el estado de privacidad exige un punto de entrada, Ajustes muestra **Opciones de privacidad**, que abre `ConsentForm.ShowPrivacyOptionsForm`.

`privacyPolicyUrl` es la URL de tu política. Si tiene texto, Ajustes muestra el botón que la abre. Hace falta una política pública antes de publicar en Play, sobre todo si usas AdMob y el identificador de publicidad.

El juego no está configurado como dirigido a niños. Si tu ficha de Play declara público infantil, tienes que cumplir la política de familias y no usar anuncios personalizados no permitidos. No actives tratamiento infantil en el SDK sin revisar esa política.

Si el consentimiento o la red fallan, el menú aparece igual. Los botones de anuncio indican que no están disponibles.

## 7. Carga, cierre y errores

Cada colocación recompensada y el intersticial se cargan al terminar la inicialización. Si la carga falla, se reintenta a los 8, 16, 24… segundos, con un tope de 60. No hay un bucle inmediato.

Al cerrarse o fallar a pantalla completa, el objeto del anuncio se destruye y se pide otro. Un anuncio de estos formatos solo se muestra una vez por carga.

La recompensa se marca como entregada en ese callback. Cerrar el anuncio sin recompensa no concede nada.

`OnAdPaid` registra el valor que envía el SDK (`value_micros`, moneda y precisión) en el registro local. Un micro-unidad es una millonésima de la moneda indicada. El juego no convierte eso en ingresos estimados ni asume un pago fijo por impresión. En el editor, el plugin puede informar un valor de marcador: el evento lleva `editor_placeholder` en 1.

## 8. Si la build dice «Resolution failed» o «Gradle failed to fetch dependencies»

Ese error lo escribe el External Dependency Manager cuando intenta descargar los AAR con su Gradle 5.1.1. Esa herramienta no resuelve `play-services-ads` 25.4.0. La build de Unity 2022.3 usa su propio Gradle, más nuevo, y sí puede.

Antes de compilar, el proyecto deja listas `mainTemplate.gradle`, `gradleTemplate.properties` y `settingsTemplate.gradle`, activa esas plantillas en Player Settings y mete las dependencias en `mainTemplate.gradle`. El resolver entonces parchea los archivos y no vuelve a lanzar la descarga antigua.

Si la ventana de resolución se quedó a medias:

1. Cierra Unity por completo, para cortar el Gradle que sigue en segundo plano.
2. Trae este cambio.
3. Abre el proyecto, espera a que importe y compila otra vez para Android.

No borres `mainTemplate.gradle` si ya contiene `**DEPS**` y `play-services-ads`. Si el archivo está vacío o es de otra versión de Unity, bórralo y deja que el editor lo copie de nuevo al compilar.

`Assets/GoogleMobileAds/link.xml` evita otro error de Windows del plugin: `Invalid AssetDatabase path ... link.xml`.

## 9. Permisos de Android

El plugin añade `INTERNET` y `com.google.android.gms.permission.AD_ID` mediante su procesador de Gradle. El reproductor también fuerza el permiso de internet. No hace falta un `AndroidManifest.xml` propio; si añades uno, no borres la meta-data `com.google.android.gms.ads.APPLICATION_ID`.
