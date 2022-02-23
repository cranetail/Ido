using AElf.Standards.ACS2;
using AElf.Types;
namespace Awaken.Contracts.AToken
{
    public partial class ATokenContract
    {
         public override ResourceInfo GetResourceInfo(Transaction txn)
        {
            switch (txn.MethodName)
            {
                case nameof(Transfer):
                {
                    var args = TransferInput.Parser.ParseFrom(txn.Params);
                    var resourceInfo = new ResourceInfo
                    {
                        WritePaths =
                        {
                            GetPath(nameof(ATokenContractState.BalanceMap), txn.From.ToString(), args.Symbol),
                            GetPath(nameof(ATokenContractState.BalanceMap), args.To.ToString(), args.Symbol),
                        },
                        ReadPaths =
                        {
                            GetPath(nameof(ATokenContractState.TokenInfoMap), args.Symbol),
                        }
                    };

                    AddPathForTransactionFee(resourceInfo, txn.From);
                    return resourceInfo;
                }

                case nameof(ATokenContractState):
                {
                    var args = TransferFromInput.Parser.ParseFrom(txn.Params);
                    var resourceInfo = new ResourceInfo
                    {
                        WritePaths =
                        {
                            GetPath(nameof(ATokenContractState.AllowanceMap), args.From.ToString(), txn.From.ToString(),
                                args.Symbol),
                            GetPath(nameof(ATokenContractState.BalanceMap), args.From.ToString(), args.Symbol),
                            GetPath(nameof(ATokenContractState.BalanceMap), args.To.ToString(), args.Symbol),
                        },
                        ReadPaths =
                        {
                            GetPath(nameof(ATokenContractState.TokenInfoMap), args.Symbol),
                        }
                    };
                    AddPathForTransactionFee(resourceInfo, txn.From);
                    return resourceInfo;
                }

                default:
                    return new ResourceInfo {NonParallelizable = true};
            }
        }

        private void AddPathForTransactionFee(ResourceInfo resourceInfo, Address from)
        {
            var path = GetPath(nameof(ATokenContractState.BalanceMap), from.ToString(), Context.Variables.NativeSymbol);
            if (!resourceInfo.WritePaths.Contains(path))
            {
                resourceInfo.WritePaths.Add(path);
            }
        }

        private ScopedStatePath GetPath(params string[] parts)
        {
            return new ScopedStatePath
            {
                Address = Context.Self,
                Path = new StatePath
                {
                    Parts =
                    {
                        parts
                    }
                }
            };
        }
    }
}