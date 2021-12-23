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
        
        private void RedeemAllowedInternal(Address gToken, Address redeemer, uint redeemTokens)
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
    }
}