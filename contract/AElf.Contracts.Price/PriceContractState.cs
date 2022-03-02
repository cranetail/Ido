
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Price
{
    public class PriceContractState: ContractState
    {
        public MappedState<string,long> Price { get; set; }
    }
}