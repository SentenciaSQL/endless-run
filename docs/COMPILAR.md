# Compilar y ejecutar

## Editor

1. Instala **Unity 2022.3 LTS** o **Unity 6** con estos módulos:
   - Android Build Support
   - OpenJDK
   - Android SDK & NDK Tools
2. En Unity Hub, **Add project from disk** y selecciona la carpeta del repositorio.
3. La primera apertura descarga `com.google.ads.mobile` 11.5.0 desde OpenUPM. El registro está en `Packages/manifest.json`.
4. Si el External Dependency Manager pregunta, activa la resolución de Android.
5. Abre `Assets/LuminaRush/Scenes/Main.unity`.
6. Comprueba que esa escena está en **File > Build Settings** y que es la escena 0. Ya está registrada en `ProjectSettings/EditorBuildSettings.asset`.
7. Pulsa Play.

El objeto de arranque no está colocado a mano en la escena. `GameRoot` se crea con `RuntimeInitializeOnLoadMethod` al cargar cualquier escena. La escena aporta la cámara, la luz y una referencia al material para que el shader entre en la build.

## Comprobar que compila

1. Espera a que la consola deje de importar.
2. Si falta el paquete de anuncios, los scripts de `Assets/LuminaRush/Scripts/Ads` no compilan: necesitan `GoogleMobileAds.Api` y `GoogleMobileAds.Ump.Api`. No sustituyas esas llamadas por un anuncio falso.
3. Cuando el paquete esté importado, la consola no debe mostrar errores de compilación.
4. Menú **Lumina Rush > Aplicar App ID de prueba**. Eso escribe el App ID de prueba de Google en `GoogleMobileAdsSettings`. Sin ese paso, la build de Android se detiene porque el App ID está vacío.

## Ajustes del reproductor que ya vienen definidos

| Ajuste | Valor |
| --- | --- |
| Producto | Lumina Rush |
| Compañía | Lumina Rush |
| Identificador Android | `com.luminarush.game` |
| Versión | 1.0.0 |
| Bundle version code | 1 |
| Orientación | Retrato |
| Mínimo Android | API 24 |
| Target SDK | Automático (el más alto instalado con el editor) |
| Scripting backend | IL2CPP en Android |
| Arquitecturas | ARMv7 y ARM64 |
| Internet | Obligatorio |
| Input | Input Manager clásico |
| Espacio de color | Gamma |

Si Unity reescribe `ProjectSettings/ProjectSettings.asset` al actualizar el proyecto, vuelve a revisar esta tabla en **Edit > Project Settings > Player**.

Sube el version code en cada archivo que subas a Play Console.

## Rendimiento

El juego apunta a móviles de gama media y baja:

- sombras desactivadas y sin antialiasing;
- un shader opaco con color de vértice;
- mallas compartidas y pools de segmentos, monedas, obstáculos y decorado;
- 60 fps como objetivo, sin VSync;
- la interfaz solo cambia el texto cuando el número cambia.

## Dónde queda el guardado

`Application.persistentDataPath/lumina_rush_profile.json`

Los eventos locales quedan en `Application.persistentDataPath/analytics/events.jsonl`. En Ajustes, **Borrar datos locales** pide una segunda pulsación y elimina ambos archivos.
