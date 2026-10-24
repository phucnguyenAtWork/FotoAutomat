using System.Text.Json;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

// Match Look parameter-predictor dataset tool (protocol: docs/ai-match-look/param-predictor-protocol-2026-10-16.md).
// Labels come from FotoAutomat's own recipe layout (RecipeVector), so they match the app exactly.
//   schema  --out <file>                                   parameter names and ranges
//   recipes --count N --seed S --out <file.jsonl> [--anchors <color-workspace.json>]
//   render  --content <content-set dir> --recipes <file.jsonl> --pairs N --seed S --out <dir>
//           C# reference renders for parity of the training renderer: N random (content, recipe) pairs, BGRA in/out.
//   v4      --content <content-set dir> --triplets <file.json> --out <dir>
//           Baseline for evaluation: current ReferenceMatchEngine (default options, no face regions) on each
//           (target A, reference = B rendered with the triplet's recipe) pair; writes the v4 render and its recipe vector.
var options = new Dictionary<string, string>();
for (int i = 1; i + 1 < args.Length; i += 2) options[args[i].TrimStart('-')] = args[i + 1];
string Need(string key) => options.TryGetValue(key, out var v) ? v : throw new ArgumentException($"Missing --{key}");
var json = new JsonSerializerOptions { WriteIndented = false };
switch (args.FirstOrDefault())
{
    case "schema":
        File.WriteAllText(Need("out"), JsonSerializer.Serialize(new { version = 1, parameters = RecipeVector.Parameters }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{RecipeVector.Length} parameters");
        break;
    case "recipes":
        int count = int.Parse(Need("count")); var random = new Random(int.Parse(Need("seed")));
        var anchors = options.TryGetValue("anchors", out var path) ? LoadAnchors(path) : [];
        using (var writer = new StreamWriter(Need("out")))
        {
            var sources = new Dictionary<string, int>();
            for (int n = 0; n < count; n++)
            {
                var (unit, source) = RecipeSampler.Sample(random, anchors);
                // Store the vector of the recipe the app would actually build (Decode keeps curves monotone).
                var recipe = RecipeVector.Decode(RecipeVector.FromUnit(unit)); var stored = RecipeVector.ToUnit(RecipeVector.Encode(recipe));
                string split = (n * 2654435761u % 100) switch { < 5 => "test", < 10 => "validation", _ => "train" };
                writer.WriteLine(JsonSerializer.Serialize(new { id = n, split, source, unit = stored.Select(v => Math.Round(v, 6)) }, json));
                string kind = source.Split(':')[0]; sources[kind] = sources.GetValueOrDefault(kind) + 1;
            }
            Console.WriteLine($"{count} recipes, {anchors.Count} anchors: " + string.Join(", ", sources.Select(p => $"{p.Key} {p.Value}")));
        }
        break;
    case "render":
        {
            var root = Need("content"); var outDir = Directory.CreateDirectory(Need("out")).FullName; var rng = new Random(int.Parse(Need("seed")));
            var items = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "content-manifest.json"))).RootElement.GetProperty("items").EnumerateArray()
                .Select(e => (Id: e.GetProperty("id").GetString()!, Size: e.GetProperty("size").GetInt32())).ToArray();
            var recipes = File.ReadLines(Need("recipes")).Select(l => JsonDocument.Parse(l).RootElement)
                .Select(e => (Id: e.GetProperty("id").GetInt32(), Unit: e.GetProperty("unit").EnumerateArray().Select(v => v.GetDouble()).ToArray())).ToArray();
            int pairs = int.Parse(Need("pairs")); var list = new List<object>();
            for (int k = 0; k < pairs; k++)
            {
                var item = items[rng.Next(items.Length)]; var recipe = recipes[rng.Next(recipes.Length)];
                var input = File.ReadAllBytes(Path.Combine(root, "content", item.Id + ".bgra"));
                var output = ColorProcessor.Apply(input, RecipeVector.Decode(RecipeVector.FromUnit(recipe.Unit)), item.Size, item.Size);
                File.WriteAllBytes(Path.Combine(outDir, $"{k}.out.bgra"), output);
                list.Add(new { k, content = item.Id, size = item.Size, recipe = recipe.Id, unit = recipe.Unit });
            }
            File.WriteAllText(Path.Combine(outDir, "pairs.json"), JsonSerializer.Serialize(list));
            Console.WriteLine($"{pairs} reference renders");
            break;
        }
    case "v4":
        {
            var root = Need("content"); var outDir = Directory.CreateDirectory(Need("out")).FullName;
            var triplets = JsonDocument.Parse(File.ReadAllText(Need("triplets"))).RootElement.EnumerateArray().ToArray(); var rows = new List<object>();
            byte[] Load(string id) => File.ReadAllBytes(Path.Combine(root, "content", id + ".bgra"));
            foreach (var t in triplets)
            {
                int k = t.GetProperty("k").GetInt32(), size = t.GetProperty("size").GetInt32();
                var unit = t.GetProperty("unit").EnumerateArray().Select(v => v.GetDouble()).ToArray();
                var target = Load(t.GetProperty("a").GetString()!); var look = RecipeVector.Decode(RecipeVector.FromUnit(unit));
                // "b" is one content id or a list (K references of the same look on different content).
                var b = t.GetProperty("b"); var ids = b.ValueKind == JsonValueKind.Array ? b.EnumerateArray().Select(e => e.GetString()!).ToArray() : [b.GetString()!];
                var references = ids.Select(id => new MatchPhoto(ColorProcessor.Apply(Load(id), look, size, size), size, size)).ToArray();
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var fit = ReferenceMatchEngine.FitReferences(new MatchPhoto(target, size, size), references);
                long ms = clock.ElapsedMilliseconds;
                File.WriteAllBytes(Path.Combine(outDir, $"{k}.v4.bgra"), ColorProcessor.Apply(target, fit.Recipe, size, size));
                rows.Add(new { k, ms, unit = RecipeVector.ToUnit(RecipeVector.Encode(fit.Recipe)).Select(v => Math.Round(v, 6)) });
                if (rows.Count % 50 == 0) Console.WriteLine($"{rows.Count}/{triplets.Length}");
            }
            File.WriteAllText(Path.Combine(outDir, "v4.json"), JsonSerializer.Serialize(rows));
            break;
        }
    default:
        Console.Error.WriteLine("Usage: schema --out <file> | recipes --count N --seed S --out <file> [--anchors <color-workspace.json>]"); Environment.ExitCode = 2; break;
}

