using System;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Price;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Awaken.Contracts.Controller;
using Awaken.Contracts.InterestRateModel;
using Google.Protobuf.WellKnownTypes;

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
            var underlying = State.UnderlyingMap[aToken];
            DoTransferIn(Context.Sender, amount, underlying);
            //  mintTokens = actualMintAmount / exchangeRate
            var mintTokensStr =  new BigIntValue(ExchangeMantissa).Mul(amount).Div(exchangeRate).Value;
            if (!long.TryParse(mintTokensStr, out var mintTokens))
            {
                throw new AssertionException($"Failed to parse {mintTokensStr}");
            }
            // totalSupplyNew = totalSupply + mintTokens
            var totalSupplyNew = State.TotalSupply[aToken].Add(mintTokens);
            //accountTokensNew = accountTokens[minter] + mintTokens
            var accountTokensNew = State.AccountTokens[aToken][Context.Sender].Add(mintTokens);
            State.TotalSupply[aToken] = totalSupplyNew;
            State.AccountTokens[aToken][Context.Sender] = accountTokensNew;
            Context.Fire(new Mint()
            {
                Sender = Context.Sender,
                Underlying = underlying,
                UnderlyingAmount = amount,
                AToken = aToken,
                ATokenAmount = mintTokens,
                Channel = channel
            });
        }

      
        private long GetCashPrior(Address aToken)
        {
            var symbol = State.UnderlyingMap[aToken];
            var result = State.TokenContract.GetBalance.Call(new GetBalanceInput()
            {
                Owner = Context.Self,
                Symbol = symbol
            });
            return result.Balance;
        }
        
        private long GetSupplyRatePerBlockInternal(Address aToken)
        {
            var totalCash = GetCashPrior(aToken);
            var totalBorrow = State.TotalBorrows[aToken];
            var totalReserves = State.TotalReserves[aToken];
            State.InterestRateModelContract.Value = State.InterestRateModelContractsAddress[aToken];
            return State.InterestRateModelContract.GetSupplyRate.Call(new GetSupplyRateInput()
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
            State.InterestRateModelContract.Value = State.InterestRateModelContractsAddress[aToken];
            return State.InterestRateModelContract.GetBorrowRate.Call(new GetBorrowRateInput()
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
            var exchangeRateStr = new BigIntValue(totalCash).Add(totalBorrow).Sub(totalReserves).Mul(ExchangeMantissa).Div(totalSupply).Value;
            if (!long.TryParse(exchangeRateStr, out var exchangeRate))
            {
                throw new AssertionException($"Failed to parse {exchangeRate}");
            }
            return exchangeRate;
        }
        
        private void DoTransferIn(Address from, long amount, string symbol)
        {
            var input = new AElf.Contracts.MultiToken.TransferFromInput()
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
            var input = new AElf.Contracts.MultiToken.TransferInput()
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
            var borrowBalance = BorrowBalanceStoredInternal(new Account() {AToken = aToken, User = borrower});
            State.AccountBorrows[aToken][borrower] = State.AccountBorrows[aToken][borrower] ?? new BorrowSnapshot();
            DoTransferOut(borrower, borrowAmount, State.UnderlyingMap[aToken]);
            State.AccountBorrows[aToken][borrower].Principal =
                borrowBalance.Add(borrowAmount);
            State.TotalBorrows[aToken] = State.TotalBorrows[aToken].Add(borrowAmount);
            State.AccountBorrows[aToken][borrower].InterestIndex = State.BorrowIndex[aToken];
            Context.Fire(new Borrow()
            {
                Borrower = borrower,
                Amount = borrowAmount,
                BorrowBalance =  State.AccountBorrows[aToken][borrower].Principal,
                AToken = aToken,
                TotalBorrows = State.TotalBorrows[aToken]
            });
            
            State.ControllerContract.BorrowVerify.Send(new BorrowVerifyInput()
            {
                AToken = aToken,
                BorrowAmount = borrowAmount,
                Borrower = borrower
            });
            
        }

        private long BorrowBalanceStoredInternal(Account input)
        {
            var borrowSnapshot = State.AccountBorrows[input.AToken][input.User];
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
            if (borrowSnapshot.InterestIndex == new BigIntValue(0))
            {
                return 0;
            }

            var principal = borrowSnapshot.Principal;
            var borrowBalanceStr = new BigIntValue(borrowIndex).Mul(principal).Div(borrowSnapshot.InterestIndex).Value;
            if (!long.TryParse(borrowBalanceStr, out var borrowBalance))
            {
                throw new AssertionException($"Failed to parse {borrowBalanceStr}");
            }
            
            return borrowBalance;
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
                var redeemAmountStr = new BigIntValue(exchangeRate).Mul(redeemTokensIn).Div(ExchangeMantissa).Value ;
                if (!long.TryParse(redeemAmountStr, out redeemAmount))
                {
                    throw new AssertionException($"Failed to parse {redeemAmountStr}");
                }
            }
            else
            {
                var redeemTokensStr = new BigIntValue(redeemAmountIn).Mul(ExchangeMantissa).Div(exchangeRate).Value ;
                redeemAmount = redeemAmountIn;
                if (!long.TryParse(redeemTokensStr, out redeemTokens))
                {
                    throw new AssertionException($"Failed to parse {redeemTokensStr}");
                }
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
            
            var totalSupplyNew = State.TotalSupply[aToken].Sub(redeemTokens);
            var accountTokensNew = State.AccountTokens[aToken][Context.Sender].Sub(redeemTokens);
            
            Assert(GetCashPrior(aToken) >= redeemAmount, "Insufficient Token Cash");
            Assert(accountTokensNew >= 0, "Insufficient Token Balance");
            var underlying = State.UnderlyingMap[aToken];
            DoTransferOut(redeemer, redeemAmount, underlying );
            
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
                Sender = redeemer,
                UnderlyingAmount = redeemAmount,
                Underlying = underlying,
                ATokenAmount = redeemTokens,
                AToken = aToken
            });
            Context.Fire(new Transferred()
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = redeemTokens,
                Symbol = State.TokenSymbolMap[aToken]
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
                RepayAToken = borrowToken,
                SeizeAToken = collateralToken,
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

        private void RepayBorrowBehalfInternal(Address borrower,long repayAmount, Address aToken)
        {
            AccrueInterest(aToken);
            RepayBorrowFresh(Context.Sender, borrower, repayAmount, aToken);
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
            var accountBorrows = BorrowBalanceStoredInternal(account);
            if (repayAmount == long.MaxValue)
            {
                repayAmount = accountBorrows;
            }

            var underling = State.UnderlyingMap[aToken];
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
                AToken = aToken,
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
        
        private void AssertContractInitialized()
        {
            Assert(State.Admin.Value != null, "Contract not initialized.");
        }

        private void AssertSenderIsAdmin()
        {
            AssertContractInitialized();
            Assert(Context.Sender == State.Admin.Value, "No permission.");
        }
        
        private void AssertSenderIsOwner()
        {
            Assert(Context.Sender == State.Owner.Value, "No permission.");
        }
        private string GetATokenSymbol(string symbol)
        {
            ValidTokenSymbol(symbol);
            return $"A-{symbol}";
        }
        
        private void ValidTokenSymbol(string token)
        {
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new AElf.Contracts.MultiToken.GetTokenInfoInput
            {
                Symbol = token
            });
            Assert(!string.IsNullOrEmpty(tokenInfo.Symbol), $"Token {token} not exists.");
        }
        
        private void ValidTokenExisting(string symbol)
        {
            var tokenInfo = State.ATokenVirtualAddressMap[symbol];
            if (tokenInfo == new Address())
            {
                throw new AssertionException($"Token {symbol} not found.");
            }
        }

        private void AddReservesInternal(Address aToken, long underlyingAddAmount)
        {
            var symbol = State.UnderlyingMap[aToken];
            AccrueInterest(aToken);
            var accrualBlockNumberPrior = State.AccrualBlockNumbers[aToken];
            Assert(accrualBlockNumberPrior == Context.CurrentHeight,
                "market's block number should equals current block number");
            DoTransferIn(Context.Sender,underlyingAddAmount,symbol);
            var totalReservesNew = State.TotalReserves[aToken].Add(underlyingAddAmount);
            State.TotalReserves[aToken] = totalReservesNew;
             Context.Fire(new ReservesAdded()
             {
                 Underlying = symbol,
                 AToken= aToken,
                 AddAmount = underlyingAddAmount,
                 Sender = Context.Sender,
                 TotalReserves = totalReservesNew
                 
             });
        }

        private void ReduceReservesInternal(Address aToken, long underlyingReduceAmount)
        {
            var symbol = State.UnderlyingMap[aToken];
            AccrueInterest(aToken);
            AssertSenderIsAdmin();
            var accrualBlockNumberPrior = State.AccrualBlockNumbers[aToken];
            Assert(accrualBlockNumberPrior == Context.CurrentHeight,
                "market's block number should equals current block number");
            Assert(GetCashPrior(aToken) >= underlyingReduceAmount, "reduce reserve cash not available");
            var totalReserve = State.TotalReserves[aToken];
            Assert(totalReserve >= underlyingReduceAmount);
            DoTransferOut(Context.Sender, underlyingReduceAmount, symbol);
            var totalReservesNew = totalReserve.Sub(underlyingReduceAmount);
            State.TotalReserves[aToken] = totalReservesNew;
            Context.Fire(new ReservesReduced()
            {
                Underlying = symbol,
                AToken= aToken,
                ReduceAmount = underlyingReduceAmount,
                Sender = Context.Sender,
                TotalReserves = totalReservesNew
            });
        }
        private void TransferTokens(Address spender, Address src, Address dst, string aTokenSymbol, long amount)
        {
            var aToken = State.ATokenVirtualAddressMap[aTokenSymbol];
            State.ControllerContract.TransferAllowed.Send(new TransferAllowedInput(){AToken = aToken,Src = src,Dst = dst,TransferTokens = amount});
            Assert(src != dst,"transfer not allowed");
            long startingAllowance = 0;
            if (spender == src)
            {
                startingAllowance = Int64.MaxValue;
            }
            else
            {
                startingAllowance = State.AllowanceMap[aToken][src][spender];
            }

            var allowanceNew = startingAllowance.Sub(amount);
            var srcTokensNew = State.AccountTokens[aToken][src].Sub(amount);
            var dstTokensNew = State.AccountTokens[aToken][dst].Add(amount);

            State.AccountTokens[aToken][src] = srcTokensNew;
            State.AccountTokens[aToken][dst] = dstTokensNew;

            if (startingAllowance != Int64.MaxValue)
            {
                State.AllowanceMap[aToken][src][spender] = allowanceNew;
            }
            
            Context.Fire(new Transferred()
            {
                Symbol = aTokenSymbol,
                From = src,
                To = dst,
                Amount = amount
            });
            State.ControllerContract.TransferVerify.Send(new TransferVerifyInput{AToken = aToken,Src = src,Dst = dst,TransferTokens = amount});

            
        }

        private void SetReserveFactorFresh(Address aToken, long newReserveFactor)
        {
            AssertSenderIsAdmin();
            var accrualBlockNumberPrior = State.AccrualBlockNumbers[aToken];
            Assert(accrualBlockNumberPrior == Context.CurrentHeight,
                "market's block number should equals current block number");
            Assert(newReserveFactor <= MaxReserveFactor,"Invalid ReserveFactor input");
            var oldReserveFactor = State.ReserveFactor[aToken];
            State.ReserveFactor[aToken] = newReserveFactor;
            Context.Fire(new ReserveFactorChanged()
            {
                OldReserveFactor = oldReserveFactor,
                NewReserveFactor = newReserveFactor,
                AToken = aToken
            });
            
        }
    }
}