using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.InterestRateModel
{
    public partial class InterestRateModelContract
    {
        public override Int64Value GetUtilizationRate(GetUtilizationRateInput input)
        {
            var util = GetUtilizationRateInternal(input.Cash,input.Borrows,input.Reserves);
            return new Int64Value()
            {
                Value = util
            };
        }

        public override Int64Value GetBorrowRate(GetBorrowRateInput input)
        {
            var borrowRate = GetBorrowRateInternal(input.Cash,input.Borrows,input.Reserves);
            return new Int64Value()
            {
                Value = borrowRate
            };
        }

        public override Int64Value GetSupplyRate(GetSupplyRateInput input)
        {
            var oneMinusReserveFactor = Mantissa.Sub(input.ReserveFactor);
            var borrowRate = GetBorrowRateInternal(input.Cash, input.Borrows, input.Reserves);
            var rateToPool = new BigIntValue(borrowRate).Mul(oneMinusReserveFactor).Div(Mantissa);
            var util = GetUtilizationRateInternal(input.Cash, input.Borrows, input.Reserves);
            var supplyRateStr = new BigIntValue(util).Mul(rateToPool).Div(Mantissa).Value;
            if (!long.TryParse(supplyRateStr, out var supplyRate))
            {
                throw new AssertionException($"Failed to parse {supplyRateStr}");
            }
            return new Int64Value()
            {
                Value = supplyRate
            };

        }

        public override Int64Value GetKink(Empty input)
        {
            return new Int64Value()
            {
                Value = State.Kink.Value
            };
        }

        public override Int64Value GetMultiplierPerBlock(Empty input)
        {
            return new Int64Value()
            {
                Value = State.MultiplierPerBlock.Value
            };
        }

        public override Int64Value GetBaseRatePerBlock(Empty input)
        {
            return new Int64Value()
            {
                Value = State.BaseRatePerBlock.Value
            };
        }

        public override Int64Value GetJumpMultiplierPerBlock(Empty input)
        {
            return new Int64Value()
            {
                Value = State.JumpMultiplierPerBlock.Value
            };
        }

        public override Address GetOwner(Empty input)
        {
            return State.Owner.Value;
        }
    }
}