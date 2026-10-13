#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LuminaRush.EditorTools
{
    public sealed class AndroidAdsGradleSetup : IPreprocessBuildWithReport
    {
        const string PluginsAndroid = "Assets/Plugins/Android";
        const string DepsToken = "**DEPS**";
        const string DependenciesStart = "// Android Resolver Dependencies Start";
        const string DependenciesEnd = "// Android Resolver Dependencies End";
        const string JetifierIgnore = "android.jetifier.ignorelist=annotation-experimental-1.4.0.aar";

        const string DependencyBlock =
            "    // Android Resolver Dependencies Start\n" +
            "    implementation 'androidx.constraintlayout:constraintlayout:2.1.4' // GoogleMobileAdsDependencies.xml:7\n" +
            "    implementation 'androidx.fragment:fragment:1.7.1' // GoogleMobileAdsDependencies.xml:16\n" +
            "    implementation 'androidx.lifecycle:lifecycle-process:2.6.2' // GoogleMobileAdsDependencies.xml:12\n" +
            "    implementation 'com.google.android.gms:play-services-ads:25.4.0' // GoogleMobileAdsDependencies.xml:3\n" +
            "    implementation 'com.google.android.ump:user-messaging-platform:4.0.0' // GoogleUmpDependencies.xml:3\n" +
            "    // Android Resolver Dependencies End\n";

        public int callbackOrder { get { return -100; } }

        [InitializeOnLoadMethod]
        static void SchedulePrepare()
        {
            EditorApplication.delayCall += delegate { Prepare(false); };
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;
            Prepare(true);
        }

        public static void Prepare(bool building)
        {
            Directory.CreateDirectory(PluginsAndroid);
            if (EditorUserBuildSettings.androidBuildSystem != AndroidBuildSystem.Gradle)
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EnableTemplateFlags();
            CopyTemplateIfMissing("mainTemplate.gradle", building);
            CopyTemplateIfMissing("gradleTemplate.properties", building);
            CopyTemplateIfMissing("settingsTemplate.gradle", building);
            EnsureDependencies();
            EnsureGradleProperties();
        }

        static void EnableTemplateFlags()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null)
                return;
            for (int i = 0; i < assets.Length; i++)
            {
                Object asset = assets[i];
                if (asset == null || asset.name != "PlayerSettings")
                    continue;
                SerializedObject serialized = new SerializedObject(asset);
                bool changed = false;
                changed |= Enable(serialized, "useCustomMainGradleTemplate");
                changed |= Enable(serialized, "useCustomGradlePropertiesTemplate");
                changed |= Enable(serialized, "useCustomGradleSettingsTemplate");
                if (changed)
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        static bool Enable(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Boolean || property.boolValue)
                return false;
            property.boolValue = true;
            return true;
        }

        static void CopyTemplateIfMissing(string fileName, bool building)
        {
            string destination = Path.Combine(PluginsAndroid, fileName);
            if (File.Exists(destination))
                return;
            string source = Path.Combine(
                EditorApplication.applicationContentsPath,
                "PlaybackEngines",
                "AndroidPlayer",
                "Tools",
                "GradleTemplates",
                fileName);
            if (!File.Exists(source))
            {
                string message = "Lumina Rush no encuentra la plantilla de Gradle de Unity en " + source + ". Instala el módulo Android Build Support en Unity Hub.";
                if (building)
                    Debug.LogError(message);
                else
                    Debug.LogWarning(message);
                return;
            }
            File.Copy(source, destination);
            Debug.Log("Lumina Rush copió " + fileName + " desde el editor de Unity.");
        }

        static void EnsureDependencies()
        {
            string path = Path.Combine(PluginsAndroid, "mainTemplate.gradle");
            if (!File.Exists(path))
                return;
            string text = File.ReadAllText(path);
            if (text.Contains("com.google.android.gms:play-services-ads:"))
                return;
            int token = text.IndexOf(DepsToken);
            if (token < 0)
            {
                Debug.LogError("mainTemplate.gradle no contiene **DEPS**. Borra Assets/Plugins/Android/mainTemplate.gradle y vuelve a compilar para que Unity copie la plantilla de 2022.3.");
                return;
            }
            string updated = text.Insert(token, DependencyBlock);
            if (!updated.Contains(DependenciesStart) || !updated.Contains(DependenciesEnd))
                return;
            File.WriteAllText(path, updated, new UTF8Encoding(false));
        }

        static void EnsureGradleProperties()
        {
            string path = Path.Combine(PluginsAndroid, "gradleTemplate.properties");
            if (!File.Exists(path))
                return;
            string text = File.ReadAllText(path);
            string addition = "";
            if (!text.Contains("android.useAndroidX="))
                addition += "android.useAndroidX=true\n";
            if (!text.Contains("android.enableJetifier="))
                addition += "android.enableJetifier=true\n";
            if (!text.Contains("android.jetifier.ignorelist="))
                addition += JetifierIgnore + "\n";
            if (addition.Length == 0)
                return;
            const string propertiesToken = "**ADDITIONAL_PROPERTIES**";
            int token = text.IndexOf(propertiesToken);
            if (token >= 0)
                text = text.Insert(token, addition);
            else
                text = text.TrimEnd() + "\n" + addition;
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }
    }
}
#endif
