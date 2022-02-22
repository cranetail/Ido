using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Types;
using Awaken.Contracts.AToken;
using Awaken.Contracts.Controller.Tests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Tsp;
using Shouldly;
using Xunit;

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
           await AdminStub.SupportMarket.SendAsync(new StringValue() {Value = "ELF"});
           var address = await AdminStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
           var aTokens =  await AdminStub.GetAllMarkets.CallAsync(new Empty());
           address.Value.ShouldNotBeEmpty();
           aTokens.AToken.ShouldContain(address);
           
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
            await AdminStub.SupportMarket.SendAsync(new StringValue() {Value = "ELF"});
            var aElfAddress = await AdminStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
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
            await AddMarket();
            var aElfAddress = await AdminStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            await TomStub.EnterMarkets.SendAsync(new ATokens() {AToken = {aElfAddress}});
            var assetList = await TomStub.GetAssetsIn.CallAsync(UserTomAddress);
            assetList.Assets.ShouldContain(aElfAddress);
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
            await AdminTokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "ELF",
                Memo = "Recharge",
                To = UserTomAddress
            });
        }

        private async Task AddMarket()
        {
            const long closeFactorExpect = 500000000000000000;
            const long collateralFactorExpect = 750000000000000000;
            const long liquidationIncentiveExpect = 1080000000000000000;
            const int maxAssetsExpect = 20;
            await AdminStub.SupportMarket.SendAsync(new StringValue() {Value = "ELF"});
            await AdminStub.SetCloseFactor.SendAsync(new Int64Value() {Value = closeFactorExpect});
            var aElfAddress = await AdminStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            await AdminStub.SetCollateralFactor.SendAsync(new SetCollateralFactorInput()
                {AToken = aElfAddress, NewCollateralFactor = collateralFactorExpect});
            await AdminStub.SetLiquidationIncentive.SendAsync(new Int64Value() {Value = liquidationIncentiveExpect});
            await AdminStub.SetMaxAssets.SendAsync(new Int32Value() {Value = maxAssetsExpect});
            await AdminStub.SetPriceOracle.SendAsync(PriceContractAddress);
        }

        private async Task InitializeAToken()
        {
            await AdminATokenContractStub.Initialize.SendAsync(new Empty());
            var aElfAddress = await AdminStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
            //to do InitializeAToken
        }
    }
}