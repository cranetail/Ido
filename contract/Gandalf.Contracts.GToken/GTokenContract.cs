using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Sdk.CSharp;

namespace Gandalf.Contracts.GToken
{
    public partial class GTokenContract: GTokenContractContainer.GTokenContractBase
    {
        public override Empty Initialize(Empty input)
        {
            Assert(State.Admin.Value != null, "Initialized");
            State.Admin.Value = Context.Sender;
            return new Empty();
        }

        public override Empty AccrueInterest(Address gToken)
        {
            /* Remember the initial block number */
            var currentBlockNumber = Context.CurrentHeight;
            var accrualBlockNumberPrior = State.AccrualBlockNumbers[gToken];
            if (accrualBlockNumberPrior == currentBlockNumber)
            {
                return new Empty();
            }

            /*
               * Calculate the interest accumulated into borrows and reserves and the new index:
               *  simpleInterestFactor = borrowRate * blockDelta
               *  interestAccumulated = simpleInterestFactor * totalBorrows
               *  totalBorrowsNew = interestAccumulated + totalBorrows
               *  totalReservesNew = interestAccumulated * reserveFactor + totalReserves
               *  borrowIndexNew = simpleInterestFactor * borrowIndex + borrowIndex
               */
            var cashPrior = GetCashPrior(gToken);
            var borrowPrior = State.TotalBorrows[gToken];
            var reservesPrior = State.TotalReserves[gToken];
            var borrowIndexPrior = State.BorrowIndex[gToken];
            var supplyRate = GetSupplyRatePerBlockInternal(gToken);
            var borrowRate = GetBorrowRatePerBlockInternal(gToken);
            Assert(borrowRate <= MaxBorrowRate, "BorrowRate is higher than MaxBorrowRate");
            //Calculate the number of blocks elapsed since the last accrual 
            var blockDelta = Context.CurrentHeight.Sub(State.AccrualBlockNumbers[gToken]);
            var simpleInterestFactor = borrowRate * blockDelta;
            var interestAccumulated = simpleInterestFactor * borrowPrior;
            var totalBorrowsNew = interestAccumulated + borrowPrior;
            var totalReservesNew = State.ReserveFactor[gToken] * interestAccumulated +
                                   reservesPrior;
            var borrowIndexNew = simpleInterestFactor * borrowIndexPrior + borrowIndexPrior;
            State.AccrualBlockNumbers[gToken] = currentBlockNumber;
            State.BorrowIndex[gToken] = borrowIndexNew;
            State.TotalBorrows[gToken] = totalBorrowsNew;
            State.TotalReserves[gToken] = totalReservesNew;
            Context.Fire(new AccrueInterest()
            {
                Symbol = gToken,
                Cash = cashPrior,
                InterestAccumulated = decimal.ToInt64(interestAccumulated),
                BorrowIndex = borrowIndexNew,
                TotalBorrows = totalBorrowsNew,
                BorrowRatePerBlock = borrowRate,
                SupplyRatePerBlock = supplyRate
            });
            return new Empty();
        }

        public override Empty Mint(MintInput input)
        {
            MintInternal(input.GToken, input.MintAmount, input.Channel);
            return new Empty();
        }
    }
}