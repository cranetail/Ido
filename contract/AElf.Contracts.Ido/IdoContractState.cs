
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Ido
{
    public partial class IdoContractState: ContractState
    {
        public SingletonState<Address> Admin { get; set; }
        //项目信息（上市前字段）
        public MappedState<Hash, ProjectInfo> ProjectInfoMap {get;set;}
        //项目上市后信息
        public MappedState<Hash, ProjectListInfo> ProjectListInfoMap {get;set;}
        //用户投资信息
        public MappedState<Hash, Address, InvestDetail> InvestDetailMap {get; set; }
        //用户收益（领取平台币信息）
        public MappedState<Hash,Address, ProfitDetail> ProfitDetailMap {get; set; }
        //已领取记录
        public MappedState<Address,ClaimedProfitsInfo> ClaimedProfitsInfoMap {get; set; }
        //违约金记录
        public MappedState<Hash, LiquidatedDamageDetails> LiquidatedDamageDetailsMap {get; set; }
        //白名单记录
        public MappedState<Hash, long> WhitelistIdMap { get; set; }
    }
}