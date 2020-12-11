using System;
using System.Collections.Generic;
using System.Text;

namespace DasContract.Abstraction.Data
{
    public class Token : Entity
    {
        public string Symbol { get; set; }
        public bool IsFungible { get; set; }
        public bool IsIssued { get; set; }
        //If the token contract is already deployed and is only supposed to be utilized in the contract, 
        //then the address of the contract should be specified in this field. If this field is null, then
        //a new token contract is created and deployed when the main contract is deployed.
        public string Address { get; set; }

        public string MintScript { get; set; }
        public string TransferScript { get; set; }
    }
}
