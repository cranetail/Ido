using System;
using AElf;
using AElf.Contracts.Price;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Sdk.CSharp;
namespace Awaken.Contracts.Controller
{
    /// <summary>
    /// The C# implementation of the contract defined in controller_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class ControllerContract : ControllerContractContainer.ControllerContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.TokenContract.Value == null, "Already initialized.");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.ATokenContract.Value = input.ATokenContract;
            State.Admin.Value = Context.Sender;
            return new Empty();
        }
        public override Empty EnterMarkets(ATokens input)
        {
            var len = input.AToken.Count;
            for (var i = 0; i < len; i++)
            {
                var aToken = input.AToken[i];
                AddToMarketInternal(aToken, Context.Sender);
            }

            return new Empty();
        }
        
        public override Empty ExitMarket(Address aToken)
        {
            // MarketVerify(input.Value);
            var result = State.ATokenContract.GetAccountSnapshot.Call(new Awaken.Contracts.AToken.Account()
            {
                AToken = aToken,
                User = Context.Sender
            });
            Assert(result.BorrowBalance == 0, "Nonzero borrow balance");
            if (!State.Markets[aToken].AccountMembership.TryGetValue(Context.Sender.ToString(), out var isExist) ||
                !isExist)
            {
                return new Empty();
            }
            
            var shortfall =
                GetHypotheticalAccountLiquidityInternal(Context.Sender, aToken, result.ATokenBalance, 0);
            Assert(shortfall <= 0, "Insufficient liquidity"); //INSUFFICIENT_LIQUIDITY
            State.Markets[aToken].AccountMembership[Context.Sender.ToString()] = false;
            //Delete cToken from the account’s list of assets
            var userAssetList = State.AccountAssets[Context.Sender];
            userAssetList.Assets.Remove(aToken);
            Context.Fire(new MarketExited()
            {
                AToken = aToken,
                Account = Context.Sender
            });
            return new Empty();
        }

        public override Empty MintAllowed(MintAllowedInput input)
        {
            Assert(!State.MintGuardianPaused[input.AToken], "Mint is paused");
            MarketVerify(input.AToken);
            UpdatePlatformTokenSupplyIndex(input.AToken);
            DistributeSupplierPlatformToken(input.AToken, input.Minter, false);
            return new Empty();
        }

        public override Empty MintVerify(MintVerifyInput input)
        {
            return new Empty();
        }

        public override Empty RedeemAllowed(RedeemAllowedInput input)
        {
            RedeemAllowedInternal(input.AToken, input.Redeemer, input.RedeemTokens);
            UpdatePlatformTokenSupplyIndex(input.AToken);
            DistributeSupplierPlatformToken(input.AToken, input.Redeemer, false);
            return new Empty();
        }

        public override Empty RedeemVerify(RedeemVerifyInput input)
        {
            Assert(input.RedeemTokens == 0 && input.RedeemAmount > 0, "RedeemTokens zero");
            return new Empty();
        }

        public override Empty BorrowAllowed(BorrowAllowedInput input)
        {
            Assert(!State.BorrowGuardianPaused[input.AToken], "Borrow is paused");
            MarketVerify(input.AToken);
            if (!State.Markets[input.AToken].AccountMembership
                .TryGetValue(Context.Sender.ToString(), out var isExist) || !isExist)
            {
                AddToMarketInternal(input.AToken, Context.Sender);
            }
            //To do:Check Price in Oracle
            var borrowCap = State.BorrowCaps[input.AToken].Value;
            if (borrowCap != 0)
            {
               var totalBorrows = State.ATokenContract.GetTotalBorrows.Call(input.AToken).Value;
               Assert(totalBorrows.Add(input.BorrowAmount) < borrowCap,"Market borrow cap reached");
            }
            var shortfall =
                GetHypotheticalAccountLiquidityInternal(Context.Sender, input.AToken, 0, input.BorrowAmount);
            Assert(shortfall <= 0, "Insufficient liquidity"); //INSUFFICIENT_LIQUIDITY
             //To do:get borrowIndex from AToken
            long borrowIndex = 1;
            UpdatePlatformTokenBorrowIndex(input.AToken, borrowIndex);
            DistributeBorrowerPlatformToken(input.AToken, input.Borrower, borrowIndex, false);
            
            return new Empty();
        }

        public override Empty BorrowVerify(BorrowVerifyInput input)
        {
            return new Empty();
        }

        public override Empty RepayBorrowAllowed(RepayBorrowAllowedInput input)
        {
            MarketVerify(input.AToken);
            //To do:get borrowIndex from AToken
            long borrowIndex = 1;
            UpdatePlatformTokenBorrowIndex(input.AToken, borrowIndex);
            DistributeBorrowerPlatformToken(input.AToken, input.Borrower, borrowIndex, false);
            return new Empty();
        }

        public override Empty RepayBorrowVerify(RepayBorrowVerifyInput input)
        {
            return new Empty();
        }

        public override Empty LiquidateBorrowAllowed(LiquidateBorrowAllowedInput input)
        {
            MarketVerify(input.ATokenBorrowed);
            MarketVerify(input.ATokenCollateral);
            var shortfall = GetAccountLiquidityInternal(input.Borrower);
            Assert(shortfall > 0, "Insufficient shortfall");
            var borrowBalance = State.ATokenContract.GetBorrowBalanceStored.Call(new Awaken.Contracts.AToken.Account()
            {
                AToken = input.ATokenBorrowed,
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

        public override Empty SeizeAllowed(SeizeAllowedInput input)
        {
            Assert(!State.SeizeGuardianPaused.Value, "Seize is paused");
            MarketVerify(input.ATokenBorrowed);
            MarketVerify(input.ATokenCollateral);
            UpdatePlatformTokenSupplyIndex(input.ATokenCollateral);
            DistributeSupplierPlatformToken(input.ATokenCollateral, input.Borrower, false);
            DistributeSupplierPlatformToken(input.ATokenCollateral,input.Liquidator,false);
            return new Empty();
        }

        public override Empty SeizeVerify(SeizeVerifyInput input)
        {
            return new Empty();
        }

        public override Empty TransferAllowed(TransferAllowedInput input)
        {
            RedeemAllowedInternal(input.AToken, input.Src, input.TransferTokens);
            UpdatePlatformTokenSupplyIndex(input.AToken);
            DistributeSupplierPlatformToken(input.AToken,input.Src,false);
            DistributeSupplierPlatformToken(input.AToken,input.Dst,false);
            return base.TransferAllowed(input);
        }

        public override Empty TransferVerify(TransferVerifyInput input)
        {
            return new Empty();
        }

        public override Int64Value LiquidateCalculateSeizeTokens(LiquidateCalculateSeizeTokensInput input)
        {
            var priceBorrow = GetUnderlyingPrice(input.ATokenBorrowed);
            var priceCollateral= GetUnderlyingPrice(input.ATokenCollateral);
           
             
            Assert(priceBorrow != 0 && priceCollateral != 0, "Error Price");
            var exchangeRate = State.ATokenContract.GetExchangeRateStored.Call(input.ATokenCollateral).Value;
            var numerator = new BigIntValue(priceBorrow).Mul(State.LiquidationIncentive.Value);
            var denominator = new BigIntValue(priceCollateral).Mul(exchangeRate);
            var seizeTokens = numerator.Div(denominator).Mul(input.ActualRepayAmount);
            
            return new Int64Value()
            {
                Value = Convert.ToInt64(seizeTokens.ToString())
            };
        }

        public override Empty SupportMarket(Address input)
        {
            Assert(Context.Sender == State.Admin.Value, "Unauthorized");
          
            var market = State.Markets[input];
            if (market != null)
            {
                Assert(!market.IsListed, "Support market exists"); //
            }
            State.Markets[input] = new Market()
            {
                IsListed = true
            };
            AddMarketInternal(input);
            Context.Fire(new MarketListed
            {
                AToken = input
            });

            return new Empty();
        }

        public override Empty RefreshPlatformTokenSpeeds(Empty input)
        {
            Assert(Context.Origin == Context.Sender, "Only externally owned accounts may refresh speeds");
            RefreshPlatformTokenSpeedsInternal();
            return new Empty();
        }

        public override Empty ClaimPlatformToken(ClaimPlatformTokenInput input)
        {
            foreach (var aToken in input.ATokens)
            {
                Assert(State.Markets[aToken].IsListed, "market must be listed");
                if (input.Borrowers)
                {
                    var borrowIndex = State.ATokenContract.GetBorrowIndex.Call(aToken).Value;
                    UpdatePlatformTokenBorrowIndex(aToken, borrowIndex);
                    foreach (var t in input.Holders)
                    {
                        DistributeBorrowerPlatformToken(aToken, t, borrowIndex, true);
                    }
                }

                if (!input.Suppliers) continue;
                {
                    UpdatePlatformTokenSupplyIndex(aToken);
                    foreach (var t in input.Holders)
                    {
                        DistributeSupplierPlatformToken(aToken, t, true);
                    }
                }
            }

            return new Empty();
        }

        
        public override Empty AddPlatformTokenMarkets(ATokens input)
        {
            Assert(Context.Sender == State.Admin.Value, "Only admin can add platformToken market");
            foreach (var t in input.AToken)
            {
                AddPlatformTokenMarketInternal(t);
            }

            return new Empty();
        }

        public override Empty DropPlatformTokenMarket(Address input)
        {
            Assert(Context.Sender == State.Admin.Value, "Only admin can drop platformToken market");
            var market = State.Markets[input];
            Assert(market.IsListed, "platformToken market is not listed");
            Assert(market.IsPlatformTokened, "platformToken market already added");
            market.IsPlatformTokened = false;
            Context.Fire(new MarketPlatformTokened()
            {
                AToken = input,
                IsPlatformTokened = false
            });
            RefreshPlatformTokenSpeedsInternal();
            return new Empty();
        }

       
    }
}