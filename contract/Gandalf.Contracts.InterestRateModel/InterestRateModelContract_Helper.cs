using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.InterestRateModel
{
    public partial class InterestRateModelContract
    {
        private Empty UpdateJumpRateModelInputInternal(long baseRatePerYear, long multiplierPerYear, long jumpMultiplierPerYear, long kink)
        {
            var newBaseRatePerBlock =  baseRatePerYear.Div(BlocksPerYear);
            var newMultiplierPerBlock = Convert.ToInt64(new BigIntValue(multiplierPerYear).Mul(Mantissa).Div(new BigIntValue(BlocksPerYear).Mul(kink)).Value) ;
            var newJumpMultiplierPerBlock = jumpMultiplierPerYear.Div(BlocksPerYear);

            State.BaseRatePerBlock.Value = newBaseRatePerBlock;
            State.JumpMultiplierPerBlock.Value = newJumpMultiplierPerBlock;
            State.MultiplierPerBlock.Value = newMultiplierPerBlock;
            State.Kink.Value = kink;
            
            Context.Fire(new NewInterestParams()
            {
                BaseRatePerYear = newBaseRatePerBlock,
                MultiplierPerYear = newMultiplierPerBlock,
                JumpMultiplierPerYear = newJumpMultiplierPerBlock,
                Kink = kink
            });
            return new Empty();
        }

        private static Int64Value GetUtilizationRateInternal(long cash, long borrows, long reserves)
        {
            if (borrows == 0)
            {
                return new Int64Value()
                {
                    Value = 0
                };
            }

            var utilizationRate = new BigIntValue(borrows).Mul(Mantissa)
                .Div(cash.Add(borrows).Sub(reserves));
            return new Int64Value()
            {
                Value = Convert.ToInt64(utilizationRate.Value)
            };
        }

        private Int64Value GetBorrowRateInternal(long cash, long borrows, long reserves)
        {
            var util = GetUtilizationRateInternal(cash, borrows, reserves);
             
            if (util.Value <= State.Kink.Value)
            {
            
                return new Int64Value()
                {
                    Value = Convert.ToInt64(new BigIntValue(State.Kink.Value).Mul(State.MultiplierPerBlock.Value).Div(Mantissa)
                        .Add(State.BaseRatePerBlock.Value).Value) 
                };
            }
            else
            {
                var normalRate = new BigIntValue(State.Kink.Value).Mul(State.MultiplierPerBlock.Value).Div(Mantissa)
                    .Add(State.BaseRatePerBlock.Value);
                var excessUtil = new BigIntValue(util.Value.Sub(State.Kink.Value));
               
                return new Int64Value()
                {
                    Value = Convert.ToInt64(excessUtil.Mul(State.JumpMultiplierPerBlock.Value).Div(Mantissa)
                        .Add(normalRate).Value)
                };
            }
        }
    }
}