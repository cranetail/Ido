using AElf.Sdk.CSharp.State;
using AElf.Types;
using Awaken.Contracts.AToken;
using Awaken.Contracts.Controller;

namespace Awaken.Contracts.AwakenLendingLens
{
    public class AwakenLendingLensContractState: ContractState
    {
        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        
        internal ControllerContractContainer.ControllerContractReferenceState ControllerContract { get;
            set;
        }
        internal ATokenContractContainer.ATokenContractReferenceState ATokenContract { get; set; }

        internal AElf.Contracts.Price.PriceContractContainer.PriceContractReferenceState PriceContract { get; set; }
       
        public MappedState<string,long> Price { get; set; }
    }
}

 