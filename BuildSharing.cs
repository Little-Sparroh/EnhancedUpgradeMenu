using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

public class EquippedUpgrades
{
    public int GearID;
    public List<EquippedUpgrade> Upgrades;

    public EquippedUpgrades(int gearID)
    {
        GearID = gearID;
        Upgrades = new();
    }

    public EquippedUpgrades(int gearID, List<EquippedUpgrade> upgrades)
    {
        GearID = gearID;
        Upgrades = upgrades;
    }

    public EquippedUpgrades(string code)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code cannot be null or empty", nameof(code));

        var bytes = Convert.FromBase64String(code);
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);
        GearID = 0; // Not stored in code
        Upgrades = new List<EquippedUpgrade>();
        while (ms.Position < ms.Length)
        {
            var posBytes = br.ReadBytes(2);
            Array.Reverse(posBytes);
            short pos = BitConverter.ToInt16(posBytes, 0);
            var idBytes = br.ReadBytes(4);
            Array.Reverse(idBytes);
            int id = BitConverter.ToInt32(idBytes, 0);
            int x = (pos >> 6) & 0x7;
            int y = (pos >> 3) & 0x7;
            byte rotation = (byte)(pos & 0x7);
            Upgrades.Add(new EquippedUpgrade(x, y, rotation, id));
        }
    }

    public void Add(EquippedUpgrade upgrade)
    {
        Upgrades.Add(upgrade);
    }

    public override string ToString()
    {
        var binList = new List<byte>();
        foreach (var u in Upgrades)
        {
            short pos = (short)((u.X << 6) | (u.Y << 3) | u.Rotation);
            var posHostNet = IPAddress.HostToNetworkOrder(pos);
            binList.AddRange(BitConverter.GetBytes(posHostNet));
            var idHostNet = IPAddress.HostToNetworkOrder(u.ID);
            binList.AddRange(BitConverter.GetBytes(idHostNet));
        }
        return Convert.ToBase64String(binList.ToArray()).TrimEnd('=');
    }

}

public class EquippedUpgrade
{
    public int X;
    public int Y;
    public byte Rotation;
    public int ID;

    public EquippedUpgrade(int x, int y, byte rotation, int id)
    {
        X = x;
        Y = y;
        Rotation = rotation;
        ID = id;
    }
}
