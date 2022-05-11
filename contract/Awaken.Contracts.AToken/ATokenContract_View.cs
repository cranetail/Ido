using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Awaken.Contracts.InterestRateModel;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.AToken
{
    public partial class ATokenContract
    {
        public override Int64Value GetSupplyRatePerBlock(Address input)
        {
            return new Int64Value()
            {
                Value = GetSupplyRatePerBlockInternal(input)
            };
        }
        

        public override Int64Value GetBorrowRatePerBlock(Address input)
        {
            return new Int64Value()
            {
                Value = GetBorrowRatePerBlockInternal(input)
            };
        }

        public override Int64Value GetExchangeRateStored(Address input)
        {
            return new Int64Value()
            {
                Value = ExchangeRateStoredInternal(input)
            };
        }

        public override Address GetAdmin(Empty input)
        {
            return State.Admin.Value;
        }

        public override Address GetComptroller(Empty input)
        {
            return State.ControllerContract.Value;
        }

        public override Int64Value GetBalance(Account input)
        {
            var balance = new Int64Value()
            {
                Value = State.AccountTokens[input.AToken][input.User]
            };
            return balance;
        }

        public override Int64Value GetBorrowBalanceStored(Account input)
        {
            var borrowBalance = BorrowBalanceStoredInternal(input);
            return new Int64Value()
            {
                Value = borrowBalance
            };
        }

        
        public override Int64Value GetCash(Address input)
        {
           
            var result = State.TokenContract.GetBalance.Call(new GetBalanceInput()
            {
                Owner = Context.Self,
                Symbol =  State.UnderlyingMap[input]
            });
            return new Int64Value()
            {
                Value = result.Balance
            };
        }

        public override GetAccountSnapshotOutput GetAccountSnapshot(Account input)
        {
            var cTokenBalance = State.AccountTokens[input.AToken][input.User];
            var borrowBalance = BorrowBalanceStoredInternal(input);
            var exchangeRate = ExchangeRateStoredInternal(input.AToken);
            return new GetAccountSnapshotOutput()
            {
                BorrowBalance = borrowBalance,
                ATokenBalance = cTokenBalance,
                ExchangeRate = exchangeRate
            };
        }

        public override Int64Value GetAccrualBlockNumber(Address input)
        {

            return new Int64Value()
                {
                    Value = State.AccrualBlockNumbers[input]
                };
        }

        public override BigIntValue GetBorrowIndex(Address input)
        {
            return State.BorrowIndex[input];

        }

        public override Int64Value GetTotalBorrows(Address input)
        {
            return new Int64Value()
            {
                Value = State.TotalBorrows[input]
            };
        }

        public override Int64Value GetTotalReserves(Address input)
        {
            return new Int64Value()
            {
                Value = State.TotalReserves[input]
            };
        }

        public override Int64Value GetReserveFactor(Address input)
        {
            return new Int64Value()
            {
                Value = State.ReserveFactor[input]
            };
        }

        public override Address GetInterestRateModel(Address input)
        {
            return State.InterestRateModelContractsAddress[input];
        }

        public override Int64Value GetInitialExchangeRate(Address input)
        {
            return new Int64Value()
            {
                Value = State.InitialExchangeRate[input]
            }; 
        }

        public override Int64Value GetUnderlyingBalance(Account input)
        {
            var exchangeRate = GetCurrentExchangeRate(input.AToken);
            var underlyingBalance = new BigIntValue(State.AccountTokens[input.AToken][input.User]).Mul(ExchangeMantissa)
                .Div(exchangeRate.Value);
            if (!long.TryParse(underlyingBalance.Value, out var balance))
            {
                throw new AssertionException($"Failed to parse {balance}");
            }
            return new Int64Value()
            {
                Value = balance
            };
        }

        public override Int64Value GetCurrentBorrowBalance(Account input)
        {
            AccrueInterest(input.AToken);
            var balance =  BorrowBalanceStoredInternal(input);
            return new Int64Value()
            {
                Value = balance
            };
        }

        public override Int64Value GetCurrentExchangeRate(Address input)
        {
            AccrueInterest(input);
            var exchangeRate = ExchangeRateStoredInternal(input);
            return new Int64Value()
            {
                Value = exchangeRate
            };
        }
        
        public override Address GetATokenAddress(StringValue input)
        {
            return State.ATokenVirtualAddressMap[State.UnderlyingToTokenSymbolMap[input.Value]];
        }

        public override StringValue GetUnderlying(Address input)
        {
            return new StringValue
            {
                Value = State.UnderlyingMap[input]
            };
        }

        public override Int64Value GetTotalSupply(Address input)
        {
            return new Int64Value()
            {
                Value = State.TotalSupply[input]
            };
        }

        public override Int64Value GetAllowance(GetAllowanceInput input)
        {
            var aToken = State.ATokenVirtualAddressMap[input.Symbol];
            return new Int64Value()
            {
                Value = State.AllowanceMap[aToken][input.Owner][input.Spender]
            };

        }

        public override Balances GetBalances(GetBalancesInput input)
        {
            var balances = new Balances();
            foreach (var tokenSymbol in input.Symbols)
            {
                var aToken = State.ATokenVirtualAddressMap[tokenSymbol];
              
                balances.Value.Add(  new Balance()
                {
                    Amount = State.AccountTokens[aToken][Context.Sender],
                    Symbol = tokenSymbol
                });
            }

            return balances;
        }

        public override Int32Value GetDecimals(Empty input)
        {
            return new Int32Value()
            {
                Value = Decimals
            };
        }
    }
}