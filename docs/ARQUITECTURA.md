# Arquitectura

El código está en `Assets/LuminaRush/Scripts`, espacio de nombres `LuminaRush`.

| Carpeta | Responsabilidad |
| --- | --- |
| `Core/GameRoot.cs` | Arranque, fases, carrera, banca de monedas y enlace entre sistemas |
| `Player` | Deslizamiento, salto, agacharse, carriles y animación del personaje |
| `World` | Segmentos infinitos, patrones, monedas, obstáculos, orbes y pools |
| `Visual` | Mallas, cámara y partículas |
| `Audio` | Música y efectos, con volumen |
| `Progress` | Perfil, tienda, misiones diarias y logros |
| `Ads` | Configuración, consentimiento UMP, recompensados e intersticial |
| `Analytics` | Registro local JSONL y, si se activa, Firebase |
| `UI` | Pantallas de juego construidas en código |
| `Editor` | Menú para copiar el App ID al ajuste del plugin |

## Fases

`Boot`, `Menu`, `Playing`, `Paused`, `GameOver`.

La simulación usa `Time.deltaTime` y se detiene en pausa y mientras `AdsService` marca un anuncio a pantalla completa. El SDK también puede poner `timeScale` a 0 durante su propio anuncio. La pausa del jugador no usa `timeScale`, para no pisar ese valor cuando el anuncio se cierra.

## Carrera

El jugador se mueve en +Z. Los carriles están en X = -2.4, 0 y 2.4. Un segmento mide 40 m. Hay seis segmentos vivos; cuando uno queda atrás se vacía en los pools y se vuelve a llenar por delante.

Los patrones son filas de tres caracteres. `c` es moneda, `b` una barrera baja, `h` una barra alta, `x` un bloqueo de carril, y `s`, `m`, `p` los orbes de escudo, imán y doble moneda. Si una fila bloqueara los tres carriles, el centro se abre.

La velocidad pasa de 11 a 23 m/s a lo largo de 1700 m. El bioma cambia cada 450 m.

Las monedas de la carrera no entran en la cartera hasta que la carrera termina de verdad. Duplicar añade esa misma cantidad una segunda vez, solo si el callback de recompensa se ejecutó y todavía no se había duplicado. Revivir no cierra la carrera y limpia los obstáculos inmediatos.

## Guardado

`ProfileStore` escribe JSON con `JsonUtility` en un archivo temporal y luego lo mueve. Se guarda al comprar, al terminar una carrera, al cambiar ajustes, al pausar la app y al salir.

## Anuncios

`AdConfig` lee `Resources/AdUnitIds.json`. `AdsService` pide consentimiento, inicializa `MobileAds` solo si se pueden solicitar anuncios, y mantiene un `RewardedAd` por colocación más un `InterstitialAd`.

## Analítica

`AnalyticsService` añade una línea JSON por evento en el dispositivo. No calcula dinero. `ad_paid` guarda los micros, la moneda y la precisión que entrega el SDK.

Eventos que se registran: `session_start`, `retention_active`, `run_start`, `run_end`, `coins_banked`, `rewarded_load_requested`, `rewarded_loaded`, `rewarded_load_failed`, `rewarded_show`, `rewarded_completed`, `rewarded_failed`, `rewarded_unavailable`, `reward_granted`, `interstitial_load_requested`, `interstitial_loaded`, `interstitial_load_failed`, `interstitial_show`, `interstitial_failed`, `ad_paid`, `shop_open`, `shop_purchase`, `mission_claimed`, `achievement_claimed`, `powerup_used`, `powerup_pickup`, `consent_update_requested`, `consent_result`, `mobile_ads_initialized`.

`run_end` incluye distancia, duración, monedas, puntuación y si hubo resurrección.

## Firebase Analytics, opcional

No está incluido, porque exige un proyecto de Firebase y el archivo `google-services.json`. El registro local funciona sin eso.

Para activarlo:

1. Crea un proyecto en https://console.firebase.google.com y añade una app Android con el paquete `com.luminarush.game`.
2. Descarga `google-services.json` y colócalo en `Assets/`.
3. Importa el SDK de Firebase Analytics para Unity siguiendo https://firebase.google.com/docs/unity/setup.
4. En **Player Settings > Scripting Define Symbols**, para Android, añade `FIREBASE_ANALYTICS`.
5. `FirebaseAnalyticsSink` llama a `FirebaseAnalytics.LogEvent` solo con ese símbolo. `TryInit` se ejecuta al entrar al menú únicamente si el UMP permitió solicitar anuncios. Si el jugador cambia el consentimiento más tarde, la recogida de Firebase queda para el siguiente arranque.

No inventes un valor de ingreso en Firebase. Si más adelante registras `ad_impression`, usa el `AdValue` real del callback `OnAdPaid`.

## Dependencias

- Unity 2022.3 LTS o Unity 6.
- Módulos integrados declarados en `Packages/manifest.json`: UI, física, audio, partículas, Android JNI y UnityWebRequest.
- `com.google.ads.mobile` 11.5.0, que trae el SDK de anuncios, el UMP y External Dependency Manager.
- Firebase Analytics, solo si añades el símbolo anterior.
- No hay assets de pago ni de la Asset Store.

## Prefabs y escena

- Escena: `Assets/LuminaRush/Scenes/Main.unity`
- Prefabs usados en ejecución: `Assets/LuminaRush/Resources/Prefabs/`
- Material: `Assets/LuminaRush/Resources/VertexLit.mat`
- Shader: `Assets/LuminaRush/Shaders/VertexLit.shader`
- Anuncios: `Assets/LuminaRush/Resources/AdUnitIds.json`
