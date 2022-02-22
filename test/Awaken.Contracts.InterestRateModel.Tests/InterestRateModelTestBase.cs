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
using AElf.Standards.ACS0;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;


namespace Awaken.Contracts.InterestRateModel.Tests
{
    public class InterestRateModelContractTestBase : DAppContractTestBase<InterestRateModelContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        internal readonly Address InterestRateModelContractAddress;

        internal InterestRateModelContractContainer.InterestRateModelContractStub GetInterestRateModelContractStub(
            ECKeyPair senderKeyPair)
        {
            return Application.ServiceProvider.GetRequiredService<IContractTesterFactory>()
                .Create<Awaken.Contracts.InterestRateModel.InterestRateModelContractContainer.
                    InterestRateModelContractStub>(InterestRateModelContractAddress, senderKeyPair);
        }



        public InterestRateModelContractTestBase()
        {
            InterestRateModelContractAddress = AsyncHelper.RunSync(() => DeployContractAsync(
                KernelConstants.DefaultRunnerCategory,
                File.ReadAllBytes(typeof(InterestRateModelContract).Assembly.Location),
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


        internal InterestRateModelContractContainer.InterestRateModelContractStub AdminStub =>
            GetInterestRateModelContractStub(AdminKeyPair);


    }
}