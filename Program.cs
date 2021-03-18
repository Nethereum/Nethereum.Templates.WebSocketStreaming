using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.RPC.Reactive.Eth;

namespace Nethereum.WSLogStreamingUniswapSample
{
    class Program
    {

        public static async Task Main()
        {
            //await SubscribeToSwaps();
            await SubscribeTosync();
        }

        public static async Task SubscribeTosync() //Emitted each time reserves are updated via mint, burn, swap, or sync. https://uniswap.org/docs/v2/smart-contracts/pair/
        {
            string uniSwapFactoryAddress = "0x5C69bEe701ef814a2B6a3EDD4B1652CB9cc5aA6f";

            var web3 = new Web3.Web3("https://mainnet.infura.io/v3/7238211010344719ad14a89db874158c");


            string daiAddress = "0x6b175474e89094c44da98b954eedeac495271d0f";
            string wethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";

            var pairContractAddress = await web3.Eth.GetContractQueryHandler<GetPairFunction>()
                .QueryAsync<string>(uniSwapFactoryAddress,
                    new GetPairFunction() { TokenA = daiAddress, TokenB = wethAddress });

            var filter = Event<PairSyncEventDTO>.GetEventABI()
                .CreateFilterInput( new[] {pairContractAddress});

            using (var client = new StreamingWebSocketClient("wss://mainnet.infura.io/ws/v3/7238211010344719ad14a89db874158c"))
            {
                var subscription = new EthLogsObservableSubscription(client);
                subscription.GetSubscriptionDataResponsesAsObservable().
                             Subscribe(log =>
                             {
                                 try
                                 {
                                     EventLog<PairSyncEventDTO> decoded = Event<PairSyncEventDTO>.DecodeEvent(log);
                                     if (decoded != null)
                                     {
                                         decimal reserve0 = Web3.Web3.Convert.FromWei(decoded.Event.Reserve0);
                                         decimal reserve1 = Web3.Web3.Convert.FromWei(decoded.Event.Reserve1);
                                         Console.WriteLine($@"Price={reserve0 / reserve1}");
                                     }
                                     else Console.WriteLine(@"Found not standard transfer log");
                                 }
                                 catch (Exception ex)
                                 {
                                     Console.WriteLine(@"Log Address: " + log.Address + @" is not a standard transfer log:", ex.Message);
                                 }
                             });

                await client.StartAsync();
                subscription.GetSubscribeResponseAsObservable().Subscribe(id => Console.WriteLine($"Subscribed with id: {id}"));
                await subscription.SubscribeAsync(filter);

                while (true) //pinging to keep alive infura
                {
                    var handler = new EthBlockNumberObservableHandler(client);
                    handler.GetResponseAsObservable().Subscribe(x => Console.WriteLine(x.Value));
                    await handler.SendRequestAsync();
                    Thread.Sleep(30000);
                }

            }
        }
    

         public static async Task SubscribeToSwaps() //https://uniswap.org/docs/v2/smart-contracts/pair/ Emitted each time a swap occurs via swap.
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

                    eventSubscription.GetSubscribeResponseAsObservable().Subscribe(id => Console.WriteLine($"Subscribed with id: {id}"));

                    var filterAuction = Event<SwapEventDTO>.GetEventABI().CreateFilterInput(pairContractAddress);

                    await client.StartAsync();

                    await eventSubscription.SubscribeAsync(filterAuction);

                    Console.ReadLine();
               
                    await eventSubscription.UnsubscribeAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }

    [Event("Sync")]
    class PairSyncEventDTO : IEventDTO
    {
        [Parameter("uint112", "reserve0")]
        public virtual BigInteger Reserve0 { get; set; }

        [Parameter("uint112", "reserve1", 2)]
        public virtual BigInteger Reserve1 { get; set; }
    }




    public partial class GetPairFunction : GetPairFunctionBase { }

    [Function("getPair", "address")]
    public class GetPairFunctionBase : FunctionMessage
    {
        [Parameter("address", "tokenA", 1)]
        public virtual string TokenA { get; set; }
        [Parameter("address", "tokenB", 2)]
        public virtual string TokenB { get; set; }
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
