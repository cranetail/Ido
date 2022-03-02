using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Price;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Types;
using Awaken.Contracts.AToken;
using Awaken.Contracts.Controller.Tests;
using Awaken.Contracts.InterestRateModel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Tsp;
using Shouldly;
using Xunit;
using ApproveInput = AElf.Contracts.MultiToken.ApproveInput;
using CreateInput = Awaken.Contracts.AToken.CreateInput;

namespace Awaken.Contracts.Controller
{
    public class ControllerContractTests : ControllerContractTestBase
    {
        
        [Fact]
        public async Task InitializeTest()
        {
            await Initialize();
            var address = await AdminStub.GetAdmin.CallAsync(new Empty());
            address.ShouldBe(AdminAddress);
        }
        [Fact]
        public async Task SupportMarketTest()
        {
           await Initialize();
           await CreateAToken();
           var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
           await AdminStub.SupportMarket.SendAsync(aElfAddress); ;
           var aTokens =  await AdminStub.GetAllMarkets.CallAsync(new Empty());
           aElfAddress.Value.ShouldNotBeEmpty();
           aTokens.AToken.ShouldContain(aElfAddress);
           
        }

        [Fact]
        public async Task SetCloseFactorTest()
        {
            const long closeFactorExpect = 500000000000000000;
            await Initialize();
            await AdminStub.SetCloseFactor.SendAsync(new Int64Value() {Value = closeFactorExpect});
            var closeFactor = await AdminStub.GetCloseFactor.CallAsync(new Empty());
            closeFactor.Value.ShouldBe(closeFactorExpect);
        }
        [Fact]
        public async Task SetCollateralFactorTest()
        {
            const long collateralFactorExpect = 750000000000000000;
            
            await Initialize();
            await CreateAToken();
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            await AdminStub.SupportMarket.SendAsync(aElfAddress); 
            await AdminStub.SetPriceOracle.SendAsync(PriceContractAddress);
       
            await AdminStub.SetCollateralFactor.SendAsync(new SetCollateralFactorInput()
                {AToken = aElfAddress, NewCollateralFactor = collateralFactorExpect});
            var collateralFactor = await AdminStub.GetCollateralFactor.CallAsync(aElfAddress);
            collateralFactor.Value.ShouldBe(collateralFactorExpect);
        }
        [Fact]
        public async Task SetLiquidationIncentiveTest()
        {
            const long liquidationIncentiveExpect = 1080000000000000000;
            await Initialize();
            await AdminStub.SetLiquidationIncentive.SendAsync(new Int64Value() {Value = liquidationIncentiveExpect});
            var liquidationIncentive = await AdminStub.GetLiquidationIncentive.CallAsync(new Empty());
            liquidationIncentive.Value.ShouldBe(liquidationIncentiveExpect);
        }
        [Fact]
        public async Task SetMaxAssetsTest()
        {
            const int maxAssetsExpect = 20;
            await Initialize();
            await AdminStub.SetMaxAssets.SendAsync(new Int32Value() {Value = maxAssetsExpect});
            var maxAssets = await AdminStub.GetMaxAssets.CallAsync(new Empty());
            maxAssets.Value.ShouldBe(maxAssetsExpect);
        }
        [Fact]
        public async Task SetPriceOracleTest()
        {
            await Initialize();
            await AdminStub.SetPriceOracle.SendAsync(PriceContractAddress);
            var oracle = await AdminStub.GetPriceOracle.CallAsync(new Empty());
            oracle.ShouldBe(PriceContractAddress);
        }
        [Fact]
        public async Task EnterMarketsTest()
        {
            await Initialize();
            await CreateAToken();
            await AddMarket();
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            var assetList = await TomStub.GetAssetsIn.CallAsync(UserTomAddress);
            assetList.Assets.ShouldContain(aElfAddress);
        }
        [Fact]
        public async Task ExitMarketsTest()
        {
            await Initialize();
            await CreateAToken();
            
            await AddMarket();
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
           
            await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            await TomStub.ExitMarket.SendAsync(aElfAddress);
        }
        
        [Fact]
        public async Task MintTest()
        {
            await Initialize();
            await CreateAToken();
            
            await AddMarket();
            const long amount = 100000000;
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
           
            //await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            await UserTomATokenContractStub.Mint.SendAsync(new MintInput() {AToken = aElfAddress, MintAmount = amount, Channel = ""});
           var aElfBalance =  await UserTomATokenContractStub.GetBalance.CallAsync(new AToken.Account()
            {
                AToken = aElfAddress,
                User = UserTomAddress
            });
           aElfBalance.Value.ShouldBe(amount);
        }
        
