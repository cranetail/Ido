using Awaken.Contracts.Swap;

namespace AElf.Contracts.Ido
{
    public partial class IdoContractState
    {
       internal  AElf.Contracts.Whitelist.WhitelistContractContainer.WhitelistContractReferenceState WhitelistContract
       {
           get;
           set;
       }

       internal Awaken.Contracts.Swap.AwakenSwapContractContainer.AwakenSwapContractReferenceState SwapContract
       {
           get;
           set;
       }
        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}