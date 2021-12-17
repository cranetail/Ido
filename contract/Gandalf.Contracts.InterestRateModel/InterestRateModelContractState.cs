using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Gandalf.Contracts.InterestRateModel
{
    public class InterestRateModelContractState: ContractState
    {
        public SingletonState<Address> Owner { get; set; }
        
        public SingletonState<long> MultiplierPerBlock{ get; set; }
 
        public SingletonState<long> BaseRatePerBlock{ get; set; }
    
        public SingletonState<long> JumpMultiplierPerBlock { get; set; }
 
        public SingletonState<long> Kink{ get; set; }
    }
}