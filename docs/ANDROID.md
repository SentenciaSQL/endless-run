# APK y Android App Bundle

Haz antes los pasos de [COMPILAR.md](COMPILAR.md) y, si la build es de desarrollo, [ADMOB.md](ADMOB.md) con los identificadores de prueba. Para una build que vayas a subir a Play, usa los de producción y `useTestAds` en false.

## Preparar la plataforma

1. **File > Build Settings**.
2. Elige **Android** y pulsa **Switch Platform**. La primera vez tarda porque reimporta assets.
3. Deja marcada `Assets/LuminaRush/Scenes/Main.unity`.
4. **Player Settings** y revisa la tabla de COMPILAR.md.

En un dispositivo propio, activa la depuración USB y acepta la instalación de orígenes desconocidos solo para probar el APK. Play no usa el APK suelto como formato principal.

## Keystore

La build de depuración puede usar el keystore de debug de Unity. La build que subes a Play necesita un keystore propio. Guárdalo fuera del repositorio y no lo pierdas: sin él no podrás actualizar la app.

1. **Player Settings > Publishing Settings**.
2. **Keystore Manager > Create New**.
3. Anota la contraseña, el alias y la contraseña del alias en un lugar seguro, fuera de git.
4. Activa **Custom Keystore**, elige el archivo y escribe alias y contraseñas.

Incrementa **Bundle Version Code** en cada subida. La versión visible para el jugador es **Version** (`1.0.0` en la primera build).

## APK

1. **File > Build Settings**.
2. Desmarca **Build App Bundle (Google Play)** si solo quieres un APK de prueba.
3. **Build**.
4. Guarda el archivo, por ejemplo `LuminaRush.apk`.
5. Instálalo con `adb install -r LuminaRush.apk` o copiándolo al teléfono.

El APK de prueba debe llevar identificadores de anuncio de prueba. Instalar una build de producción en un dispositivo de desarrollo sin registrarlo como dispositivo de prueba puede generar tráfico inválido en AdMob.

## Android App Bundle

Google Play exige AAB.

1. **File > Build Settings**.
2. Marca **Build App Bundle (Google Play)**.
3. **Build** y guarda `LuminaRush.aab`.
4. En **Player Settings > Publishing Settings**, deja **Split Application Binary** desmarcado salvo que necesites expansión. Este juego no usa archivos OBB.
5. Sube el `.aab` a Play Console, no el APK.

Unity firma el AAB con el keystore seleccionado. Play App Signing puede volver a firmarlo. La primera subida registra la clave.

## Target SDK

`AndroidTargetSdkVersion` está en automático para usar el SDK más alto instalado con el editor. Play Console indica el nivel mínimo aceptado en el momento de la subida. Si rechaza el paquete, instala ese platform SDK en Unity Hub y fija **Target API Level** a ese valor.

## IL2CPP y arquitecturas

La build de Android usa IL2CPP, ARMv7 y ARM64. La primera compilación IL2CPP es larga. Si falta el NDK, Unity Hub lo instala desde el módulo Android.

## Probar en el dispositivo

1. Instala el APK de prueba.
2. Acepta el formulario de consentimiento si aparece.
3. Juega una carrera, muere y comprueba que **Revivir** solo continúa si el anuncio de prueba termina y el SDK confirma la recompensa.
4. Cierra el anuncio antes de la recompensa y comprueba que la carrera sigue terminada.
5. Duplicar monedas debe sumar otra vez las monedas de esa carrera, una sola vez.
6. Jugar, la tienda y el reintento deben funcionar con el teléfono en modo avión: los botones de anuncio avisarán que no hay inventario, sin bloquear el juego.
