using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DasContract.Abstraction
{
    class Program
    {
        static void Main(string[] args)
        {
            string xmlString = File.ReadAllText(@"C:\\Users\\Johny\\Desktop\\diagram_1.bpmn");
            var contract = ContractFactory.FromBpmn(xmlString);
        }
    }
}
