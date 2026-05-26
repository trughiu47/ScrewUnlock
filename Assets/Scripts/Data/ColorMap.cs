using UnityEngine;
using System.Collections.Generic;

public static class ColorMap
{
    static readonly Dictionary<BlockColor, Color> map = new Dictionary<BlockColor, Color>
    {
        { BlockColor.Cyan,   new Color(0.30f, 0.78f, 0.90f) },
        { BlockColor.Orange, new Color(0.95f, 0.52f, 0.15f) },
        { BlockColor.Red,    new Color(0.88f, 0.22f, 0.22f) },
        { BlockColor.Blue,   new Color(0.20f, 0.42f, 0.90f) },
        { BlockColor.Green,  new Color(0.22f, 0.78f, 0.32f) },
        { BlockColor.Yellow, new Color(0.95f, 0.85f, 0.10f) },
        { BlockColor.Purple, new Color(0.60f, 0.22f, 0.82f) },
    };

    public static Color Get(BlockColor c)
    {
        return map.TryGetValue(c, out Color col) ? col : Color.white;
    }
}
