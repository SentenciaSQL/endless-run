# Gráficos y audio

Lumina Rush no copia personajes, escenarios ni nombres de otros juegos. El corredor, el suelo, las monedas, los obstáculos, los cristales, los árboles y las nubes son mallas generadas en `MeshLibrary`. El shader `LuminaRush/VertexLit` ilumina el color de vértice con una luz fija. El material está en `Assets/LuminaRush/Resources/VertexLit.mat` y también lo referencia la escena para que no se elimine al compilar.

El audio de salto, moneda, golpe, interfaz y la música en bucle se crean con `AudioClip.Create` en `AudioDirector`. No hay archivos wav que importar para la primera versión.

## Sustituir el aspecto

1. Crea los materiales o texturas en Unity.
2. Si quieres otro shader, asígnalo al material `VertexLit` o cambia `MeshLibrary.SharedMaterial` para devolver el tuyo. Tiene que seguir exponiendo el color `_Tint`, porque los biomas lo cambian con un `MaterialPropertyBlock`.
3. Para cambiar solo colores, edita las paletas de `Catalog.Skins` y los biomas de `Catalog.Biomes`.
4. Los prefabs de `Assets/LuminaRush/Resources/Prefabs` (`Runner`, `Coin`, `Hazard`, `PowerOrb`, `Prop`) ya traen collider, rigidbody cinemático y el script correspondiente. Al instanciarlos, el juego asigna la malla si el `MeshFilter` está vacío. Puedes guardar una malla propia en el prefab y no se sustituirá.

## Sustituir el sonido

`AudioDirector.Build` crea los clips. Para usar archivos importados:

1. Importa wav u ogg en `Assets/LuminaRush/Audio`.
2. En `AudioDirector.Build`, asigna esos `AudioClip` a los campos `coin`, `jump`, `slide`, `hit`, `ui`, `unlock`, `start` y `music.clip` en lugar de llamar a los generadores.
3. Deja el clip de música en loop.

Los volúmenes de música y efectos salen de los ajustes y se guardan en el perfil.

## Icono de la aplicación

Unity usa el icono por defecto hasta que asignes texturas en **Player Settings > Icon > Android**. Hacen falta los tamaños que pide esa pantalla, y para la ficha de Play un PNG de 512×512 aparte. No está incluido porque el proyecto no depende de arte externo para ejecutarse.
