using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.InterestRateModel
{
    public partial class InterestRateModelContract
    {
        private Empty UpdateJumpRateModelInputInternal(long baseRatePerYear, long multiplierPerYear, long jumpMultiplierPerYear, long kink)
        {
            var newBaseRatePerBlock =  baseRatePerYear.Div(BlocksPerYear);
            var newMultiplierPerBlockStr =  new BigIntValue(multiplierPerYear).Mul(Mantissa).Div(new BigIntValue(BlocksPerYear).Mul(kink)).Value ;
            var newJumpMultiplierPerBlock = jumpMultiplierPerYear.Div(BlocksPerYear);
            
            if (!long.TryParse(newMultiplierPerBlockStr, out var newMultiplierPerBlock))
            {
                throw new AssertionException($"Failed to parse {newMultiplierPerBlockStr}");
            }
            State.BaseRatePerBlock.Value = newBaseRatePerBlock;
            State.MultiplierPerBlock.Value = newMultiplierPerBlock;
            State.JumpMultiplierPerBlock.Value = newJumpMultiplierPerBlock;
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

            var utilizationRateStr = new BigIntValue(borrows).Mul(Mantissa)
                .Div(cash.Add(borrows).Sub(reserves)).Value;
            if (!long.TryParse(utilizationRateStr, out var utilizationRate))
            {
                throw new AssertionException($"Failed to parse {utilizationRateStr}");
            }
            return new Int64Value()
            {
                Value = utilizationRate
            };
        }

        private Int64Value GetBorrowRateInternal(long cash, long borrows, long reserves)
        {
            var util = GetUtilizationRateInternal(cash, borrows, reserves);
             
            if (util.Value <= State.Kink.Value)
            {
                var borrowRateStr = new BigIntValue(State.Kink.Value).Mul(State.MultiplierPerBlock.Value).Div(Mantissa)
                    .Add(State.BaseRatePerBlock.Value).Value;
                if (!long.TryParse(borrowRateStr, out var borrowRate))
                {
                    throw new AssertionException($"Failed to parse {borrowRateStr}");
                }
                return new Int64Value()
                {
                    Value = borrowRate
                };
            }
            else
            {
                var normalRate = new BigIntValue(State.Kink.Value).Mul(State.MultiplierPerBlock.Value).Div(Mantissa)
                    .Add(State.BaseRatePerBlock.Value);
                var excessUtil = new BigIntValue(util.Value.Sub(State.Kink.Value));
                var borrowRateStr = excessUtil.Mul(State.JumpMultiplierPerBlock.Value).Div(Mantissa)
                    .Add(normalRate).Value;
                if (!long.TryParse(borrowRateStr, out var borrowRate))
                {
                    throw new AssertionException($"Failed to parse {borrowRateStr}");
                }
                return new Int64Value()
                {
                    Value = borrowRate
                };
            }
        }
    }
}