
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Ido
{
    public class IdoContractState: ContractState
    {
        public MappedState<string,long> Price { get; set; }
    }
}