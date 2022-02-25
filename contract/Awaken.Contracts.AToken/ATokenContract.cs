﻿using AElf;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Sdk.CSharp;
using Awaken.Contracts.InterestRateModel;

namespace Awaken.Contracts.AToken
{
    public partial class ATokenContract: ATokenContractContainer.ATokenContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.TokenContract.Value == null, "Already initialized.");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.ControllerContract.Value = input.Controller;
            State.Admin.Value = Context.Sender;
            return new Empty();
        }

        
        public override Empty Create(CreateInput input)
        {
            AssertSenderIsAdmin();
            var symbolString = GetATokenSymbol(input.UnderlyingSymbol);
            var symbolHash = HashHelper.ComputeFrom(symbolString);
            var symbolVirtualAddress = Context.ConvertVirtualAddressToContractAddress(symbolHash);
            
            State.ATokenVirtualAddressMap[symbolString] = symbolVirtualAddress;
            State.UnderlyingMap[symbolVirtualAddress] = input.UnderlyingSymbol;
            State.UnderlyingToTokenSymbolMap[input.UnderlyingSymbol] = symbolString;
            State.TokenSymbolMap[symbolVirtualAddress] = symbolString;
            State.InterestRateModelContractsAddress[symbolVirtualAddress] = input.InterestRateModel;
            State.InitialExchangeRate[symbolVirtualAddress] = input.InitialExchangeRate;
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
            var simpleInterestFactor = new BigIntValue(borrowRate).Mul(blockDelta);
            var interestAccumulated = simpleInterestFactor.Mul(borrowPrior).Div(Mantissa);
            
            var totalBorrowsNewStr = interestAccumulated.Add(borrowPrior).Value;
            var totalReservesNewStr =
                interestAccumulated.Mul(State.ReserveFactor[aToken]).Div(Mantissa).Add(reservesPrior).Value;
            var borrowIndexNewStr = simpleInterestFactor.Mul(borrowIndexPrior).Div(Mantissa).Add(borrowIndexPrior).Value;
            
            if (!long.TryParse(totalBorrowsNewStr, out var totalBorrowsNew))
            {
                throw new AssertionException($"Failed to parse {totalBorrowsNewStr}");
            }
            if (!long.TryParse(totalReservesNewStr, out var totalReservesNew))
            {
                throw new AssertionException($"Failed to parse {totalReservesNewStr}");
            }
            if (!long.TryParse(borrowIndexNewStr, out var borrowIndexNew))
            {
                throw new AssertionException($"Failed to parse {borrowIndexNewStr}");
            }
            if (!long.TryParse(interestAccumulated.Value, out var interestAccumulatedInt64))
            {
                throw new AssertionException($"Failed to parse {interestAccumulated.Value}");
            }
            State.AccrualBlockNumbers[aToken] = currentBlockNumber;
            State.TotalBorrows[aToken] = totalBorrowsNew;
            State.TotalReserves[aToken] = totalReservesNew;
            State.BorrowIndex[aToken] = borrowIndexNew;
            
            Context.Fire(new AccrueInterest()
            {
                Symbol = aToken,
                Cash = cashPrior,
                InterestAccumulated = interestAccumulatedInt64,
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
            return new Empty();
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
            State.InterestRateModelContractsAddress[input.AToken] = input.Model;
            return new Empty();
        }
    }
}