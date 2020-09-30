using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Nethereum.WSLogStreamingUniswapSample
{
    class Program
    {
        public static async Task Main()
        {
            var token0 = "DAI";
            var token1 = "ETH";
            var pairContractAddress = "0xa478c2975ab1ea89e8196811f51a7b7ade33eb11";

            using (var client = new StreamingWebSocketClient("wss://mainnet.infura.io/ws/v3/7238211010344719ad14a89db874158c"))
            {
                Console.WriteLine($"Uniswap trades for {token0} and {token1}");
                try
                { 
                    var eventSubscription = new EthLogsObservableSubscription(client);
                    eventSubscription.GetSubscriptionDataResponsesAsObservable().Subscribe(log =>
                    {
                        var swap = log.DecodeEvent<SwapEventDTO>();

                        var amount0Out = Web3.Web3.Convert.FromWei(swap.Event.Amount0Out);
                        var amount1In = Web3.Web3.Convert.FromWei(swap.Event.Amount1In);


                        var amount0In = Web3.Web3.Convert.FromWei(swap.Event.Amount0In);
                        var amount1Out = Web3.Web3.Convert.FromWei(swap.Event.Amount1Out);


                        if (swap.Event.Amount0In == 0 && swap.Event.Amount1Out == 0)
                        {

                            var price = amount0Out / amount1In;
                            var quantity = amount1In;

                            Console.WriteLine($"Sell {token1} Price: {price.ToString("F4")} Quantity: {quantity.ToString("F4")}, From: {swap.Event.To}  Block: {swap.Log.BlockNumber}");
                        }
                        else
                        {

                            var price = amount0In / amount1Out;
                            var quantity = amount1Out;
                            Console.WriteLine($"Buy {token1} Price: {price.ToString("F4")} Quantity: {quantity.ToString("F4")}, From: {swap.Event.To}  Block: {swap.Log.BlockNumber}");

                        }
                    }
                    
                    );

                    var filterAuction = Event<SwapEventDTO>.GetEventABI().CreateFilterInput(pairContractAddress);

                    await client.StartAsync();

                    await eventSubscription.SubscribeAsync(filterAuction);

                    await Task.Delay(600000);
                    await eventSubscription.UnsubscribeAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }


    public partial class SwapEventDTO : SwapEventDTOBase { }

    [Event("Swap")]
    public class SwapEventDTOBase : IEventDTO
    {
        [Parameter("address", "sender", 1, true)]
        public virtual string Sender { get; set; }
        [Parameter("uint256", "amount0In", 2, false)]
        public virtual BigInteger Amount0In { get; set; }
        [Parameter("uint256", "amount1In", 3, false)]
        public virtual BigInteger Amount1In { get; set; }
        [Parameter("uint256", "amount0Out", 4, false)]
        public virtual BigInteger Amount0Out { get; set; }
        [Parameter("uint256", "amount1Out", 5, false)]
        public virtual BigInteger Amount1Out { get; set; }
        [Parameter("address", "to", 6, true)]
        public virtual string To { get; set; }
    }
}
