using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Awaken.Contracts.Controller;
using Awaken.Contracts.InterestRateModel;

namespace Awaken.Contracts.AToken
{
    public partial class ATokenContract
    {
        private void MintInternal(Address aToken, long amount, string channel)
        {
            AccrueInterest(aToken);
            MintFresh(aToken, amount, channel);
        }

        private void MintFresh(Address aToken, long amount, string channel)
        {
            State.ControllerContract.MintAllowed.Send(new MintAllowedInput()
            {
                AToken = aToken,
                MintAmount = amount,
                Minter = Context.Self
            });
            Assert(State.AccrualBlockNumbers[aToken] == Context.CurrentHeight,"Market's block number should equals current block number");
            var exchangeRate = ExchangeRateStoredInternal(aToken);
            DoTransferIn(Context.Sender, amount, State.Underlying[aToken]);
            //  mintTokens = actualMintAmount / exchangeRate
            var mintTokens =amount.Div(exchangeRate) ;
            // totalSupplyNew = totalSupply + mintTokens
            var totalSupplyNew = State.TotalSupply[aToken].Add(mintTokens);
            //accountTokensNew = accountTokens[minter] + mintTokens
            var accountTokensNew = State.AccountTokens[aToken][Context.Sender].Add(mintTokens);
            State.TotalSupply[aToken] = totalSupplyNew;
            State.AccountTokens[aToken][Context.Sender] = accountTokensNew;
            Context.Fire(new Mint()
            {
                Address = Context.Sender,
                Amount = amount,
                CTokenAmount = mintTokens,
                Symbol = aToken
            });
        }

      
        private long GetCashPrior(Address aToken)
        {
             
            var result = State.TokenContract.GetBalance.Call(new GetBalanceInput()
            {
                Owner = Context.Self,
                Symbol = State.Underlying[aToken]
            });
            return result.Balance;
        }

        private long GetSupplyRatePerBlockInternal(Address aToken)
        {
            var totalCash = GetCashPrior(aToken);
            var totalBorrow = State.TotalBorrows[aToken];
            var totalReserves = State.TotalReserves[aToken];
            return State.InterestRateModelContracts[aToken].GetSupplyRate.Call(new GetSupplyRateInput()
            {
                Cash = totalCash,
                Borrows = totalBorrow,
                Reserves = totalReserves,
                ReserveFactor = State.ReserveFactor[aToken]
            }).Value;
        }

        private long GetBorrowRatePerBlockInternal(Address aToken)
        {
            var totalCash = GetCashPrior(aToken);
            var totalBorrow = State.TotalBorrows[aToken];
            var totalReserves = State.TotalReserves[aToken];
            return State.InterestRateModelContracts[aToken].GetBorrowRate.Call(new GetBorrowRateInput()
            {
                Cash = totalCash,
                Borrows = totalBorrow,
                Reserves = totalReserves
            }).Value;
        }

        private long ExchangeRateStoredInternal(Address aToken)
        {
            var totalSupply = State.TotalSupply[aToken];
            var totalCash = GetCashPrior(aToken);
            var totalBorrow = State.TotalBorrows[aToken];
            var totalReserves = State.TotalReserves[aToken];
            if (totalSupply == 0)
            {
                return State.InitialExchangeRate[aToken];
            }

            // exchangeRate = (totalCash + totalBorrows - totalReserves) / totalSupply
            var exchangeRate = totalCash.Add(totalBorrow).Sub(totalReserves).Div(totalSupply);
            return exchangeRate;
        }
        
        private void DoTransferIn(Address from, long amount, string symbol)
        {
            var input = new TransferFromInput()
            {
                Amount = amount,
                From = from,
                Memo = "TransferIn",
                Symbol = symbol,
                To = Context.Self
            };
            State.TokenContract.TransferFrom.Send(input);
        }

        private void DoTransferOut(Address to, long amount, string symbol)
        {
            var input = new TransferInput()
            {
                Amount = amount,
                Memo = "TransferOut",
                Symbol = symbol,
                To = to
            };
            State.TokenContract.Transfer.Send(input);
        }
        
        private void BorrowInternal(Address aToken, long amount, string channel)
        {
            AccrueInterest(aToken);
            BorrowFresh(Context.Sender, amount, aToken, channel);
        }

        private void BorrowFresh(Address borrower, long borrowAmount, Address aToken,  string channel)
        {
            State.ControllerContract.BorrowAllowed.Send(new BorrowAllowedInput()
            {
                BorrowAmount = borrowAmount,
                Borrower = borrower,
                AToken = aToken
            });
            Assert(State.AccrualBlockNumbers[aToken] == Context.CurrentHeight,"Market's block number should equals current block number");
            Assert(GetCashPrior(aToken) >= borrowAmount, "Borrow cash not available");
        }

