using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Types;
using Awaken.Contracts.Controller.Tests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
            address.Value.ShouldBe(AdminAddress.Value);
        }
        [Fact]
        public async Task SupportMarketTest()
        {
           await Initialize();
           await AdminStub.SupportMarket.SendAsync(new StringValue() {Value = "ELF"});
           var address = await AdminStub.GetATokenAddress.CallAsync(new StringValue() {Value = "ELF"});
           var gTokens =  await AdminStub.GetAllMarkets.CallAsync(new Empty());
           
        }

        private async Task Initialize()
        {
            await CreateAndGetToken();
            await AdminStub.Initialize.SendAsync(new Empty());
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
    }
}