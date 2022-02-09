using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Gandalf.Contracts.Controller;
using Gandalf.Contracts.InterestRateModel;

namespace Gandalf.Contracts.GToken
{
    public partial class GTokenContract
    {
        private void MintInternal(Address gToken, long amount, string channel)
        {
            AccrueInterest(gToken);
            MintFresh(gToken, amount, channel);
        }

        private void MintFresh(Address gToken, long amount, string channel)
        {
            State.ControllerContract.MintAllowed.Call(new MintAllowedInput()
            {
                GToken = gToken,
                MintAmount = amount,
                Minter = Context.Self
            });
            Assert(State.AccrualBlockNumbers[gToken] == Context.CurrentHeight,"Market's block number should equals current block number");
            var exchangeRate = ExchangeRateStoredInternal(gToken);
            DoTransferIn(Context.Sender, amount, State.Underlying[gToken]);
            //  mintTokens = actualMintAmount / exchangeRate
            var mintTokens =amount.Div(exchangeRate) ;
            // totalSupplyNew = totalSupply + mintTokens
            var totalSupplyNew = State.TotalSupply[gToken].Add(mintTokens);
            //accountTokensNew = accountTokens[minter] + mintTokens
            var accountTokensNew = State.AccountTokens[gToken][Context.Sender].Add(mintTokens);
            State.TotalSupply[gToken] = totalSupplyNew;
            State.AccountTokens[gToken][Context.Sender] = accountTokensNew;
            Context.Fire(new Mint()
            {
                Address = Context.Sender,
                Amount = amount,
                CTokenAmount = mintTokens,
                Symbol = gToken
            });
        }

      
        private long GetCashPrior(Address gToken)
        {
             
            var result = State.TokenContract.GetBalance.Call(new GetBalanceInput()
            {
                Owner = Context.Self,
                Symbol = State.Underlying[gToken]
            });
            return result.Balance;
        }

        private long GetSupplyRatePerBlockInternal(Address gToken)
        {
            var totalCash = GetCashPrior(gToken);
            var totalBorrow = State.TotalBorrows[gToken];
            var totalReserves = State.TotalReserves[gToken];
            return State.InterestRateModelContracts[gToken].GetSupplyRate.Call(new GetSupplyRateInput()
            {
                Cash = totalCash,
                Borrows = totalBorrow,
                Reserves = totalReserves,
                ReserveFactor = State.ReserveFactor[gToken]
            }).Value;
        }

        private long GetBorrowRatePerBlockInternal(Address gToken)
        {
            var totalCash = GetCashPrior(gToken);
            var totalBorrow = State.TotalBorrows[gToken];
            var totalReserves = State.TotalReserves[gToken];
            return State.InterestRateModelContracts[gToken].GetBorrowRate.Call(new GetBorrowRateInput()
            {
                Cash = totalCash,
                Borrows = totalBorrow,
                Reserves = totalReserves
            }).Value;
        }

        private long ExchangeRateStoredInternal(Address gToken)
        {
            var totalSupply = State.TotalSupply[gToken];
            var totalCash = GetCashPrior(gToken);
            var totalBorrow = State.TotalBorrows[gToken];
            var totalReserves = State.TotalReserves[gToken];
            if (totalSupply == 0)
            {
                return State.InitialExchangeRate[gToken];
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
    }
}