using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Sdk.CSharp;
namespace Gandalf.Contracts.Controller
{
    /// <summary>
    /// The C# implementation of the contract defined in controller_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class ControllerContract : ControllerContractContainer.ControllerContractBase
    {
        public override Empty EnterMarkets(GTokens input)
        {
            var len = input.GToken.Count;
            for (var i = 0; i < len; i++)
            {
                var gToken = input.GToken[i];
                AddToMarketInternal(gToken, Context.Sender);
            }

            return new Empty();
        }
        
        public override Empty ExitMarket(Address gToken)
        {
            // MarketVerify(input.Value);
          var   result =State.GTokenContract.GetAccountSnapshot.Call(new GToken.Account()
             {
                 GToken = gToken,
                 User = Context.Sender
             });
            Assert(result.BorrowBalance == 0, "Nonzero borrow balance");
            if (!State.Markets[gToken].AccountMembership.TryGetValue(Context.Sender.ToString(), out var isExist) ||
                !isExist)
            {
                return new Empty();
            }
            
            var shortfall =
                GetHypotheticalAccountLiquidityInternal(Context.Sender, gToken, result.GTokenBalance, 0);
            Assert(shortfall <= 0, "Insufficient liquidity"); //INSUFFICIENT_LIQUIDITY
            State.Markets[gToken].AccountMembership[Context.Sender.ToString()] = false;
            //Delete cToken from the account’s list of assets
            var userAssetList = State.AccountAssets[Context.Sender];
            userAssetList.Assets.Remove(gToken);
            Context.Fire(new MarketExited()
            {
                GToken = gToken,
                Account = Context.Sender
            });
            return new Empty();
        }
    }
}