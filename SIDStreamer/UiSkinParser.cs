using System.Text.Json.Nodes;

namespace SIDStream
{



    // Config parser for the UI skin JSON file - Auto Generated Class (Copilot)
    public class UiSkinParser
    {
        private readonly JsonNode _root;

        public UiSkinParser(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            _root = JsonNode.Parse(json) ?? throw new Exception("Invalid JSON");
        }

        // ---------------------------
        // Generic helpers
        // ---------------------------
        private JsonObject? GetObject(string name)
            => _root[name] as JsonObject;

        private JsonObject? GetObject(params string[] path)
        {
            JsonNode? current = _root;
            foreach (var p in path)
            {
                if (current is JsonObject obj && obj.TryGetPropertyValue(p, out var next))
                    current = next;
                else
                    return null;
            }
            return current as JsonObject;
        }

        // ---------------------------
        // Top-level fields
        // ---------------------------
        public string BgSettingsImage =>
            _root["bgSettingsImage"]?.ToString() ?? "";

        // ---------------------------
        // ITERATION SUPPORT
        // ---------------------------

        public IEnumerable<(string name, JsonObject obj)> GetButtons()
        {
            var buttons = GetObject("buttons");
            if (buttons == null) yield break;

            foreach (var kv in buttons)
                if (kv.Value is JsonObject o)
                    yield return (kv.Key, o);
        }

        public IEnumerable<(string name, JsonObject obj)> GetLabels()
        {
            var labels = GetObject("labels");
            if (labels == null) yield break;

            foreach (var kv in labels)
                if (kv.Value is JsonObject o)
                    yield return (kv.Key, o);
        }

        public IEnumerable<(string name, JsonObject obj)> GetComboBoxes()
        {
            var combo = GetObject("comboBoxes");
            if (combo == null) yield break;

            foreach (var kv in combo)
                if (kv.Value is JsonObject o)
                    yield return (kv.Key, o);
        }

        public IEnumerable<(string name, JsonObject obj)> GetImages()
        {
            var images = GetObject("images");
            if (images == null) yield break;

            foreach (var kv in images)
                if (kv.Value is JsonObject o)
                    yield return (kv.Key, o);
        }

        // ---------------------------
        // Strongly-typed helpers (optional)
        // ---------------------------
        public static int Int(JsonObject obj, string key) =>
            obj[key]?.GetValue<int>() ?? 0;

        public static string Str(JsonObject obj, string key) =>
            obj[key]?.ToString() ?? "";
    }

}
