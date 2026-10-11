using UnityEngine;

namespace LuminaRush
{
    public enum MissionKind
    {
        Distance = 0,
        Coins = 1,
        Jumps = 2,
        Runs = 3,
        Slides = 4
    }

    public enum PowerKind
    {
        Shield = 0,
        Magnet = 1,
        Boost = 2
    }

    public struct RunnerShape
    {
        public string Name;
        public string Blurb;
        public int Price;
        public float BodyW;
        public float BodyH;
        public float Head;
        public float Leg;
        public int Style;
    }

    public struct SkinTint
    {
        public string Name;
        public int Price;
        public Color Body;
        public Color Accent;
        public Color Cloth;
        public Color Dark;
    }

    public struct BoostOffer
    {
        public string Name;
        public string Blurb;
        public int Price;
    }

    public struct AchievementDef
    {
        public string Name;
        public string Blurb;
        public int Reward;
    }

    public struct BiomeLook
    {
        public string Name;
        public Color Sky;
        public Color Fog;
        public Color Ground;
        public Color Lane;
        public Color PropA;
        public Color PropB;
        public Color FogShadow;
    }

    public static class Catalog
    {
        public static readonly RunnerShape[] Characters =
        {
            new RunnerShape
            {
                Name = "Kite",
                Blurb = "Mensajero de bufanda triangular. Equilibrado y ligero.",
                Price = 0,
                BodyW = 0.62f,
                BodyH = 0.78f,
                Head = 0.42f,
                Leg = 0.48f,
                Style = 0
            },
            new RunnerShape
            {
                Name = "Pebble",
                Blurb = "Corredor redondo de piedra pulida. Más compacto.",
                Price = 350,
                BodyW = 0.86f,
                BodyH = 0.66f,
                Head = 0.5f,
                Leg = 0.32f,
                Style = 1
            },
            new RunnerShape
            {
                Name = "Volt",
                Blurb = "Figura angular con crestas. Salto visual más marcado.",
                Price = 800,
                BodyW = 0.58f,
                BodyH = 0.84f,
                Head = 0.36f,
                Leg = 0.52f,
                Style = 2
            },
            new RunnerShape
            {
                Name = "Nimbus",
                Blurb = "Silueta ancha y suave, como una nube que corre.",
                Price = 1500,
                BodyW = 0.96f,
                BodyH = 0.7f,
                Head = 0.46f,
                Leg = 0.36f,
                Style = 3
            }
        };

        public static readonly SkinTint[] Skins =
        {
            new SkinTint { Name = "Alba", Price = 0, Body = Hex("F4E7D4"), Accent = Hex("FF5A36"), Cloth = Hex("F2C14E"), Dark = Hex("3C4A59") },
            new SkinTint { Name = "Coral", Price = 200, Body = Hex("FFD0C2"), Accent = Hex("E23E57"), Cloth = Hex("FF8A5B"), Dark = Hex("4A3040") },
            new SkinTint { Name = "Laguna", Price = 450, Body = Hex("D7F6F3"), Accent = Hex("148F8A"), Cloth = Hex("3EC6B8"), Dark = Hex("1E3A4C") },
            new SkinTint { Name = "Ember", Price = 700, Body = Hex("FFC857"), Accent = Hex("E4572E"), Cloth = Hex("F4A261"), Dark = Hex("3D2B1F") },
            new SkinTint { Name = "Glaciar", Price = 1100, Body = Hex("E7F1FF"), Accent = Hex("6C8AE4"), Cloth = Hex("B9D6FF"), Dark = Hex("2C3A55") }
        };

        public static readonly BoostOffer[] Boosts =
        {
            new BoostOffer { Name = "Escudo", Blurb = "Absorbe un golpe durante la carrera.", Price = 75 },
            new BoostOffer { Name = "Imán", Blurb = "Atrae monedas durante 8 segundos.", Price = 60 },
            new BoostOffer { Name = "Doble", Blurb = "Las monedas valen el doble durante 10 segundos.", Price = 90 }
        };

        public static readonly AchievementDef[] Achievements =
        {
            new AchievementDef { Name = "Primer tramo", Blurb = "Termina una carrera.", Reward = 20 },
            new AchievementDef { Name = "Ritmo", Blurb = "Recorre 400 m en una carrera.", Reward = 40 },
            new AchievementDef { Name = "Horizonte", Blurb = "Recorre 1500 m en una carrera.", Reward = 120 },
            new AchievementDef { Name = "Puñado", Blurb = "Reúne 40 monedas en una carrera.", Reward = 30 },
            new AchievementDef { Name = "Reserva", Blurb = "Acumula 500 monedas en total.", Reward = 50 },
            new AchievementDef { Name = "Constancia", Blurb = "Juega 10 carreras.", Reward = 40 },
            new AchievementDef { Name = "Segunda oportunidad", Blurb = "Usa una resurrección.", Reward = 25 },
            new AchievementDef { Name = "Primera compra", Blurb = "Compra algo en la tienda.", Reward = 20 }
        };

        public static readonly BiomeLook[] Biomes =
        {
            new BiomeLook
            {
                Name = "Mesa del Alba",
                Sky = Hex("7EC8F2"),
                Fog = Hex("F6D7B8"),
                Ground = Hex("E7B07A"),
                Lane = Hex("F3D2A6"),
                PropA = Hex("E07A5F"),
                PropB = Hex("81B29A"),
                FogShadow = Hex("C9845A")
            },
            new BiomeLook
            {
                Name = "Arboleda de Neón",
                Sky = Hex("2E3A73"),
                Fog = Hex("5CE1C5"),
                Ground = Hex("243044"),
                Lane = Hex("3D5368"),
                PropA = Hex("FF4D8D"),
                PropB = Hex("3DDC97"),
                FogShadow = Hex("1B2438")
            },
            new BiomeLook
            {
                Name = "Cañón de Ámbar",
                Sky = Hex("F2A65A"),
                Fog = Hex("E07A3D"),
                Ground = Hex("C4622D"),
                Lane = Hex("E08A45"),
                PropA = Hex("F2CC8F"),
                PropB = Hex("9B2226"),
                FogShadow = Hex("8C3A1E")
            },
            new BiomeLook
            {
                Name = "Cinta de Escarcha",
                Sky = Hex("D7E6F5"),
                Fog = Hex("B7D0E8"),
                Ground = Hex("E8F1F8"),
                Lane = Hex("C5D8EA"),
                PropA = Hex("8ECAE6"),
                PropB = Hex("6C8AE4"),
                FogShadow = Hex("9BB4C9")
            }
        };

        public static Color Hex(string html)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString("#" + html, out color))
                return color;
            return Color.white;
        }

        public static BiomeLook Blend(BiomeLook a, BiomeLook b, float t)
        {
            t = Mathf.Clamp01(t);
            return new BiomeLook
            {
                Name = t < 0.5f ? a.Name : b.Name,
                Sky = Color.Lerp(a.Sky, b.Sky, t),
                Fog = Color.Lerp(a.Fog, b.Fog, t),
                Ground = Color.Lerp(a.Ground, b.Ground, t),
                Lane = Color.Lerp(a.Lane, b.Lane, t),
                PropA = Color.Lerp(a.PropA, b.PropA, t),
                PropB = Color.Lerp(a.PropB, b.PropB, t),
                FogShadow = Color.Lerp(a.FogShadow, b.FogShadow, t)
            };
        }
    }
}
