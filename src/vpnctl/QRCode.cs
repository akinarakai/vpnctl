using QRCoder;

public static class QRCode
{
    public static void Render(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);

        var matrix = qrCodeData.ModuleMatrix;
        for (int row = 0; row < matrix.Count; row++)
        {
            for (int col = 0; col < matrix[row].Count; col++)
            {
                if (matrix[row][col])
                {
                    Console.Write("██");
                }
                else
                {
                    Console.Write("  ");
                }
            }
            Console.WriteLine();
        }
    }
}