        private BigIntValue BorrowBalanceStoredInternal(Account input)
        {
            BorrowSnapshot borrowSnapshot = State.AccountBorrows[input.A][input.User];
            if (borrowSnapshot == null)
            {
                return 0;
            }

            if (borrowSnapshot.Principal == 0)
            {
                return 0;
            }

            //Calculate new borrow balance using the interest index:
            //recentBorrowBalance = borrower.borrowBalance * market.borrowIndex / borrower.borrowIndex
            var borrowIndex = State.BorrowIndex[input.AToken];
            if (borrowSnapshot.InterestIndex == 0)
            {
                return 0;
            }

            var result = new BigIntValue(borrowIndex).Mul(borrowSnapshot.Principal).Div(borrowSnapshot.InterestIndex);
            return result;
        }

        private void RedeemInternal(Address aToken, long redeemTokens)
        {
            AccrueInterest(aToken);
            RedeemFresh(aToken, Context.Sender, redeemTokens, 0);
        }

        private void RedeemUnderlyingInternal(Address aToken, long redeemAmount)
        {
            AccrueInterest(aToken);
            RedeemFresh(aToken, Context.Sender, 0, redeemAmount);
        }
        private void RedeemFresh(Address aToken, Address redeemer, long redeemTokensIn, long redeemAmountIn)
        {
            Assert(redeemTokensIn == 0 || redeemAmountIn == 0, "one of redeemTokensIn or redeemAmountIn must be zero");
            var exchangeRate = ExchangeRateStoredInternal(aToken);
            long redeemTokens;
            long redeemAmount;
            if (redeemTokensIn > 0)
            {
                redeemTokens = redeemTokensIn;
                redeemAmount = Convert.ToInt64(new BigIntValue(exchangeRate).Mul(redeemTokensIn).Div(Mantissa).ToString()) ;
            }
            else
            {
                redeemTokens = Convert.ToInt64(new BigIntValue(redeemAmountIn).Mul(Mantissa).Div(exchangeRate).ToString()) ;
                redeemAmount = redeemAmountIn;
            }
            State.ControllerContract.RedeemAllowed.Send(new RedeemAllowedInput()
            {
                AToken = aToken,
                Redeemer = redeemer,
                RedeemTokens = redeemTokens
            });
            var accrualBlockNumberPrior = State.AccrualBlockNumbers[aToken];
            Assert(accrualBlockNumberPrior == Context.CurrentHeight,
                "market's block number should equals current block number");
            //totalSupplyNew = totalSupply - redeemTokens
            //accountTokensNew = accountTokensRedeemFresh[redeemer] - redeemTokens
            //to do:send burn to token contract to burn Atoken
            var totalSupplyNew = State.TotalSupply[aToken].Sub(redeemTokens);
            var accountTokensNew = State.AccountTokens[aToken][Context.Sender].Sub(redeemTokens);
            
            Assert(GetCashPrior(aToken) >= redeemAmount, "Insufficient Token Cash");
            Assert(accountTokensNew >= 0, "Insufficient Token Balance");
            var underlying = State.Underlying[aToken];
            DoTransferOut(aToken, redeemAmount, underlying );
            
            //We write previously calculated values into storage
            State.TotalSupply[aToken] = totalSupplyNew;
            State.AccountTokens[aToken][redeemer] = accountTokensNew;
            
            State.ControllerContract.RedeemVerify.Send(new RedeemVerifyInput()
            {
                AToken = aToken,
                Minter = redeemer,
                RedeemAmount = redeemAmount,
                RedeemTokens = redeemTokens
            });
            
            Context.Fire(new Redeem()
            {
                Address = redeemer,
                Amount = redeemAmount,
                CTokenAmount = redeemTokens,
                Symbol = aToken
            });
        }

        private void SeizeInternal(Address collateralToken, Address seizerToken, Address liquidator, Address borrower, long seizeTokens)
        {
            State.ControllerContract.SeizeAllowed.Send(new SeizeAllowedInput()
            {
                ATokenBorrowed = collateralToken,
                ATokenCollateral = seizerToken,
                Borrower = borrower,
                Liquidator = liquidator,
                SeizeTokens = seizeTokens
            });
            Assert(borrower != liquidator, "Liquidator is borrower");
            var borrowerTokensNew = State.AccountTokens[collateralToken][borrower].Sub(seizeTokens);
            var liquidatorTokensNew = State.AccountTokens[collateralToken][liquidator].Add(seizeTokens);
            State.AccountTokens[collateralToken][borrower] = borrowerTokensNew;
            State.AccountTokens[collateralToken][liquidator] = liquidatorTokensNew;
            // to do : transfer atoken
            State.ControllerContract.SeizeVerify.Send(new SeizeVerifyInput()
            {
                Borrower = borrower,
                ATokenBorrowed = collateralToken,
                ATokenCollateral = seizerToken,
                Liquidator = liquidator,
                SeizeTokens = seizeTokens
            });
        }

        private void LiquidateBorrowInternal(Address collateralToken, Address borrowToken, Address borrower,
            long repayAmount)
        {
            AccrueInterest(collateralToken);
            AccrueInterest(borrowToken);
            LiquidateBorrowFresh(collateralToken,borrowToken,Context.Sender,borrower,repayAmount);
        }

