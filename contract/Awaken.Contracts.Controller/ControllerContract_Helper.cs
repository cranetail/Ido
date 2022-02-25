using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Price;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.CSharp.Core;


namespace Awaken.Contracts.Controller
{
    public partial class ControllerContract
    {
        private void AddToMarketInternal(Address aToken, Address borrower)
        {
            var market = State.Markets[aToken];
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
            State.AccountAssets[borrower].Assets.Add(aToken);
            Context.Fire(new MarketEntered()
            {
                AToken = aToken,
                Account = borrower
            });
        }
        
        private void RedeemAllowedInternal(Address aToken, Address redeemer, long redeemTokens)
        {
            MarketVerify(aToken);
            State.Markets[aToken].AccountMembership.TryGetValue(redeemer.ToString(), out var isExist);
            if(!isExist)
            {
                return;
            }

            var shortfall = GetHypotheticalAccountLiquidityInternal(redeemer, aToken, redeemTokens, 0);
            Assert(shortfall <= 0, "Insufficient Liquidity");
        }
        private long GetHypotheticalAccountLiquidityInternal(Address account, Address aTokenModify, long redeemTokens,
            long borrowAmount)
        {
            var assets = State.AccountAssets[account];
            var sumCollateral = new BigIntValue(){Value = "0"};
            var sumBorrowPlusEffects = new BigIntValue(){Value = "0"};
            for (var i = 0; i < assets.Assets.Count; i++)
            {
                var aToken = assets.Assets[i];
                // Read the balances and exchange rate from the cToken
                var accountSnapshot = State.ATokenContract.GetAccountSnapshot.Call(new Awaken.Contracts.AToken.Account()
                {
                    AToken = aToken,
                    User = account
                });
                var aTokenBalance = accountSnapshot.ATokenBalance;
                var exchangeRate = accountSnapshot.ExchangeRate;
                var price = GetUnderlyingPrice(aToken);
                 
                var collateralFactor = State.Markets[aTokenModify].CollateralFactor;
                var tokensToDenom = new BigIntValue(exchangeRate).Mul(price).Mul(collateralFactor).Div(Mantissa).Div(Mantissa);
                sumCollateral = sumCollateral.Add(new BigIntValue(aTokenBalance).Mul(tokensToDenom).Div(Mantissa));
                sumBorrowPlusEffects = sumBorrowPlusEffects.Add(new BigIntValue(accountSnapshot.BorrowBalance).Mul(price).Div(Mantissa));
                if (aTokenModify == aToken)
                {
                    // redeem effect
                    // sumBorrowPlusEffects += tokensToDenom * redeemTokens
                    sumBorrowPlusEffects  = sumBorrowPlusEffects.Add(new BigIntValue(tokensToDenom).Mul(redeemTokens).Div(Mantissa));
                    // borrow effect
                    // sumBorrowPlusEffects += oraclePrice * borrowAmount
                    sumBorrowPlusEffects = sumBorrowPlusEffects.Add(new BigIntValue(price).Mul(borrowAmount).Div(Mantissa));
                }
            }

            var liquidityStr = sumBorrowPlusEffects.Sub(sumCollateral).Value;
            if (!long.TryParse(liquidityStr, out var liquidity))
            {
                throw new AssertionException($"Failed to parse {liquidityStr}");
            }
            return liquidity;
        }
        private void MarketVerify(Address aToken)
        {
            var market = State.Markets[aToken];
            Assert(market != null && market.IsListed, "Market is not listed");
        }

        private void UpdatePlatformTokenSupplyIndex(Address aToken)
        {
            State.PlatformTokenSupplyState[aToken] =
                State.PlatformTokenSupplyState[aToken] ?? new PlatformTokenMarketState();
            var supplyState = State.PlatformTokenSupplyState[aToken];
            var supplySpeed = State.PlatformTokenSpeeds[aToken];
            var blockNumber = Context.CurrentHeight;
            var deltaBlocks = blockNumber.Sub(supplyState.Block);
            switch (deltaBlocks > 0)
            {
                case true when supplySpeed > 0:
                {
                    //To do:get totalSupply from ATokenContract;
                    var supplyTokens = State.ATokenContract.GetTotalSupply.Call(aToken).Value;
                    var platformTokenAccrued = deltaBlocks.Mul(supplySpeed);
                    var ratio = supplyTokens > 0 ? Fraction(platformTokenAccrued, supplyTokens) : 0;
                    var index = supplyState.Index.Add(ratio);
                    State.PlatformTokenSupplyState[aToken] = new PlatformTokenMarketState()
                    {
                        Index = index,
                        Block = blockNumber
                    };
                    break;
                }
                case true:
                    State.PlatformTokenSupplyState[aToken].Block = blockNumber;
                    break;
            }
        }
        
