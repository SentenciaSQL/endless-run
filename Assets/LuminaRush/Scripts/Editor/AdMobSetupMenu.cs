#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LuminaRush.EditorTools
{
    public static class AdMobSetupMenu
    {
        const string SettingsHint = "El paquete com.google.ads.mobile todavía no está importado. Espera a que Package Manager termine, abre Assets > Google Mobile Ads > Settings una vez y vuelve a ejecutar este menú.";

        [MenuItem("Lumina Rush/Aplicar App ID de prueba")]
        public static void ApplyTestIds()
        {
            Apply(true);
        }

        [MenuItem("Lumina Rush/Aplicar App ID de producción")]
        public static void ApplyProductionIds()
        {
            Apply(false);
        }

        static void Apply(bool testIds)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/LuminaRush/Resources/AdUnitIds.json");
            if (asset == null)
            {
                EditorUtility.DisplayDialog("Lumina Rush", "No se encontró Assets/LuminaRush/Resources/AdUnitIds.json.", "Entendido");
                return;
            }

            AdConfigFile file = JsonUtility.FromJson<AdConfigFile>(asset.text);
            if (file == null)
            {
                EditorUtility.DisplayDialog("Lumina Rush", "AdUnitIds.json no se pudo leer.", "Entendido");
                return;
            }

            if (!testIds && (string.IsNullOrEmpty(file.androidAppIdProduction) || string.IsNullOrEmpty(file.iosAppIdProduction)))
            {
                EditorUtility.DisplayDialog("Lumina Rush", "Escribe androidAppIdProduction e iosAppIdProduction en AdUnitIds.json antes de aplicar los identificadores de producción.", "Entendido");
                return;
            }

            string android = testIds || string.IsNullOrEmpty(file.androidAppIdProduction) ? file.androidAppIdTest : file.androidAppIdProduction;
            string ios = testIds || string.IsNullOrEmpty(file.iosAppIdProduction) ? file.iosAppIdTest : file.iosAppIdProduction;
            ScriptableObject settings = LoadGoogleSettings();
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Lumina Rush", SettingsHint, "Entendido");
                return;
            }

            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty androidProperty = serialized.FindProperty("adMobAndroidAppId");
            SerializedProperty iosProperty = serialized.FindProperty("adMobIOSAppId");
            if (androidProperty == null || iosProperty == null)
            {
                EditorUtility.DisplayDialog("Lumina Rush", "La versión instalada del plugin no expone adMobAndroidAppId. Ábrelo en Assets > Google Mobile Ads > Settings y pega los App ID a mano.", "Entendido");
                return;
            }

            androidProperty.stringValue = android;
            iosProperty.stringValue = ios;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Lumina Rush", "App ID escritos en GoogleMobileAdsSettings.\n\nAndroid: " + android + "\niOS: " + ios + "\n\nLos ad unit de cada anuncio se leen en ejecución desde AdUnitIds.json.", "Entendido");
        }

        static ScriptableObject LoadGoogleSettings()
        {
            Type settingsType = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            if (settingsType == null)
                return null;
            MethodInfo load = settingsType.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (load == null)
                return null;
            return load.Invoke(null, null) as ScriptableObject;
        }
    }
}
#endif
