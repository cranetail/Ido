using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.Controller
{
    /// <summary>
    /// The state class of the contract, it inherits from the AElf.Sdk.CSharp.State.ContractState type. 
    /// </summary>
    public class ControllerContractState : ContractState
    {
        internal AElf.Contracts.MultiToken.TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        
        internal GToken.GTokenContractContainer.GTokenContractReferenceState GTokenContract { get; set; }

        internal AElf.Contracts.Price.PriceContractContainer.PriceContractReferenceState PriceContract { get; set; }
        /// <summary>
        /// Contract administrator
        /// </summary>
        public SingletonState<Address> Admin { get; set; }
        /// <summary>
        /// Contract pending administrator
        /// </summary>
        public SingletonState<Address> PendingAdmin { get; set; }
        /// <summary>
        /// Market administrator
        /// </summary>

        /// <summary>
        /// Multiplier used to calculate the maximum repayAmount when liquidating a borrow
        /// </summary>
        /// <returns></returns>
        public Int64State CloseFactor { get; set; }

        /// <summary>
        /// Market metadata
        /// </summary>
        public MappedState<Address, Market> Markets { get; set; }
        /// <summary>

        /// <summary>
        /// Multiplier representing the discount on collateral that a liquidator receives
        /// </summary>
        /// <returns></returns>
        public Int64State LiquidationIncentive { get; set; }

        /// <summary>
        /// Max number of assets a single account can participate in (borrow or use as collateral)
        /// </summary>
        /// <returns></returns>
        public Int32State MaxAssets { get; set; }
        /// Per-account mapping of "assets you are in", capped by maxAssets
        /// </summary>
        public MappedState<Address, AssetList> AccountAssets { get; set; }
        public SingletonState<Address> PauseGuardian { get; set; }
        public BoolState TransferGuardianPaused { get; set; }
        public BoolState SeizeGuardianPaused { get; set; }
        public MappedState<Address, bool> MintGuardianPaused { get; set; }
        public MappedState<Address, bool> BorrowGuardianPaused { get; set; }
        /// <summary>
        /// A list of all markets
        /// </summary>
        public SingletonState<SymbolList> AllMarkets { get; set; }

        /// @notice The rate at which the flywheel distributes PLATFORMTOKEN, per block
        public Int64State PlatformTokenRate { get; set; }
        /// @notice The portion of platformTokenRate that each market currently receives
        public MappedState<Address, Int64State> PlatformTokenSpeeds { get; set; }

        /// @notice The PLATFORMTOKEN market supply state for each market
        public MappedState<Address, PlatformTokenMarketState> PlatformTokenSupplyState
        { get; set; }

        /// @notice The PLATFORMTOKEN market borrow state for each market
        public MappedState<Address, PlatformTokenMarketState> PlatformTokenBorrowState{ get; set; }

        /// @notice The PLATFORMTOKEN borrow index for each market for each supplier as of the last time they accrued PLATFORMTOKEN
        public MappedState<Address, Address, Int64State> PlatformTokenSupplierIndex { get; set; }

        /// @notice The PLATFORMTOKEN borrow index for each market for each borrower as of the last time they accrued PLATFORMTOKEN
        public MappedState<Address,Address, Int64State> PlatformTokenBorrowerIndex { get; set; }

        /// @notice The PLATFORMTOKEN accrued but not yet transferred to each user
        public MappedState<Address, Int64State> PlatformTokenAccrued;
        // @notice The borrowCapGuardian can set borrowCaps to any number for any market. Lowering the borrow cap could disable borrowing on the given market.

        public SingletonState<Address> BorrowCapGuardian{ get; set; }
        // @notice Borrow caps enforced by borrowAllowed for each gToken address. Defaults to zero which corresponds to unlimited borrowing.
        public MappedState<Address, Int64State> BorrowCaps { get; set; }
    }
}