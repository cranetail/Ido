using AElf.Boilerplate.TestBase;
using AElf.Cryptography.ECDSA;

namespace Gandalf.Contracts.Controller
{
    public class ControllerContractTestBase : DAppContractTestBase<ControllerContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);

        internal ControllerContractContainer.ControllerContractStub GetControllerContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<ControllerContractContainer.ControllerContractStub>(DAppContractAddress, senderKeyPair);
        }
    }
}