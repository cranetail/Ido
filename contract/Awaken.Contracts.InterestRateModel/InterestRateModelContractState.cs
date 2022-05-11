using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Awaken.Contracts.InterestRateModel
{
    public class InterestRateModelContractState: ContractState
    {
        public SingletonState<Address> Owner { get; set; }
        
        public SingletonState<long> MultiplierPerBlock{ get; set; }
 
        public SingletonState<long> BaseRatePerBlock{ get; set; }
    
        public SingletonState<long> JumpMultiplierPerBlock { get; set; }
 
        public SingletonState<long> Kink{ get; set; }
        
        //bool : 0:WhitePaperInterestRateModel  1:JumpInterestRateModel
        public SingletonState<bool> InterestRateModelType { get; set; }
    }
}