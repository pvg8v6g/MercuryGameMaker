using GameLibrary.Models.Modules;
using Windows.Graphics.Imaging;
using Windows.Storage;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Props;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Location;

namespace GameMaker.Services.Maps;

public class PropFillService(IGameDataService gameDataService, ILocationService locationService) : IPropFillService
{
    #region Constants

    // how close to an unpassable area counts as "near" it
    private const int NearDistance = 48;

    // the room an existing prop keeps around itself
    private const int ExistingSpacing = 24;

    #endregion

    #region Methods

    public async Task<List<MapProp>> Fill(GameMap map, FillSet fillSet, int seed, IEnumerable<MapProp> keepClearOf)
    {
        var tile = gameDataService.GridSize;
        var step = gameDataService.StepSize;
        var grid = new MapGrid(map.Width * tile, map.Height * tile, step);
        var random = new Random(seed);
        var ground = await GroundPixels(map, grid);

        // unpassable is the painted collision plus everything walled off by it: ground the player can't reach
        var collision = grid.Mask(map.Areas.Where(x => x.AreaType == MapAreaType.Collision));
        var blocked = grid.Not(grid.Reachable(grid.Not(collision), PlayerSpots(map, tile)));
        var near = grid.Near(blocked, NearDistance);

        // everything placed so far, x, y and the spacing it keeps
        var placed = keepClearOf.Select(x => (x: (double) x.X, y: (double) x.Y, spacing: (double) ExistingSpacing)).ToList();
        var result = new List<MapProp>();

        foreach (var rule in fillSet.Rules)
        {
            var pool = rule.Props.Where(guid => gameDataService.Props.Any(x => x.Guid == guid)).ToArray();
            if (pool.Length == 0 || rule.Density <= 0) continue;

            var allowed = rule.Place switch
            {
                FillPlace.Unpassable => blocked,
                FillPlace.Open => grid.Not(blocked),
                FillPlace.NearUnpassable => near,
                FillPlace.InsideArea => grid.Mask(map.Areas.Where(x => x.AreaType == MapAreaType.Area && string.Equals(x.Name, rule.AreaName, StringComparison.OrdinalIgnoreCase))),
                _ => grid.Not(blocked)
            };
            if (ParseColor(rule.GroundColor) is { } color && ground is not null) allowed = grid.And(allowed, grid.ColorNear(ground, color, rule.ColorTolerance));

            var candidates = grid.Cells(allowed);
            if (candidates.Count == 0) continue;

            // as many as fit at this spacing, times the density
            var spacing = Math.Max(step, rule.Spacing);
            var clump = Math.Max(1, rule.ClumpSize);
            var area = candidates.Count * (double) step * step;
            var target = (int) Math.Round(area / (spacing * spacing) * rule.Density / 100.0);
            Shuffle(candidates, random);

            var count = 0;
            foreach (var (cx, cy) in candidates)
            {
                if (count >= target) break;

                // a clump is a few props around one spot, single props are a clump of one
                for (var i = 0; i < clump && count < target; i++)
                {
                    var x = cx + (i == 0 ? 0 : (random.NextDouble() * 2 - 1) * spacing * 1.5);
                    var y = cy + (i == 0 ? 0 : (random.NextDouble() * 2 - 1) * spacing);
                    (x, y) = grid.Snap(x, y);
                    if (!grid.Allows(allowed, x, y) || !IsClear(placed, x, y, spacing)) continue;

                    placed.Add((x, y, spacing));
                    result.Add(new MapProp { PropGuid = pool[random.Next(pool.Length)], X = (int) x, Y = (int) y });
                    count++;
                }
            }
        }

        return result;
    }

    #endregion

    #region Private Methods

    // where the player can be on this map: the test spawn, the game start and every entity (feet on the tile)
    private List<(double x, double y)> PlayerSpots(GameMap map, int tile)
    {
        var spots = new List<(double x, double y)>();
        if (map.TestSpawn is { } spawn) spots.Add(((spawn.TileX + 0.5) * tile, (spawn.TileY + 0.5) * tile));

        var start = gameDataService.GameStart;
        if (start.MapGuid == map.Guid) spots.Add(((start.Spawn.TileX + 0.5) * tile, (start.Spawn.TileY + 0.5) * tile));

        foreach (var position in map.Entities.SelectMany(x => x.Pages).SelectMany(x => x.Modules).OfType<PositionModule>())
        {
            spots.Add((position.X + tile / 2.0, position.Y + tile / 2.0));
        }

        return spots;
    }

    // two props stand at least the average of their spacings apart
    private static bool IsClear(List<(double x, double y, double spacing)> placed, double x, double y, double spacing)
    {
        foreach (var other in placed)
        {
            var need = (spacing + other.spacing) / 2.0;
            var dx = other.x - x;
            var dy = other.y - y;
            if (dx * dx + dy * dy < need * need) return false;
        }

        return true;
    }

