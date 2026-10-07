using System;
using System.Linq;
using Godot;

/// <summary>
/// A generated test landscape about 16 km square: rolling hills, and a canyon winding north to south
/// through a band of high ground, past the jet's start. Its edges sink to the flat ground around it.
/// The same heights build the mesh and its collision, so the jet can crash into what it sees.
/// </summary>
public partial class TestTerrain : MeshInstance3D
{
    const float Size = 16000;      // meters along each side
    const int Cells = 512;         // grid squares along each side, 31.25 m each
    const float HillHeight = 700;  // meters at the highest
    const float RimHeight = 450;   // the high ground the canyon cuts through, in meters
    const float FloorHeight = 30;  // the canyon floor, in meters
    const float FloorHalfWidth = 150, RimHalfWidth = 400;  // meters from the canyon's middle to its floor's edge and to its rim
    const float EdgeFade = 1500;   // meters over which the land sinks to the flat ground at its edges

    readonly FastNoiseLite _noise = new() { Seed = 7, Frequency = 1 / 3000f, FractalOctaves = 4 };

    public override void _Ready()
    {
        const int n = Cells + 1;  // grid points along each side
        const float step = Size / Cells;
        var heights = new float[n * n];
        var vertices = new Vector3[n * n];
        for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var position = new Vector2(x * step - Size / 2, z * step - Size / 2);
                heights[z * n + x] = Height(position.X, position.Y);
                vertices[z * n + x] = new Vector3(position.X, heights[z * n + x], position.Y);
            }

        // Each point's normal leans away from the slope between its neighbours.
        var normals = new Vector3[n * n];
        for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var acrossX = heights[z * n + Math.Min(x + 1, n - 1)] - heights[z * n + Math.Max(x - 1, 0)];
                var acrossZ = heights[Math.Min(z + 1, n - 1) * n + x] - heights[Math.Max(z - 1, 0) * n + x];
                normals[z * n + x] = new Vector3(-acrossX, 2 * step, -acrossZ).Normalized();
            }

        // Two triangles per grid square, clockwise seen from above, which Godot draws as the front.
        var indices = new int[Cells * Cells * 6];
        var k = 0;
        for (var z = 0; z < Cells; z++)
            for (var x = 0; x < Cells; x++)
            {
                var i = z * n + x;
                indices[k++] = i; indices[k++] = i + 1; indices[k++] = i + n + 1;
                indices[k++] = i; indices[k++] = i + n + 1; indices[k++] = i + n;
            }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Mesh = mesh;

        // A height map's points sit 1 m apart, so it's scaled up to the grid, heights included.
        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D
        {
            Shape = new HeightMapShape3D { MapWidth = n, MapDepth = n, MapData = heights.Select(h => h / step).ToArray() },
            Scale = Vector3.One * step,
        });
        AddChild(body);
    }

    float Height(float x, float z)
    {
        var hills = HillHeight * Mathf.Clamp(_noise.GetNoise2D(x, z) * 0.5f + 0.5f, 0, 1);

        // How far the point is from the canyon's middle, measured across the canyon rather than along x.
        var middle = 1200 * MathF.Sin(z / 2000) + 300 * MathF.Sin(z / 600 + 1);
        var slope = 1200f / 2000 * MathF.Cos(z / 2000) + 300f / 600 * MathF.Cos(z / 600 + 1);
        var across = MathF.Abs(x - middle) / MathF.Sqrt(1 + slope * slope);

        // High ground along the canyon, so its walls stand tall, then the canyon cut into it.
        var ground = MathF.Max(hills, RimHeight * (1 - Mathf.SmoothStep(600, 2000, across)));
        var height = Mathf.Lerp(FloorHeight, ground, Mathf.SmoothStep(FloorHalfWidth, RimHalfWidth, across));

        var fromEdge = Size / 2 - MathF.Max(MathF.Abs(x), MathF.Abs(z));
        return height * Mathf.SmoothStep(0, EdgeFade, fromEdge);
    }
}
