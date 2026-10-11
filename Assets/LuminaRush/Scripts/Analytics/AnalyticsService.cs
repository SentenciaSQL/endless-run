using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace LuminaRush
{
    public sealed class AnalyticsService
    {
        const int MaxBytes = 512 * 1024;
        readonly string path;
        readonly bool console;

        public AnalyticsService(bool logToConsole)
        {
            console = logToConsole;
            string folder = Path.Combine(Application.persistentDataPath, "analytics");
            Directory.CreateDirectory(folder);
            path = Path.Combine(folder, "events.jsonl");
        }

        public void Track(string name)
        {
            Write(name, null);
        }

        public void Track(string name, params string[] pairs)
        {
            Write(name, pairs);
        }

        public void TrackAdValue(string placement, long valueMicros, string currency, string precision)
        {
            Track(
                "ad_paid",
                "placement", placement,
                "value_micros", valueMicros.ToString(CultureInfo.InvariantCulture),
                "currency", currency ?? "",
                "precision", precision ?? "",
                "editor_placeholder", Application.isEditor ? "1" : "0");
        }

        public void DeleteLocalLog()
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                string previous = path + ".prev";
                if (File.Exists(previous))
                    File.Delete(previous);
            }
            catch (Exception error)
            {
                Debug.LogWarning("No se pudo borrar el registro local de analítica. " + error.Message);
            }
        }

        void Write(string name, string[] pairs)
        {
            var builder = new StringBuilder(160);
            builder.Append("{\"t\":\"");
            builder.Append(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            builder.Append("\",\"name\":\"");
            builder.Append(Escape(name));
            builder.Append('"');
            if (pairs != null)
            {
                int count = pairs.Length - (pairs.Length % 2);
                for (int i = 0; i < count; i += 2)
                {
                    builder.Append(",\"");
                    builder.Append(Escape(pairs[i]));
                    builder.Append("\":\"");
                    builder.Append(Escape(pairs[i + 1]));
                    builder.Append('"');
                }
            }
            builder.Append('}');
            string line = builder.ToString();
            if (console)
                Debug.Log("[LuminaRush] " + line);
            FirebaseAnalyticsSink.Track(name, pairs);
            try
            {
                FileInfo info = new FileInfo(path);
                if (info.Exists && info.Length > MaxBytes)
                {
                    string previous = path + ".prev";
                    if (File.Exists(previous))
                        File.Delete(previous);
                    File.Move(path, previous);
                }
                File.AppendAllText(path, line + "\n");
            }
            catch (Exception error)
            {
                Debug.LogWarning("No se pudo escribir el evento local. " + error.Message);
            }
        }

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }
    }
}
