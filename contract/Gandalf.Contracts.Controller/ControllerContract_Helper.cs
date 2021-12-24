using System;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.CSharp.Core;


namespace Gandalf.Contracts.Controller
{
    public partial class ControllerContract
    {
        private void AddToMarketInternal(Address gToken, Address borrower)
        {
            var market = State.Markets[gToken];
            Assert(market != null && market.IsListed, "Market is not listed");
            market.AccountMembership.TryGetValue(borrower.ToString(), out var isMembership);
            if (isMembership)
            {
                return;
            }

            var asset = State.AccountAssets[borrower];
            if (asset == null)
            {
                State.AccountAssets[borrower] = new AssetList();
            }

            Assert(State.AccountAssets[borrower].Assets.Count < State.MaxAssets.Value, "Too Many Assets");
            market.AccountMembership[borrower.ToString()] = true;
            State.AccountAssets[borrower].Assets.Add(gToken);
            Context.Fire(new MarketEntered()
            {
                GToken = gToken,
                Account = borrower
            });
        }
        
        private void RedeemAllowedInternal(Address gToken, Address redeemer, long redeemTokens)
        {
            MarketVerify(gToken);
            State.Markets[gToken].AccountMembership.TryGetValue(redeemer.ToString(), out var isExist);
            if(!isExist)
            {
                return;
            }

            var shortfall = GetHypotheticalAccountLiquidityInternal(redeemer, gToken, redeemTokens, 0);
            Assert(shortfall <= 0, "Insufficient Liquidity");
        }
        private decimal GetHypotheticalAccountLiquidityInternal(Address account, Address gTokenModify, long redeemTokens,
            long borrowAmount)
        {
            var assets = State.AccountAssets[account];
            decimal sumCollateral = 0;
            decimal sumBorrowPlusEffects = 0;
            for (int i = 0; i < assets.Assets.Count; i++)
            {
                var gToken = assets.Assets[i];
                // Read the balances and exchange rate from the cToken
                var accountSnapshot = State.GTokenContract.GetAccountSnapshot.Call(new GToken.Account()
                {
                    GToken = gToken,
                    User = account
                });
                var cTokenBalance = accountSnapshot.GTokenBalance;
                var exchangeRate = decimal.Parse(accountSnapshot.ExchangeRate);
                var symbol = "";
                // var price = State.PriceContract.GetExchangeTokenPriceInfo.Call(
                //     new Price.GetExchangeTokenPriceInfoInput()
                //     {
                //
                //     });
                 var price = decimal.Parse(symbol);
                 
                var collateralFactor = decimal.Parse(State.Markets[gTokenModify].CollateralFactor.ToString());
                var tokensToDenom = exchangeRate * price * collateralFactor;
                sumCollateral += cTokenBalance * tokensToDenom;
                sumBorrowPlusEffects += accountSnapshot.BorrowBalance * price;
                if (gTokenModify == gToken)
                {
                    // redeem effect
                    // sumBorrowPlusEffects += tokensToDenom * redeemTokens
                    sumBorrowPlusEffects += tokensToDenom * redeemTokens;
                    // borrow effect
                    // sumBorrowPlusEffects += oraclePrice * borrowAmount
                    sumBorrowPlusEffects += price * borrowAmount;
                }
            }

            return sumBorrowPlusEffects - sumCollateral;
        }
        private void MarketVerify(Address gToken)
        {
            var market = State.Markets[gToken];
            Assert(market != null && market.IsListed, "Market is not listed");
        }

        private void UpdatePlatformTokenSupplyIndex(Address gToken)
        {
         var supplyState = State.PlatformTokenSupplyState[gToken];
         var supplySpeed = State.PlatformTokenSpeeds[gToken].Value;
         var blockNumber = Context.CurrentHeight;
         var deltaBlocks = blockNumber.Sub(supplyState.Block);
         if (deltaBlocks > 0 && supplySpeed > 0)
         {
             //To do:get totalSupply from GTokenContract;
             long supplyTokens = 1;
             var platformTokenAccrued = deltaBlocks.Mul(supplySpeed);
             var ratio = supplyTokens > 0 ? Fraction(platformTokenAccrued, supplyTokens) : 0;
             var index = supplyState.Index.Add(ratio);
             State.PlatformTokenSupplyState[gToken] = new PlatformTokenMarketState()
             {
                 Index = index,
                 Block = blockNumber
             };
         }
         else if (deltaBlocks > 0)
         {
             State.PlatformTokenSupplyState[gToken].Block = blockNumber;
         }
        }
        
        private void UpdatePlatformTokenBorrowIndex(Address gToken, long marketBorrowIndex)
        {
            var borrowState = State.PlatformTokenBorrowState[gToken];
            var borrowSpeed = State.PlatformTokenSpeeds[gToken].Value;
            var blockNumber = Context.CurrentHeight;
            var deltaBlocks = blockNumber.Sub(borrowState.Block);
            if (deltaBlocks > 0 && borrowSpeed > 0)
            {
                //To do:get totalBorrows from GTokenContract;
                long totalBorrows = 1;
                var borrowAmount = totalBorrows.Div(marketBorrowIndex);
                var platformTokenAccrued = deltaBlocks.Mul(borrowSpeed);
                var ratio = borrowAmount > 0 ? Fraction(platformTokenAccrued, borrowAmount) : 0;
                var index = borrowState.Index.Add(ratio);
                State.PlatformTokenBorrowState[gToken] = new PlatformTokenMarketState()
                {
                    Index = index,
                    Block = blockNumber
                };
            }
            else if (deltaBlocks > 0)
            {
                State.PlatformTokenBorrowState[gToken].Block = blockNumber;
            }
        }

        private void DistributeSupplierPlatformToken(Address gToken, Address supplier, bool distributeAll)
        {
            
        }
        private void DistributeBorrowerPlatformToken(Address gToken, Address borrower, long marketBorrowIndex, bool distributeAll)
        {
            
        }

        private decimal GetAccountLiquidityInternal(Address account)
        {
            return GetHypotheticalAccountLiquidityInternal(account,Context.GetZeroSmartContractAddress(),0,0);
        }
        private static long Fraction(long a, long b)
        {
            return Convert.ToInt64(new BigIntValue(a).Mul(Decimals).Div(b));
        }
    }
}