// User presets that the vector can represent (no LUT, no Monochrome) anchor the look distribution.
static List<(string Name, double[] Unit)> LoadAnchors(string path) =>
    new ColorWorkspaceStore(path).Load().Looks.Where(r => r.Lut is null && !r.Monochrome).Select(r => (r.Name, RecipeVector.ToUnit(RecipeVector.Encode(r)))).Where(a => a.Item2.Any(v => Math.Abs(v) > 1e-6)).ToList();

static class RecipeSampler
{
    // Constants fixed in the protocol before any training: 5% identity, 35% jittered user anchors (strength 0.5–1.25,
    // jitter σ 0.12), the rest sparse random looks (each tool group active with the listed probability, σ 0.4, |u| ≤ 0.95).
    static readonly (string Group, int Start, int Count, double P)[] Groups =
        [("normalize", 0, 3, .6), ("light", 3, 5, .6), ("color", 8, 2, .5), ("curves", 10, 12, .4), ("hsl", 22, 24, .4), ("balance", 46, 6, .4)];
    public static (double[] Unit, string Source) Sample(Random r, IReadOnlyList<(string Name, double[] Unit)> anchors)
    {
        var unit = new double[RecipeVector.Length]; double roll = r.NextDouble();
        if (roll < .05) return (unit, "identity");
        if (roll < .40 && anchors.Count > 0)
        {
            var anchor = anchors[r.Next(anchors.Count)]; double strength = .5 + .75 * r.NextDouble();
            for (int i = 0; i < unit.Length; i++) unit[i] = Math.Clamp(anchor.Unit[i] * strength + (anchor.Unit[i] != 0 ? Gauss(r) * .12 : 0), -.95, .95);
            return (unit, "anchor:" + anchor.Name);
        }
        foreach (var (_, start, n, p) in Groups)
        {
            if (r.NextDouble() >= p) continue;
            for (int i = start; i < start + n; i++)
                if (start != 22 || r.NextDouble() < .35) unit[i] = Math.Clamp(Gauss(r) * .4, -.95, .95); // HSL: only some bands
        }
        return (unit, "random");
    }
    static double Gauss(Random r) => Math.Sqrt(-2 * Math.Log(1 - r.NextDouble())) * Math.Cos(2 * Math.PI * r.NextDouble());
}
