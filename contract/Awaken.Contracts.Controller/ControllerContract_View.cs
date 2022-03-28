using System;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;


namespace Awaken.Contracts.Controller
{
    public partial class ControllerContract
    {
        public override GetHypotheticalAccountLiquidityOutput GetHypotheticalAccountLiquidity(GetHypotheticalAccountLiquidityInput input)
        {
            var shortfall= GetHypotheticalAccountLiquidityInternal(input.Account, input.ATokenModify, input.RedeemTokens,
                input.BorrowAmount);
            
            return new GetHypotheticalAccountLiquidityOutput()
            {
                Liquidity = shortfall < 0 ? 0 : shortfall,
                Shortfall = shortfall < 0 ? -shortfall : 0
            };
        }

        public override GetAccountLiquidityOutput GetAccountLiquidity(Address input)
        {
            var shortfall= GetHypotheticalAccountLiquidityInternal(input, new Address(), 0,
               0);
            
            return new GetAccountLiquidityOutput()
            {
                Liquidity = shortfall < 0 ? 0 : shortfall,
                Shortfall = shortfall < 0 ? -shortfall : 0
            };
        }

        public override Address GetAdmin(Empty input)
        {
            return State.Admin.Value;
        }

        public override BoolValue CheckMembership(Account input)
        {
            var isMembership =
                (State.Markets[input.AToken].AccountMembership
                    .TryGetValue(input.Address.ToString(), out var isExist) && isExist);
            return new BoolValue()
            {
                Value = isMembership
            };
        }

        public override ATokens GetAllMarkets(Empty input)
        {
            return State.AllMarkets.Value;
        }

        public override AssetList GetAssetsIn(Address input)
        {
            var assetList = State.AccountAssets[input];
            return assetList;
        }

        public override Int64Value GetCloseFactor(Empty input)
        {
            return new Int64Value()
            {
                Value = State.CloseFactor.Value
            };
        }

        public override Int64Value GetCollateralFactor(Address input)
        {
             return new Int64Value()
            {
                Value = State.Markets[input].CollateralFactor
            };
        }

        public override Int64Value GetLiquidationIncentive(Empty input)
        {
            return new Int64Value()
            {
                Value = State.LiquidationIncentive.Value
            };
        }

        public override Int32Value GetMaxAssets(Empty input)
        {
            return new Int32Value()
            {
                Value = State.MaxAssets.Value
            };
        }

        public override Address GetPendingAdmin(Empty input)
        {
            return State.PendingAdmin.Value;
        }

        public override Address GetBorrowCapGuardian(Empty input)
        {
            return State.BorrowCapGuardian.Value;
        }

        public override Int64Value GetMarketBorrowCaps(Address input)
        {
            return new Int64Value()
            {
                Value = State.BorrowCaps[input]
            };
        }

       

        public override Address GetPriceOracle(Empty input)
        {
            return State.PriceContract.Value;
        }

        public override Market GetMarket(Address input)
        {

            return State.Markets[input];
        }

        public override Int64Value GetPlatformTokenRate(Empty input)
        {
            return new Int64Value()
            {
                Value = State.PlatformTokenRate.Value
            };

        }

        public override Int64Value GetPlatformTokenSpeeds(Address input)
        {
            return new Int64Value()
            {
                Value = State.PlatformTokenSpeeds[input]
            };
        }

        public override PlatformTokenMarketState GetPlatformTokenSupplyState(Address input)
        {
            return State.PlatformTokenSupplyState[input];
        }

        public override PlatformTokenMarketState GetPlatformTokenBorrowState(Address input)
        {
            return State.PlatformTokenBorrowState[input];
        }

        public override BigIntValue GetPlatformTokenSupplierIndex(Account input)
        {
            return State.PlatformTokenSupplierIndex[input.AToken][input.Address];
        }

        public override BigIntValue GetPlatformTokenBorrowerIndex(Account input)
        {
            return State.PlatformTokenBorrowerIndex[input.AToken][input.Address];
        }

        public override Int64Value GetPlatformTokenAccrued(Address input)
        {
            return new Int64Value()
            {
                Value = State.PlatformTokenAccrued[input]
            }; 
                
        }

        public override Address GetPauseGuardian(Empty input)
        {
            return State.PauseGuardian.Value;
        }

        public override BoolValue GetBorrowGuardianPaused(Address input)
        {
            return new BoolValue()
            {
                Value = State.BorrowGuardianPaused[input]
            };
        }

        public override BoolValue GetMintGuardianPaused(Address input)
        {
            return new BoolValue()
            {
                Value = State.MintGuardianPaused[input]
            };
        }

        public override BoolValue GetSeizeGuardianPaused(Empty input)
        {
            return new BoolValue()
            {
                Value = State.SeizeGuardianPaused.Value
            };
        }

        public override BoolValue GetTransferGuardianPaused(Empty input)
        {
            return new BoolValue()
            {
                Value = State.TransferGuardianPaused.Value
            };
        }

        public override Int64Value GetPlatformTokenClaimThreshold(Empty input)
        {
            return new Int64Value()
            {
                Value = PlatformTokenClaimThreshold
            };
        }
    }
}