        [Fact]
        public async Task RedeemTest()
        {
            await Initialize();
            await CreateAToken();
            
            await AddMarket();
            const long amount = 100000000;
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
           
            //await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            await UserTomATokenContractStub.Mint.SendAsync(new MintInput() {AToken = aElfAddress, MintAmount = amount, Channel = ""});
            await UserTomATokenContractStub.Redeem.SendAsync(new RedeemInput() {AToken = aElfAddress, Amount = amount});
            var balance= await UserTomATokenContractStub.GetBalance.CallAsync(new AToken.Account()
                {AToken = aElfAddress, User = UserTomAddress});
            balance.Value.ShouldBe(0);
        }
        [Fact]
        public async Task RedeemUnderlyingTest()
        {
            await Initialize();
            await CreateAToken();
            
            await AddMarket();
            const long amount = 100000000;
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
           
            //await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            await UserTomATokenContractStub.Mint.SendAsync(new MintInput() {AToken = aElfAddress, MintAmount = amount, Channel = ""});
            await UserTomATokenContractStub.RedeemUnderlying.SendAsync(new RedeemUnderlyingInput() {AToken = aElfAddress, Amount = amount});
            var balance= await UserTomATokenContractStub.GetBalance.CallAsync(new AToken.Account()
                {AToken = aElfAddress, User = UserTomAddress});
            balance.Value.ShouldBe(0);
        }

        [Fact]
        public async Task BorrowTest()
        {
            await Initialize();
            await CreateAToken();
            
            await AddMarket();
            
            const long mintAmount = 100000000;
            const long borrowAmount = 10000000;
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            //await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            await UserTomATokenContractStub.Mint.SendAsync(new MintInput() {AToken = aElfAddress, MintAmount = mintAmount, Channel = ""});
            
            await UserTomATokenContractStub.Borrow.SendAsync(new BorrowInput()
                {AToken = aElfAddress, Amount = borrowAmount});
            var balance =  await UserTomATokenContractStub.GetBorrowBalanceStored.CallAsync(new AToken.Account()
                {AToken = aElfAddress, User = UserTomAddress});
            balance.Value.ShouldBe(borrowAmount);
        }
        [Fact]
        public async Task RepayBorrowTest()
        {
            await Initialize();
            await CreateAToken();
            
            await AddMarket();
            
            const long mintAmount = 100000000;
            const long borrowAmount = 10000000;
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            //await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            await UserTomATokenContractStub.Mint.SendAsync(new MintInput() {AToken = aElfAddress, MintAmount = mintAmount, Channel = ""});
            
            await UserTomATokenContractStub.Borrow.SendAsync(new BorrowInput()
                {AToken = aElfAddress, Amount = borrowAmount});

            await UserTomATokenContractStub.RepayBorrow.SendAsync(new RepayBorrowInput()
                {AToken = aElfAddress, Amount = borrowAmount});
        }
        
