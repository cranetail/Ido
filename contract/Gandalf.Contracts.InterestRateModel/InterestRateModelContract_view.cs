using System;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.InterestRateModel
{
    public partial class InterestRateModelContract
    {
        public override Int64Value GetUtilizationRate(GetUtilizationRateInput input)
        {
            return GetUtilizationRateInternal(input.Cash,input.Borrows,input.Reserves);
        }

        public override Int64Value GetBorrowRate(GetBorrowRateInput input)
        {
            return GetBorrowRateInternal(input.Cash,input.Borrows,input.Reserves);
        }

        public override Int64Value GetSupplyRate(GetSupplyRateInput input)
        {
            var oneMinusReserveFactor = Mantissa.Sub(input.ReserveFactor);
            var borrowRate = GetBorrowRateInternal(input.Cash, input.Borrows, input.Reserves).Value;
            var rateToPool = new BigIntValue(borrowRate).Mul(oneMinusReserveFactor).Div(Mantissa);
            var util = GetUtilizationRateInternal(input.Cash, input.Borrows, input.Reserves).Value;
            return new Int64Value()
            {
                Value = Convert.ToInt64(new BigIntValue(util).Mul(rateToPool).Div(Mantissa).Value)
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
    }
}