        private void LiquidateBorrowFresh(Address collateralToken, Address borrowToken,Address liquidator, Address borrower,
            long repayAmount)
        {
            State.ControllerContract.LiquidateBorrowAllowed.Send(new LiquidateBorrowAllowedInput()
            {
                ATokenBorrowed = borrowToken,
                ATokenCollateral = collateralToken,
                Borrower = borrower,
                Liquidator = liquidator,
                RepayAmount = repayAmount
            });
            var accrualBorrowSymbolBlockNumberPrior = State.AccrualBlockNumbers[borrowToken];
            Assert(accrualBorrowSymbolBlockNumberPrior == Context.CurrentHeight,
                "Market's block number should equals current block number");
            var accrualCollateralSymbolBlockNumberPrior = State.AccrualBlockNumbers[collateralToken];
            Assert(accrualCollateralSymbolBlockNumberPrior == Context.CurrentHeight,
                "Market's block number should equals current block number");
            Assert(borrower != Context.Sender, "Liquidator is borrower");
            Assert(repayAmount > 0 && repayAmount < long.MaxValue, "Invalid close amount request");
            var actualRepayAmount = RepayBorrowFresh(liquidator, borrower, repayAmount, borrowToken);
            var seizeTokens = State.ControllerContract.LiquidateCalculateSeizeTokens.Call(new LiquidateCalculateSeizeTokensInput()
            {
                ActualRepayAmount = actualRepayAmount,
                ATokenBorrowed = borrowToken,
                ATokenCollateral = collateralToken
            }).Value;
            SeizeInternal(collateralToken, borrowToken, liquidator, borrower, seizeTokens);
            
            Context.Fire(new LiquidateBorrow
            {
                Borrower = borrower,
                Liquidator = liquidator,
                RepayAmount = actualRepayAmount,
                RepaySymbol = borrowToken,
                SeizeSymbol = collateralToken,
                SeizeTokenAmount = seizeTokens
            });
            State.ControllerContract.LiquidateBorrowVerify.Send(new LiquidateBorrowVerifyInput()
            {
                ActualRepayAmount = actualRepayAmount,
                Borrower = borrower,
                ATokenBorrowed = borrowToken,
                ATokenCollateral = collateralToken,
                Liquidator = liquidator,
                SeizeTokens = seizeTokens
            });
        }

        private void RepayBorrowInternal(long repayAmount, Address aToken)
        {
            AccrueInterest(aToken);
            RepayBorrowFresh(Context.Sender, Context.Sender, repayAmount, aToken);
        }

        private long RepayBorrowFresh(Address payer, Address borrower, long repayAmount, Address aToken)
        {
            State.ControllerContract.RepayBorrowAllowed.Send(new RepayBorrowAllowedInput()
            {
                AToken = aToken,
                Payer = payer,
                Borrower = borrower,
                RepayAmount = repayAmount
            });
            var accrualBlockNumberPrior = State.AccrualBlockNumbers[aToken];
            Assert(accrualBlockNumberPrior == Context.CurrentHeight,
                "Market's block number should equals current block number");
            // var borrowerIndex = State.AccountBorrows[symbol][borrower].InterestIndex;
            var account = new Account()
            {
                User = borrower,
                AToken = aToken
            };
            var accountBorrows = Convert.ToInt64(BorrowBalanceStoredInternal(account).Value);
            if (repayAmount == long.MaxValue)
            {
                repayAmount =accountBorrows;
            }
            var underling = State.Underlying[aToken];
            DoTransferIn(payer, repayAmount, underling);
            
            var actualRepayAmount = repayAmount;
            //accountBorrowsNew = accountBorrows - actualRepayAmount
            // totalBorrowsNew = totalBorrows - actualRepayAmount
            var accountBorrowsNew = accountBorrows.Sub(actualRepayAmount);
            Assert(accountBorrowsNew >= 0, "Insufficient Balance Of Token");
            var totalBorrowsNew = State.TotalBorrows[aToken].Sub(actualRepayAmount);
            
            State.AccountBorrows[aToken][borrower].Principal = accountBorrowsNew;
            State.AccountBorrows[aToken][borrower].InterestIndex = State.BorrowIndex[aToken];
            State.TotalBorrows[aToken] = totalBorrowsNew;
            Context.Fire(new RepayBorrow()
            {
                Amount = actualRepayAmount,
                Borrower = borrower,
                BorrowBalance = accountBorrowsNew,
                Payer = payer,
                Symbol = aToken,
                TotalBorrows = totalBorrowsNew
            });
            State.ControllerContract.RepayBorrowVerify.Send(new RepayBorrowVerifyInput()
            {
                AToken = aToken,
                Borrower = borrower,
                Payer = payer,
                BorrowerIndex = State.BorrowIndex[aToken],
                RepayAmount = actualRepayAmount
            });
            return actualRepayAmount;
        }
    }
}