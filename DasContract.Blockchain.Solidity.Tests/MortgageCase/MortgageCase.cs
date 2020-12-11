using DasContract.Abstraction;
using DasContract.Blockchain.Solidity.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace DasContract.Blockchain.Solidity.Tests.MortgageCase
{
    public class MortgageCase
    {
        [Fact]
        public void Convert()
        {
            var contract = ContractParser.FromDasFile("C:\\Users\\Johny\\Downloads\\mortgage.dascontract");
            var contractConverter = new ContractConverter(contract);
            contractConverter.ConvertContract();
            File.WriteAllText("C:\\Users\\Johny\\Downloads\\mortgage.sol", contractConverter.GetSolidityCode()); 
        }
    }
}
