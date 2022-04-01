using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Price;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Awaken.Contracts.Controller;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.AwakenLendingLens
{
    public partial class AwakenLendingLensContract: AwakenLendingLensContractContainer.AwakenLendingLensContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.TokenContract.Value == null, "Already initialized.");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.ATokenContract.Value = input.ATokenContract;
            State.ControllerContract.Value = input.ComtrollerContract;
            
            return new Empty();
        }

        public override ATokenMetadataAll GetATokenMetadataAll(ATokens input)
        {
            var result = new ATokenMetadataAll();
            foreach (var aToken in input.AToken)
            {
                result.Metadata.Add(GetATokenMetadata(aToken));
            }

            return result;
        }

        public override ATokenMetadata GetATokenMetadata(Address input)
        {
            return GetATokenMetadataInline(input);
        }

        public override ATokenBalances GetATokenBalances(Account input)
        {
            var account = new AToken.Account() {AToken = input.AToken, User = input.Address};
            var balanceOf = State.ATokenContract.GetBalance.Call(account).Value;
            var borrowBalanceCurrent = State.ATokenContract.GetCurrentBorrowBalance.Call(account).Value;
            var balanceOfUnderlying =  State.ATokenContract.GetUnderlyingBalance.Call(account).Value;
            var underlying = State.ATokenContract.GetUnderlying.Call(input.AToken).Value;
            var tokenBalance = State.TokenContract.GetBalance
                .Call(new GetBalanceInput() {Owner = input.Address, Symbol = underlying}).Balance;
            var tokenAllowance = State.TokenContract.GetAllowance.Call(new GetAllowanceInput()
                {Symbol = underlying, Spender = State.ATokenContract.Value, Owner = input.Address}).Allowance;
            return new ATokenBalances()
            {
                AToken = input.AToken,
                BalanceOf = balanceOf,
                BorrowBalanceCurrent = borrowBalanceCurrent,
                BalanceOfUnderlying = balanceOfUnderlying,
                TokenBalance = tokenBalance,
                TokenAllowance = tokenAllowance
            };
        }

        public override ATokenBalancesAll GetATokenBalancesAll(GetATokenBalancesAllInput input)
        {
            var result = new ATokenBalancesAll();
            foreach (var aToken in input.ATokens.AToken)
            {
                var account = new Account() {AToken = aToken, Address = input.User};
                result.Balances.Add(GetATokenBalances(account));
            }

            return result;
        }

        public override ATokenUnderlyingPrice GetATokenUnderlyingPrice(Address input)
        {
            var oracle = State.ControllerContract.GetPriceOracle.Call(new Empty());
            if (State.PriceContract.Value != oracle)
                State.PriceContract.Value = oracle;
            var underlying = State.ATokenContract.GetUnderlying.Call(input).Value;
            var priceStr = State.PriceContract.GetExchangeTokenPriceInfo.Call(new GetExchangeTokenPriceInfoInput()
            {
                TokenSymbol = underlying,
                TargetTokenSymbol = "",
            });
            if (!long.TryParse(priceStr.Value, out var price))
            {
                throw new AssertionException($"Failed to parse {priceStr.Value}");
            }

            return new ATokenUnderlyingPrice()
            {
                AToken = input,
                UnderlyingPrice = price
            };

        }

        public override ATokenUnderlyingPriceAll GetATokenUnderlyingPriceAll(ATokens input)
        {
            var result = new ATokenUnderlyingPriceAll();
            foreach (var aToken in input.AToken)
            {
            
                result.UnderlyingPrice.Add(GetATokenUnderlyingPrice(aToken));
            }

            return result;
        }

        public override AccountLimits GetAccountLimits(GetAccountLimitsInput input)
        {
            if (State.ControllerContract.Value != input.Comptroller)
                State.PriceContract.Value = input.Comptroller;
            var result = State.ControllerContract.GetAccountLiquidity.Call(input.User);
            var market = State.ControllerContract.GetAssetsIn.Call(input.User).Assets;
            return new AccountLimits()
            {
                Markets = {new List<Address>(market)},
                Liquidity = result.Liquidity,
                Shortfall = result.Shortfall
            };
        }

        public override PlatformTokenBalanceMetadata GetPlatformTokenBalanceMetadata(GetPlatformTokenBalanceMetadataInput input)
        {
            var balance = State.TokenContract.GetBalance.Call(
                new GetBalanceInput() {Owner = input.User, Symbol = input.PlatformToken}).Balance;
            return new PlatformTokenBalanceMetadata()
            {
                Balance = balance,
                Delegate = new Address(),
                Votes = 0
            };
        }

        public override PlatformTokenBalanceMetadataExt GetPlatformTokenBalanceMetadataExt(GetPlatformTokenBalanceMetadataExtInput input)
        {
            var balance = State.TokenContract.GetBalance.Call(
                new GetBalanceInput() {Owner = input.User, Symbol = input.PlatformToken}).Balance;
            var market = State.ControllerContract.GetAllMarkets.Call(new Empty());
            var allocatedAmount = State.ControllerContract.GetPlatformTokenClaimAmount.Call(new ClaimPlatformTokenInput(){ ATokens = { market.AToken},Holders = { input.User},Borrowers = true,Suppliers = true}).Value;
           
        
            return new PlatformTokenBalanceMetadataExt()
            {
                Allocated = allocatedAmount,
                Balance = balance,
                Delegate = new Address(),
                Votes = 0
            };
        }
        

        public override ATokenMetadata GetATokenMetadataInline(Address input)
        {
            var market = State.ControllerContract.GetMarket.Call(input);
            var underlying = State.ATokenContract.GetUnderlying.Call(input).Value;
            var underlyingDecimals=  State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput() {Symbol = underlying}).Decimals;
            var exchangeRateCurrent = State.ATokenContract.GetExchangeRateStored.Call(input).Value;
            var supplyRatePerBlock = State.ATokenContract.GetSupplyRatePerBlock.Call(input).Value;
            var borrowRatePerBlock = State.ATokenContract.GetBorrowRatePerBlock.Call(input).Value;
            var reserveFactorMantissa = State.ATokenContract.GetReserveFactor.Call(input).Value;
            var totalBorrows = State.ATokenContract.GetTotalBorrows.Call(input).Value;
            var totalReserves = State.ATokenContract.GetTotalReserves.Call(input).Value;
            var totalSupply = State.ATokenContract.GetTotalSupply.Call(input).Value;
            var totalCash = State.ATokenContract.GetCash.Call(input).Value;
            var aTokenDecimals = State.ATokenContract.GetDecimals.Call(new Empty()).Value;

            return new ATokenMetadata()
            {
                AToken = input,
                ExchangeRateCurrent = exchangeRateCurrent,
                SupplyRatePerBlock = supplyRatePerBlock,
                BorrowRatePerBlock = borrowRatePerBlock,
                ReserveFactorMantissa = reserveFactorMantissa,
                TotalBorrows = totalBorrows,
                TotalReserves = totalReserves,
                TotalSupply = totalSupply,
                TotalCash = totalCash,
                IsListed = market.IsListed,
                CollateralFactorMantissa = market.CollateralFactor,
                UnderlyingAsset = underlying,
                ATokenDecimals = aTokenDecimals,
                UnderlyingDecimals = underlyingDecimals

            };
        }
    }
}