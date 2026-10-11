# Lumina Rush

Endless runner 3D para Android. El corredor avanza solo por tres carriles, salta, se agacha y recoge monedas mientras el escenario, los obstáculos y la velocidad cambian. El progreso se guarda en el dispositivo. Los anuncios son opcionales y usan el SDK oficial de Google Mobile Ads para Unity 11.5.0.

No hace falta importar modelos ni audio para jugar: las mallas, los colores y el sonido se generan en el proyecto. Puedes sustituirlos después.

## Requisitos

- Unity 2022.3 LTS o Unity 6, con Android Build Support, Android SDK, NDK y OpenJDK.
- Conexión a internet la primera vez que se abre el proyecto, para que Package Manager descargue `com.google.ads.mobile` 11.5.0 y External Dependency Manager.
- Cuenta de Google AdMob solo cuando vayas a publicar. Durante el desarrollo se usan los identificadores de prueba oficiales de Google.

## Abrir y jugar

1. Instala el editor desde Unity Hub y añade el módulo Android.
2. Abre la carpeta de este repositorio como proyecto.
3. Si Unity pide actualizar el proyecto, acepta. El editor rellenará los ajustes que falten.
4. Espera a que termine la importación de paquetes. Si aparece External Dependency Manager, pulsa **Enable** y deja que resuelva dependencias de Android.
5. Abre `Assets/LuminaRush/Scenes/Main.unity`.
6. Pulsa Play. El juego arranca solo: crea al personaje, el circuito, la interfaz y el audio.

En el editor, el teclado también sirve: A/D o flechas para cambiar de carril, W o espacio para saltar, S o flecha abajo para agacharse. En el teléfono se usan deslizamientos.

Los anuncios dentro del editor son el marcador oficial del plugin de Google, no una recompensa inventada por el juego. En un dispositivo Android se solicitan anuncios de prueba reales.

## Documentación

- [Compilar y ejecutar](docs/COMPILAR.md)
- [Google AdMob, prueba y producción](docs/ADMOB.md)
- [APK y Android App Bundle](docs/ANDROID.md)
- [Google Play](docs/PLAY.md)
- [Gráficos y audio](docs/RECURSOS.md)
- [Arquitectura](docs/ARQUITECTURA.md)

## Qué incluye el juego

- Tres carriles, salto, agacharse y cambio de carril.
- Segmentos, obstáculos, monedas y decorado reutilizados con pools.
- Cuatro biomas: Mesa del Alba, Arboleda de Neón, Cañón de Ámbar y Cinta de Escarcha.
- Puntuación, distancia y récords.
- Monedas, cuatro corredores, cinco aspectos y tres potenciadores.
- Tienda, misiones diarias y logros.
- Pantallas de inicio, pausa, derrota, tienda, misiones y ajustes.
- Guardado local en `lumina_rush_profile.json`.
- Anuncio recompensado para revivir una vez por carrera, duplicar las monedas de esa carrera, un bonus diario opcional y un extra opcional en misiones ya reclamadas.
- Intersticial solo al salir de una carrera hacia el menú o al reintentar, con un mínimo de 3 carreras terminadas, 90 segundos desde el anterior y 45 segundos después de un recompensado.
- Consentimiento con el UMP de Google antes de inicializar anuncios.
- Registro local de eventos y un punto de conexión opcional con Firebase Analytics.
