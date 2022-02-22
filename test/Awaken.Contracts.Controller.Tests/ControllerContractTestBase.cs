using AElf.Boilerplate.TestBase;
using AElf.Cryptography.ECDSA;
using System.IO;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using System.Linq;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using Awaken.Contracts.GToken;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;


namespace Awaken.Contracts.Controller.Tests
{
    public class ControllerContractTestBase : DAppContractTestBase<ControllerContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        internal readonly Address ControllerContractAddress;

        internal readonly Address GTokenContractAddress;

        private Address tokenContractAddress => GetAddress(TokenSmartContractAddressNameProvider.StringName);
        internal ControllerContractContainer.ControllerContractStub GetControllerContractStub(
            ECKeyPair senderKeyPair)
        {
            return Application.ServiceProvider.GetRequiredService<IContractTesterFactory>()
                .Create<Awaken.Contracts.Controller.ControllerContractContainer.
                    ControllerContractStub>(ControllerContractAddress, senderKeyPair);
        }

        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
        {
            return Application.ServiceProvider.GetRequiredService<IContractTesterFactory>()
                .Create<AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub>(tokenContractAddress, senderKeyPair);
        }

        public ControllerContractTestBase()
        {
            ControllerContractAddress = AsyncHelper.RunSync(() => DeployContractAsync(
                KernelConstants.DefaultRunnerCategory,
                File.ReadAllBytes(typeof(ControllerContract).Assembly.Location),
                SampleAccount.Accounts[0].KeyPair));
            GTokenContractAddress = AsyncHelper.RunSync(() => DeployContractAsync(
                KernelConstants.DefaultRunnerCategory,
                File.ReadAllBytes(typeof(GTokenContract).Assembly.Location),
                SampleAccount.Accounts[0].KeyPair));

        }

        private async Task<Address> DeployContractAsync(int category, byte[] code, ECKeyPair keyPair)
        {
            var addressService = Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            var stub = GetTester<ACS0Container.ACS0Stub>(addressService.GetZeroSmartContractAddress(),
                keyPair);
            var executionResult = await stub.DeploySmartContract.SendAsync(new ContractDeploymentInput
            {
                Category = category,
                Code = ByteString.CopyFrom(code)
            });
            return executionResult.Output;
        }

        private ECKeyPair AdminKeyPair { get; set; } = SampleAccount.Accounts[0].KeyPair;
        private ECKeyPair UserTomKeyPair { get; set; } = SampleAccount.Accounts.Last().KeyPair;
        private ECKeyPair UserLilyKeyPair { get; set; } = SampleAccount.Accounts.Reverse().Skip(1).First().KeyPair;

        internal Address AdminAddress => Address.FromPublicKey(AdminKeyPair.PublicKey);
        internal Address UserTomAddress => Address.FromPublicKey(UserTomKeyPair.PublicKey);
        internal Address UserLilyAddress => Address.FromPublicKey(UserLilyKeyPair.PublicKey);


        internal ControllerContractContainer.ControllerContractStub AdminStub =>
            GetControllerContractStub(AdminKeyPair);
        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub AdminTokenContractStub =>
            GetTokenContractStub(AdminKeyPair);
        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractStub UserTomTokenContractStub =>
            GetTokenContractStub(UserTomKeyPair);
    }
}