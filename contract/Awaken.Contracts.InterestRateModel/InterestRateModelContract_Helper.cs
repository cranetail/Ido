using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.InterestRateModel
{
    public partial class InterestRateModelContract
    {
        private void UpdateWhitePaperInterestRateModel(long baseRatePerYear, long multiplierPerYear)
        {
            var newBaseRatePerBlock = baseRatePerYear.Div(BlocksPerYear);
            var newMultiplierPerBlock =  multiplierPerYear.Div(BlocksPerYear);
            
            State.BaseRatePerBlock.Value = newBaseRatePerBlock;
            State.MultiplierPerBlock.Value = newMultiplierPerBlock;
            Context.Fire(new NewInterestParams()
            {
                BaseRatePerBlock = newBaseRatePerBlock,
                MultiplierPerBlock = newMultiplierPerBlock
            });
        }
        private void UpdateJumpRateModelInputInternal(long baseRatePerYear, long multiplierPerYear, long jumpMultiplierPerYear, long kink)
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
                BaseRatePerBlock = newBaseRatePerBlock,
                MultiplierPerBlock = newMultiplierPerBlock,
                JumpMultiplierPerBlock = newJumpMultiplierPerBlock,
                Kink = kink
            });
        }

        private static long GetUtilizationRateInternal(long cash, long borrows, long reserves)
        {
            if (borrows == 0)
            {
                return 0;
            }

            var utilizationRateStr = new BigIntValue(borrows).Mul(Mantissa)
                .Div(cash.Add(borrows).Sub(reserves)).Value;
            if (!long.TryParse(utilizationRateStr, out var utilizationRate))
            {
                throw new AssertionException($"Failed to parse {utilizationRateStr}");
            }

            return utilizationRate;
        }

        private long GetBorrowRateInternal(long cash, long borrows, long reserves)
        {
            var util = GetUtilizationRateInternal(cash, borrows, reserves);
            string borrowRateStr;
            if (State.InterestRateModelType.Value)
            {
                 borrowRateStr = new BigIntValue(util).Mul(State.MultiplierPerBlock.Value).Div(Mantissa)
                    .Add(State.BaseRatePerBlock.Value).Value;
            }
            else
            {
                if (util <= State.Kink.Value)
                {
                     borrowRateStr = new BigIntValue(State.Kink.Value).Mul(State.MultiplierPerBlock.Value).Div(Mantissa)
                        .Add(State.BaseRatePerBlock.Value).Value;
                }
                else
                {
                    var normalRate = new BigIntValue(State.Kink.Value).Mul(State.MultiplierPerBlock.Value).Div(Mantissa)
                        .Add(State.BaseRatePerBlock.Value);
                    var excessUtil = new BigIntValue(util.Sub(State.Kink.Value)); 
                    borrowRateStr = excessUtil.Mul(State.JumpMultiplierPerBlock.Value).Div(Mantissa)
                        .Add(normalRate).Value;
                }
            }
            if (!long.TryParse(borrowRateStr, out var borrowRate))
            {
                throw new AssertionException($"Failed to parse {borrowRateStr}");
            }

            return borrowRate;

        }
    }
}