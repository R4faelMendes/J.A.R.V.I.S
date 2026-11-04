namespace Jarvis.Core;
public static class WakeOnLan
{
    public static byte[] CriarPacote(string mac)
    {
        var limpo = mac.Replace(":", "").Replace("-", "");
        if (limpo.Length != 12 || !limpo.All(Uri.IsHexDigit)) throw new ArgumentException("MAC inválido. Use AA:BB:CC:DD:EE:FF.");
        var address = Convert.FromHexString(limpo); var packet = new byte[102];
        Array.Fill(packet, (byte)255, 0, 6);
        for (var i = 0; i < 16; i++) Buffer.BlockCopy(address, 0, packet, 6 + i * 6, 6);
        return packet;
    }
}
