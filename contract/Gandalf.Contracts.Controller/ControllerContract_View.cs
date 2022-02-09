using System;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;


namespace Gandalf.Contracts.Controller
{
    public partial class ControllerContract
    {
        public override GetHypotheticalAccountLiquidityOutput GetHypotheticalAccountLiquidity(GetHypotheticalAccountLiquidityInput input)
        {
            var shortfall= GetHypotheticalAccountLiquidityInternal(input.Account, input.GTokenModify, input.RedeemTokens,
                input.BorrowAmount);
            
            return new GetHypotheticalAccountLiquidityOutput()
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
                (State.Markets[input.GToken].AccountMembership
                    .TryGetValue(input.Address.ToString(), out var isExist) && isExist);
            return new BoolValue()
            {
                Value = isMembership
            };
        }

        public override GTokens GetAllMarkets(Empty input)
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
                Value = State.BorrowCaps[input].Value
            };
        }
        
    }
}