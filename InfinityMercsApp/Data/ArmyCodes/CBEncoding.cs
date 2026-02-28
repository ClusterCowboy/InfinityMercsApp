using System.Text;

namespace InfinityMercsApp.Data.ArmyCodes;

/// <summary>
/// IN PROGRESS, NOT CURRENTLY WORKING
/// 
/// Read/Write Army Codes
/// </summary>
/// <param name="Group"></param>
/// <param name="UnitId"></param>
/// <param name="Profile"></param>
/// <param name="Option"></param>
/// <param name="Flags"></param>

public readonly record struct UnitRec(byte Group, byte UnitId, byte Profile, byte Option, byte Flags);

public static class CBEncoding
{
    public static (string FactionSlug, byte[] Header, List<UnitRec> Units) Decode(string code)
    {
        var raw = Uri.UnescapeDataString(code);
        var bytes = Convert.FromBase64String(raw);

        var slugLength = bytes[1];
        var factionSlug = Encoding.ASCII.GetString(bytes, 2, slugLength);

        var index = 2 + slugLength;
        var header = bytes[index..(index + 8)];
        index += 8;

        var count = bytes[index++];
        var units = new List<UnitRec>(count);
        for (var i = 0; i < count; i++)
        {
            units.Add(new UnitRec(
                Group: bytes[index++],
                UnitId: bytes[index++],
                Profile: bytes[index++],
                Option: bytes[index++],
                Flags: bytes[index++]));
        }

        return (factionSlug, header, units);
    }

    public static string Encode(string factionSlug, byte[] header, IEnumerable<UnitRec> units)
    {
        var list = units.ToList();
        var bytes = new List<byte>();

        bytes.Add(0x65);
        bytes.Add((byte)factionSlug.Length);
        bytes.AddRange(Encoding.ASCII.GetBytes(factionSlug));
        bytes.AddRange(header);
        bytes.Add((byte)list.Count);

        foreach (var unit in list)
        {
            bytes.Add(unit.Group);
            bytes.Add(unit.UnitId);
            bytes.Add(unit.Profile);
            bytes.Add(unit.Option);
            bytes.Add(unit.Flags);
        }

        return Uri.EscapeDataString(Convert.ToBase64String(bytes.ToArray()));
    }
}
