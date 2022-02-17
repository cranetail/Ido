using System;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Gandalf.Contracts.InterestRateModel;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.GToken
{
    public partial class GTokenContract
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
                Value = State.AccountTokens[input.GToken][input.User]
            };
            return balance;
        }

        public override Int64Value GetBorrowBalanceStored(Account input)
        {
            var result = BorrowBalanceStoredInternal(input);
            return new Int64Value()
            {
                Value = Convert.ToInt64(result.Value)
            };
        }

        
        public override Int64Value GetCash(Address input)
        {
           
            var result = State.TokenContract.GetBalance.Call(new GetBalanceInput()
            {
                Owner = Context.Self,
                Symbol =  State.Underlying[input]
            });
            return new Int64Value()
            {
                Value = result.Balance
            };
        }

        public override GetAccountSnapshotOutput GetAccountSnapshot(Account input)
        {
            var cTokenBalance = State.AccountTokens[input.User][input.GToken];
            var borrowBalance = Convert.ToInt64(BorrowBalanceStoredInternal(input).Value);
            var exchangeRate = ExchangeRateStoredInternal(input.GToken);
            return new GetAccountSnapshotOutput()
            {
                BorrowBalance = borrowBalance,
                GTokenBalance = cTokenBalance,
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

        public override Int64Value GetBorrowIndex(Address input)
        {
            return base.GetBorrowIndex(input);
        }
        
    }
}