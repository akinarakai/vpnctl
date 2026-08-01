public static class IpAllocator
{
    public static string GetNextIp(string subnetPrefix, long clientId)
    {
        var index = clientId - 1;

        var thirdOctet = index / 253;
        var fourthOctet = (index % 253) + 2;

        if (thirdOctet > 255)
            throw new InvalidOperationException("No more available IP addresses.");

        return $"{subnetPrefix}.{thirdOctet}.{fourthOctet}";
    }
}