        [Fact]
        public async Task LiquidateBorrowTest()
        {
            await Initialize();
            await CreateAToken();
            
            await AddMarket();
            
            const long mintAmount = 100000000;
            const long borrowAmount = 75000000;
            const long repayAmount = 10000000;
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            var aDaiAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "DAI"});
            await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
             await AdminStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aDaiAddress}});
             await UserTomATokenContractStub.Mint.SendAsync(new MintInput() {AToken = aElfAddress, MintAmount = mintAmount, Channel = ""});
            
            await AdminATokenContractStub.Mint.SendAsync(new MintInput() {AToken = aDaiAddress, MintAmount = mintAmount, Channel = ""});
            
            await UserTomATokenContractStub.Borrow.SendAsync(new BorrowInput()
                {AToken = aDaiAddress, Amount = borrowAmount});

            //set the borrow Token Price arise to trigger LiquidateBorrow
            await AdminPriceContractStub.SetPrice.SendAsync(new SetPriceInput() {TokenSymbol = "DAI",Price = 1100000000000000000}); 
            await AdminATokenContractStub.LiquidateBorrow.SendAsync(new LiquidateBorrowInput(){CollateralToken = aElfAddress,BorrowToken = aDaiAddress,Borrower = UserTomAddress,RepayAmount = repayAmount});
            
         
        } 
        private async Task Initialize()
        {
            await CreateAndGetToken();
            await AdminStub.Initialize.SendAsync(new InitializeInput()
            {
                ATokenContract =  ATokenContractAddress
            });
        }

        private async Task CreateAndGetToken()
        {
            const long approveAmount = long.MaxValue;
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "ELF",
                Memo = "Recharge",
                To = UserTomAddress
            });
            await UserTomTokenContractStub.Approve.SendAsync(new ApproveInput()
            {
                Amount = approveAmount,
                Spender = ATokenContractAddress,
                Symbol = "ELF"
            });
            await AdminTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput
            {
                Amount = approveAmount,
                Spender = ATokenContractAddress,
                Symbol = "ELF"
            });
            //DAI
            var result2 = await AdminTokenContractStub.Create.SendAsync(new AElf.Contracts.MultiToken.CreateInput
            {
                Issuer = AdminAddress,
                Symbol = "DAI",
                Decimals = 10,
                IsBurnable = true,
                TokenName = "DAI symbol",
                TotalSupply = 100000000_00000000
            });

            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult2 = await AdminTokenContractStub.Issue.SendAsync(new AElf.Contracts.MultiToken.IssueInput
            {
                Amount = 100000000000000,
                Symbol ="DAI",
                To = AdminAddress
            });
            issueResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "DAI",
                Memo = "Recharge",
                To = UserTomAddress
            });
            
            await UserTomTokenContractStub.Approve.SendAsync(new ApproveInput()
            {
                Amount = approveAmount,
                Spender = ATokenContractAddress,
                Symbol = "DAI"
            });
            await AdminTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput
            {
                Amount = approveAmount,
                Spender = ATokenContractAddress,
                Symbol = "DAI"
            });

            //set PRICE
            await AdminPriceContractStub.SetPrice.SendAsync(new SetPriceInput() {TokenSymbol = "ELF",Price = 1000000000000000000});
            await AdminPriceContractStub.SetPrice.SendAsync(new SetPriceInput() {TokenSymbol = "DAI",Price = 1000000000000000000});
        }

        private async Task AddMarket()
        {
            
            const long closeFactorExpect = 500000000000000000;
            const long collateralFactorExpect = 750000000000000000;
            const long liquidationIncentiveExpect = 1080000000000000000;
            const int maxAssetsExpect = 20;
            
            //ELF
            var aElfAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            await AdminStub.SetPriceOracle.SendAsync(PriceContractAddress);
            await AdminStub.SupportMarket.SendAsync(aElfAddress);
            await AdminStub.SetCloseFactor.SendAsync(new Int64Value() {Value = closeFactorExpect});
            await AdminStub.SetCollateralFactor.SendAsync(new SetCollateralFactorInput()
                {AToken = aElfAddress, NewCollateralFactor = collateralFactorExpect});
            await AdminStub.SetLiquidationIncentive.SendAsync(new Int64Value() {Value = liquidationIncentiveExpect});
            await AdminStub.SetMaxAssets.SendAsync(new Int32Value() {Value = maxAssetsExpect});
          
            //Dai
            var aDaiAddress = await AdminATokenContractStub.GetATokenAddress.CallAsync(new StringValue() {Value = "DAI"});
            await AdminStub.SupportMarket.SendAsync(aDaiAddress);
            await AdminStub.SetCollateralFactor.SendAsync(new SetCollateralFactorInput()
                {AToken = aDaiAddress, NewCollateralFactor = collateralFactorExpect});
        }

        private async Task CreateAToken()
        {
            const long initialExchangeRate = 1000000000000000000;
            await InitializeInterestRateModel();
            await AdminATokenContractStub.Initialize.SendAsync(new AToken.InitializeInput()
            {
                Controller = ControllerContractAddress
            });
            await AdminATokenContractStub.Create.SendAsync(new CreateInput()
            {
                UnderlyingSymbol = "ELF",
                InterestRateModel = InterestRateModelContractAddress,
                InitialExchangeRate = initialExchangeRate
            });
           
            //Dai
            await AdminATokenContractStub.Create.SendAsync(new CreateInput()
            {
                UnderlyingSymbol = "DAI",
                InterestRateModel = InterestRateModelContractAddress,
                InitialExchangeRate = initialExchangeRate
            });
            

            
        }
        private async Task InitializeInterestRateModel()
        {
            const long baseRatePerYear = 0;
            const long  multiplierPerYear = 57500000000000000;
            const long jumpMultiplierPerYear = 3000000000000000000;
            const long kink = 800000000000000000;
            await AdminInterestRateModelStub.Initialize.SendAsync(new UpdateJumpRateModelInput()
            {
                BaseRatePerYear = baseRatePerYear,
                MultiplierPerYear = multiplierPerYear,
                JumpMultiplierPerYear = jumpMultiplierPerYear,
                Kink = kink
            });
        } 
    }
}