        private void UpdatePlatformTokenBorrowIndex(Address aToken, long marketBorrowIndex)
        {
            State.PlatformTokenBorrowState[aToken] =
                State.PlatformTokenBorrowState[aToken] ?? new PlatformTokenMarketState();
            var borrowState = State.PlatformTokenBorrowState[aToken];
            var borrowSpeed = State.PlatformTokenSpeeds[aToken];
            var blockNumber = Context.CurrentHeight;
            var deltaBlocks = blockNumber.Sub(borrowState.Block);
            switch (deltaBlocks > 0)
            {
                case true when borrowSpeed > 0:
                {
                    //To do:get totalBorrows from ATokenContract;
                    var totalBorrows = State.ATokenContract.GetTotalBorrows.Call(aToken).Value;;
                    var borrowAmount = totalBorrows.Div(marketBorrowIndex);
                    var platformTokenAccrued = deltaBlocks.Mul(borrowSpeed);
                    var ratio = borrowAmount > 0 ? Fraction(platformTokenAccrued, borrowAmount) : 0;
                    var index = borrowState.Index.Add(ratio);
                    State.PlatformTokenBorrowState[aToken] = new PlatformTokenMarketState()
                    {
                        Index = index,
                        Block = blockNumber
                    };
                    break;
                }
                case true:
                    State.PlatformTokenBorrowState[aToken].Block = blockNumber;
                    break;
            }
        }

        private void DistributeSupplierPlatformToken(Address aToken, Address supplier, bool distributeAll)
        {
            
        }
        private void DistributeBorrowerPlatformToken(Address aToken, Address borrower, long marketBorrowIndex, bool distributeAll)
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
        
        private long GetUnderlyingPrice(Address aToken)
        {
            var underlyingSymbol = State.ATokenContract.GetUnderlying.Call(aToken).Value;
            var priceStr = State.PriceContract.GetExchangeTokenPriceInfo.Call(new GetExchangeTokenPriceInfoInput()
            {
                TokenSymbol = underlyingSymbol,
                TargetTokenSymbol = "",
            });
            if (!long.TryParse(priceStr.Value, out var price))
            {
                throw new AssertionException($"Failed to parse {priceStr.Value}");
            }

            return price;
        }

        private void AddMarketInternal(Address aToken)
        {
            var list = State.AllMarkets.Value ?? new ATokens();
            foreach (var t in list.AToken)
            {
                Assert(t == aToken,"Market already added");
            }
            list.AToken.Add(aToken);
            State.AllMarkets.Value = list;
        }

        private void RefreshPlatformTokenSpeedsInternal()
        {
            var list = State.AllMarkets.Value ?? new ATokens();
            foreach (var g in list.AToken)
            {
                var borrowIndex = State.ATokenContract.GetBorrowIndex.Call(g).Value;
                UpdatePlatformTokenSupplyIndex(g);
                UpdatePlatformTokenBorrowIndex(g,borrowIndex);
            }
            long totalUtility = 0;
            var length = list.AToken.Count;
            var utilities = new List<long>(length);
            for (var i = 0; i < list.AToken.Count; i++)
            {
               
                if (State.IsPlatformTokened[list.AToken[i]].Value)
                {
                   var price = GetUnderlyingPrice(list.AToken[i]);
                   var totalBorrows = State.ATokenContract.GetTotalBorrows.Call(list.AToken[i]).Value;
                   utilities[i] = price.Mul(totalBorrows);
                   totalUtility = totalUtility.Add(utilities[i]);
                }
                 
            }
            
            for (var i = 0; i < list.AToken.Count; i++)
            {
                var newSpeed = totalUtility > 0 ? State.PlatformTokenRate.Value.Mul(utilities[i]).Div(totalUtility) : 0;
                State.PlatformTokenSpeeds[list.AToken[i]] = newSpeed;
                Context.Fire(new PlatformTokenSpeedUpdated()
                {
                    AToken = list.AToken[i],
                    NewSpeed = newSpeed
                });
            }
        }

        private void AddPlatformTokenMarketInternal(Address aToken)
        {
            var market = State.Markets[aToken];
            Assert(market.IsListed, "platformToken market is not listed");
            Assert(market.IsPlatformTokened == false, "platformToken market already added");
            
            market.IsPlatformTokened = true;
            Context.Fire(new MarketPlatformTokened()
            {
                AToken = aToken,
                IsPlatformTokened = true
            }); 
            
            if (State.PlatformTokenSupplyState[aToken] == null)
            {
                State.PlatformTokenSupplyState[aToken] = new PlatformTokenMarketState()
                {
                    Index = PlatformTokenInitialIndex,
                    Block = Context.CurrentHeight
                };
            }
            
            if (State.PlatformTokenBorrowState[aToken] == null) {
                State.PlatformTokenBorrowState[aToken] = new PlatformTokenMarketState()
                {
                    Index = PlatformTokenInitialIndex,
                    Block = Context.CurrentHeight
                };
            }
        }

      
        
        private void AssertContractInitialized()
        {
            Assert(State.Admin.Value != null, "Contract not initialized.");
        }

        private void AssertSenderIsAdmin()
        {
            AssertContractInitialized();
            Assert(Context.Sender == State.Admin.Value, "No permission.");
        }
    }
}