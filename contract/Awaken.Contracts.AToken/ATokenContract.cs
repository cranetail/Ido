using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Sdk.CSharp;

namespace Awaken.Contracts.AToken
{
    public partial class ATokenContract: ATokenContractContainer.ATokenContractBase
    {
        public override Empty Initialize(Empty input)
        {
            Assert(State.Admin.Value != null, "Initialized");
            State.Admin.Value = Context.Sender;
            return new Empty();
        }

        public override Empty AccrueInterest(Address aToken)
        {
            /* Remember the initial block number */
            var currentBlockNumber = Context.CurrentHeight;
            var accrualBlockNumberPrior = State.AccrualBlockNumbers[aToken];
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
            var cashPrior = GetCashPrior(aToken);
            var borrowPrior = State.TotalBorrows[aToken];
            var reservesPrior = State.TotalReserves[aToken];
            var borrowIndexPrior = State.BorrowIndex[aToken];
            var supplyRate = GetSupplyRatePerBlockInternal(aToken);
            var borrowRate = GetBorrowRatePerBlockInternal(aToken);
            Assert(borrowRate <= MaxBorrowRate, "BorrowRate is higher than MaxBorrowRate");
            //Calculate the number of blocks elapsed since the last accrual 
            var blockDelta = Context.CurrentHeight.Sub(State.AccrualBlockNumbers[aToken]);
            var simpleInterestFactor = borrowRate * blockDelta;
            var interestAccumulated = simpleInterestFactor * borrowPrior;
            var totalBorrowsNew = interestAccumulated + borrowPrior;
            var totalReservesNew = State.ReserveFactor[aToken] * interestAccumulated +
                                   reservesPrior;
            var borrowIndexNew = simpleInterestFactor * borrowIndexPrior + borrowIndexPrior;
            State.AccrualBlockNumbers[aToken] = currentBlockNumber;
            State.BorrowIndex[aToken] = borrowIndexNew;
            State.TotalBorrows[aToken] = totalBorrowsNew;
            State.TotalReserves[aToken] = totalReservesNew;
            Context.Fire(new AccrueInterest()
            {
                Symbol = aToken,
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
            MintInternal(input.AToken, input.MintAmount, input.Channel);
            return new Empty();
        }

        public override Empty Borrow(BorrowInput input)
        {
            BorrowInternal(input.AToken, input.Amount, input.Channel);
            return base.Borrow(input);
        }

        public override Empty Redeem(RedeemInput input)
        {
            RedeemInternal(input.AToken, input.Amount);
            return new Empty();
        }

        public override Empty RedeemUnderlying(RedeemUnderlyingInput input)
        {
            RedeemUnderlyingInternal(input.AToken,input.Amount);
            return new Empty();
        }
        public override Empty Seize(SeizeInput input)
        {
            SeizeInternal(input.CollateralToken,input.SeizerToken,input.Liquidator,input.Borrower,input.SeizeTokens);
            return new Empty();
        }

        public override Empty LiquidateBorrow(LiquidateBorrowInput input)
        {
            LiquidateBorrowInternal(input.CollateralSymbol, input.BorrowToken, input.Borrower, input.RepayAmount);
            return new Empty();
        }

        public override Empty RepayBorrow(RepayBorrowInput input)
        {
            RepayBorrowInternal(input.Amount,input.AToken);
            return new Empty();
        }

        public override Empty AddReserves(AddReservesInput input)
        {
            
            return base.AddReserves(input);
        }

        //Set Fuction
        public override Empty SetAdmin(Address input)
        {
            State.Admin.Value = input;
            return new Empty();
        }

        public override Empty SetComptroller(Address input)
        {
            State.ControllerContract.Value = input;
            return new Empty();
        }

        public override Empty SetReserveFactor(SetReserveFactorInput input)
        {
            State.ReserveFactor[input.AToken] = input.ReserveFactor;
            return new Empty();
        }

        public override Empty SetInterestRateModel(SetInterestRateModelInput input)
        {
            State.InterestRateModelContracts[input.AToken].Value = input.Model;
            return new Empty();
        }
    }
}