#if FIREBASE_ANALYTICS
using System.Globalization;
using Firebase.Analytics;
#endif

namespace LuminaRush
{
    public static class FirebaseAnalyticsSink
    {
        public static void TryInit()
        {
#if FIREBASE_ANALYTICS
            if (checking || ready)
                return;
            checking = true;
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                ready = task.Result == Firebase.DependencyStatus.Available;
                checking = false;
            });
#endif
        }

        public static void Track(string name, string[] pairs)
        {
#if FIREBASE_ANALYTICS
            if (!ready || string.IsNullOrEmpty(name))
                return;
            if (pairs == null || pairs.Length < 2)
            {
                FirebaseAnalytics.LogEvent(name);
                return;
            }

            int count = pairs.Length / 2;
            var parameters = new Parameter[count];
            for (int i = 0; i < count; i++)
            {
                string key = pairs[i * 2] ?? "value";
                string text = pairs[i * 2 + 1] ?? "";
                double number;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                    parameters[i] = new Parameter(key, number);
                else
                    parameters[i] = new Parameter(key, text);
            }
            FirebaseAnalytics.LogEvent(name, parameters);
#endif
        }

#if FIREBASE_ANALYTICS
        static bool ready;
        static bool checking;
#endif
    }
}
