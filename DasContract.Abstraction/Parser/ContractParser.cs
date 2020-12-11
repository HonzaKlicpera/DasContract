using DasContract.Abstraction.Exceptions;
using DasContract.Abstraction.Processes;
using DasContract.DasContract.Abstraction.Data;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DasContract.Abstraction
{
    public class ContractParser
    {
        public static Contract FromDasFile(string path)
        {
            var contract = new Contract();
            var xDoc = XDocument.Load(path, LoadOptions.SetLineInfo);
            var xEditorContract = xDoc.Element("EditorContract");
            contract.Id = xEditorContract.Element("Id").Value;
            var xContractName = xEditorContract.Element("Name");
            if (xContractName != null)
                contract.Name = xContractName.Value;

            contract.Processes = ProcessParser.ParseProcesses(xDoc.Descendants("Process"));
            contract.DataTypes = DataModelParser.ParseDataTypes(xDoc.Descendants("DataModel").First());
            return contract;
        }

        public static string GetLineNumber(XElement xElement)
        {
            if ((xElement as IXmlLineInfo).HasLineInfo())
                return (xElement as IXmlLineInfo).LineNumber.ToString();
            return "N/A";
        }
    }
}
