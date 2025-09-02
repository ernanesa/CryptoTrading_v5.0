using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Generated;

public class Test
{
    public void TestTypes()
    {
        // Test candle types
        CandleData candle = new CandleData();
        var timestamp = candle.T; // Check actual property names

        ListCandlesResponse response = new ListCandlesResponse();
        var closes = response.C; // Check actual property names
    }
}
