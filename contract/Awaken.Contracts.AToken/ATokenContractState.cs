using AElf.Sdk.CSharp.State;
using AElf.Types;
using Awaken.Contracts.Controller;
using Awaken.Contracts.InterestRateModel;

namespace Awaken.Contracts.AToken
{
    public class ATokenContractState:ContractState
    {
        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        
        internal MappedState<Address,InterestRateModelContractContainer.InterestRateModelContractReferenceState> InterestRateModelContracts
        {
            get;
            set;
        } 
        internal ControllerContractContainer.ControllerContractReferenceState ControllerContract { get;
            set;
        }
        public SingletonState<Address> Admin { get; set; }
        /// <summary>
        /// Contract pending administrator
        /// </summary>
        public SingletonState<Address> PendingAdmin { get; set; }
        
        public MappedState<string, Address> ATokenAddress { get; set; }
        
        public MappedState<Address, long> AccrualBlockNumbers { get; set; }
        /// <summary>
        /// Total amount of outstanding borrows of the underlying in this market
        /// </summary>
        public MappedState<Address, long> TotalBorrows { get; set; }
        /// <summary>
        /// Total amount of reserves of the underlying held in this market
        /// </summary>
        public MappedState<Address, long> TotalReserves { get; set; }
        /// <summary>
        /// Total number of tokens in circulation(CToken)
        /// </summary>
        public MappedState<Address, long> TotalSupply { get; set; }
        /// <summary>
        /// Accumulator of the total earned interest rate since the opening of the market
        /// </summary>
        public MappedState<Address, long> BorrowIndex { get; set; }
        /// <summary>
        /// Mapping of account addresses to outstanding borrow balances
        /// </summary>
        public MappedState<Address, Address, BorrowSnapshot> AccountBorrows { get; set; }
        /// <summary>
        /// Initial exchange rate used when minting the first CTokens (used when totalSupply = 0)
        /// </summary>
        /// <returns></returns>
        public MappedState<Address, long> InitialExchangeRate { get; set; }
        /// <summary>
        /// Fraction of interest currently set aside for reserves
        /// </summary>
        public MappedState<Address, long> ReserveFactor { get; set; }
        /// <summary>
        /// Token balances for each account(CToken)
        /// </summary>
        public MappedState<Address, Address, long> AccountTokens { get; set; }
        
        public MappedState<Address, string> Underlying { get; set; }
        
    }
}