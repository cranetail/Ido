using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;

using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;


namespace AElf.Contracts.Ido
{
    [DependsOn(typeof(MainChainDAppContractTestModule))]
    public class IdoContractTestModule: MainChainDAppContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o=>o.ContractDeploymentAuthorityRequired = false);
        }
    }
}