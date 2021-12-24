using AElf.CSharp.Core;
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

        public override Empty MintAllowed(MintAllowedInput input)
        {
            Assert(!State.MintGuardianPaused[input.GToken], "Mint is paused");
            MarketVerify(input.GToken);
            UpdatePlatformTokenSupplyIndex(input.GToken);
            DistributeSupplierPlatformToken(input.GToken, input.Minter, false);
            return new Empty();
        }

        public override Empty MintVerify(MintVerifyInput input)
        {
            return new Empty();
        }

        public override Empty RedeemAllowed(RedeemAllowedInput input)
        {
            RedeemAllowedInternal(input.GToken, input.Redeemer, input.RedeemTokens);
            UpdatePlatformTokenSupplyIndex(input.GToken);
            DistributeSupplierPlatformToken(input.GToken, input.Redeemer, false);
            return new Empty();
        }

        public override Empty RedeemVerify(RedeemVerifyInput input)
        {
            Assert(input.RedeemTokens == 0 && input.RedeemAmount > 0, "RedeemTokens zero");
            return new Empty();
        }

        public override Empty BorrowAllowed(BorrowAllowedInput input)
        {
            Assert(!State.BorrowGuardianPaused[input.GToken], "Borrow is paused");
            MarketVerify(input.GToken);
            if (!State.Markets[input.GToken].AccountMembership
                .TryGetValue(Context.Sender.ToString(), out var isExist) || !isExist)
            {
                AddToMarketInternal(input.GToken, Context.Sender);
            }
            //To do:Check Price in Oracle
            var borrowCap = State.BorrowCaps[input.GToken].Value;
            if (borrowCap != 0)
            {
               var totalBorrows = State.GTokenContract.GetTotalBorrows.Call(input.GToken).Value;
               Assert(totalBorrows.Add(input.BorrowAmount) < borrowCap,"Market borrow cap reached");
            }
            var shortfall =
                GetHypotheticalAccountLiquidityInternal(Context.Sender, input.GToken, 0, input.BorrowAmount);
            Assert(shortfall <= 0, "Insufficient liquidity"); //INSUFFICIENT_LIQUIDITY
             //To do:get borrowIndex from GToken
            long borrowIndex = 1;
            UpdatePlatformTokenBorrowIndex(input.GToken, borrowIndex);
            DistributeBorrowerPlatformToken(input.GToken, input.Borrower, borrowIndex, false);
            
            return new Empty();
        }

        public override Empty BorrowVerify(BorrowVerifyInput input)
        {
            return new Empty();
        }

        public override Empty RepayBorrowAllowed(RepayBorrowAllowedInput input)
        {
            MarketVerify(input.GToken);
            //To do:get borrowIndex from GToken
            long borrowIndex = 1;
            UpdatePlatformTokenBorrowIndex(input.GToken, borrowIndex);
            DistributeBorrowerPlatformToken(input.GToken, input.Borrower, borrowIndex, false);
            return new Empty();
        }

        public override Empty RepayBorrowVerify(RepayBorrowVerifyInput input)
        {
            return new Empty();
        }

        public override Empty LiquidateBorrowAllowed(LiquidateBorrowAllowedInput input)
        {
            MarketVerify(input.GTokenBorrowed);
            MarketVerify(input.GTokenCollateral);
            var shortfall = GetAccountLiquidityInternal(input.Borrower);
            Assert(shortfall > 0, "Insufficient shortfall");
            var borrowBalance = State.GTokenContract.GetBorrowBalanceStored.Call(new GToken.Account()
            {
                GToken = input.GTokenBorrowed,
                User = input.Borrower
            }).Value;
            var maxClose = borrowBalance.Mul(State.CloseFactor.Value);
            Assert(input.RepayAmount <= maxClose,"Too much repay");
            return new Empty();
        }

        public override Empty LiquidateBorrowVerify(LiquidateBorrowVerifyInput input)
        {
            return new Empty();
        }
    }
}