    private static void Shuffle<T>(List<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static (byte r, byte g, byte b)? ParseColor(string text)
    {
        var hex = text.Trim().TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value)) return null;
        return ((byte) (value >> 16), (byte) (value >> 8), (byte) value);
    }

    // the map's ground image as one colour per grid cell (averaged), null when the map has no ground image
    private async Task<(byte r, byte g, byte b)[,]?> GroundPixels(GameMap map, MapGrid grid)
    {
        var path = new MapLayer(MapLayerType.Ground).ImagePath(locationService.GraphicsDirectory!, map.Id);
        if (!File.Exists(path)) return null;

        var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(path));
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, new BitmapTransform(),
            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
        return grid.Average(data.DetachPixelData(), (int) decoder.PixelWidth, (int) decoder.PixelHeight);
    }

    #endregion

    #region Types

    // the map cut into cells of one character step, a mask says which cells a rule may use
    private sealed class MapGrid(int width, int height, int step)
    {
        private readonly int _columns = Math.Max(1, width / step);
        private readonly int _rows = Math.Max(1, height / step);

        public bool[,] Mask(IEnumerable<Area> areas)
        {
            var mask = new bool[_columns, _rows];
            foreach (var area in areas)
            {
                var left = Math.Max(0, area.OffsetX / step);
                var top = Math.Max(0, area.OffsetY / step);
                var right = Math.Min(_columns, (area.OffsetX + area.Width + step - 1) / step);
                var bottom = Math.Min(_rows, (area.OffsetY + area.Height + step - 1) / step);
                for (var x = left; x < right; x++)
                for (var y = top; y < bottom; y++) mask[x, y] = true;
            }

            return mask;
        }

        public bool[,] Not(bool[,] mask) => Map(mask, (_, _, value) => !value);

        public bool[,] And(bool[,] a, bool[,] b) => Map(a, (x, y, value) => value && b[x, y]);

        // open cells within distance of a blocked cell
        public bool[,] Near(bool[,] blocked, int distance)
        {
            var reach = Math.Max(1, distance / step);
            return Map(blocked, (x, y, value) =>
            {
                if (value) return false;
                for (var dx = -reach; dx <= reach; dx++)
                for (var dy = -reach; dy <= reach; dy++)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < _columns && ny < _rows && blocked[nx, ny]) return true;
                }

                return false;
            });
        }

        // the open cells connected to any of the spots (flood fill), or the biggest open region when no spot is on open ground
        public bool[,] Reachable(bool[,] open, List<(double x, double y)> spots)
        {
            var region = new int[_columns, _rows];
            var sizes = new List<int> { 0 };
            for (var x = 0; x < _columns; x++)
            for (var y = 0; y < _rows; y++)
            {
                if (!open[x, y] || region[x, y] != 0) continue;
                sizes.Add(Flood(open, region, x, y, sizes.Count));
            }

            var keep = new HashSet<int>();
            foreach (var (sx, sy) in spots)
            {
                var cx = (int) Math.Clamp(sx / step, 0, _columns - 1);
                var cy = (int) Math.Clamp(sy / step, 0, _rows - 1);
                if (region[cx, cy] != 0) keep.Add(region[cx, cy]);
            }

            if (keep.Count == 0 && sizes.Count > 1) keep.Add(sizes.IndexOf(sizes.Max()));
            return Map(open, (x, y, _) => keep.Contains(region[x, y]));
        }

        private int Flood(bool[,] open, int[,] region, int startX, int startY, int id)
        {
            var size = 0;
            var stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));
            region[startX, startY] = id;
            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                size++;
                foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                {
                    if (nx < 0 || ny < 0 || nx >= _columns || ny >= _rows || !open[nx, ny] || region[nx, ny] != 0) continue;
                    region[nx, ny] = id;
                    stack.Push((nx, ny));
                }
            }

            return size;
        }

        public bool[,] ColorNear((byte r, byte g, byte b)[,] colors, (byte r, byte g, byte b) target, int tolerance)
        {
            return Map(new bool[_columns, _rows], (x, y, _) =>
            {
                var (r, g, b) = colors[x, y];
                var dr = r - target.r;
                var dg = g - target.g;
                var db = b - target.b;
                return Math.Sqrt(dr * dr + dg * dg + db * db) <= tolerance;
            });
        }

        // the middle of every allowed cell, in map pixels
        public List<(double x, double y)> Cells(bool[,] mask)
        {
            var cells = new List<(double, double)>();
            for (var x = 0; x < _columns; x++)
            for (var y = 0; y < _rows; y++)
                if (mask[x, y]) cells.Add((x * step + step / 2.0, y * step + step / 2.0));
            return cells;
        }

        // props stand on the step grid, inside the map
        public (double x, double y) Snap(double x, double y)
        {
            return (Math.Clamp(Math.Round(x / step) * step, 0, width), Math.Clamp(Math.Round(y / step) * step, 0, height));
        }

        public bool Allows(bool[,] mask, double x, double y)
        {
            var cx = (int) Math.Clamp(x / step, 0, _columns - 1);
            var cy = (int) Math.Clamp(y / step, 0, _rows - 1);
            return mask[cx, cy];
        }

        public (byte r, byte g, byte b)[,] Average(byte[] bgra, int imageWidth, int imageHeight)
        {
            var colors = new (byte, byte, byte)[_columns, _rows];
            for (var cx = 0; cx < _columns; cx++)
            for (var cy = 0; cy < _rows; cy++)
            {
                long r = 0, g = 0, b = 0, n = 0;
                for (var px = cx * step; px < Math.Min(imageWidth, (cx + 1) * step); px++)
                for (var py = cy * step; py < Math.Min(imageHeight, (cy + 1) * step); py++)
                {
                    var i = (py * imageWidth + px) * 4;
                    b += bgra[i];
                    g += bgra[i + 1];
                    r += bgra[i + 2];
                    n++;
                }

                colors[cx, cy] = n == 0 ? ((byte) 0, (byte) 0, (byte) 0) : ((byte) (r / n), (byte) (g / n), (byte) (b / n));
            }

            return colors;
        }

        private bool[,] Map(bool[,] source, Func<int, int, bool, bool> value)
        {
            var mask = new bool[_columns, _rows];
            for (var x = 0; x < _columns; x++)
            for (var y = 0; y < _rows; y++) mask[x, y] = value(x, y, source[x, y]);
            return mask;
        }
    }

    #endregion
}
