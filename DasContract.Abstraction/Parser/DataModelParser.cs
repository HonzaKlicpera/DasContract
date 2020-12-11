using DasContract.Abstraction.Data;
using DasContract.Abstraction.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Enum = DasContract.Abstraction.Data.Enum;

namespace DasContract.DasContract.Abstraction.Data
{
    public class DataModelParser
    {

        public static IDictionary<string, DataType> ParseDataTypes(XElement xDataModel)
        {
            IDictionary<string, DataType> dataTypes = new Dictionary<string, DataType>();
            var xDataTypeElements = xDataModel.Descendants();

            foreach (var xElement in xDataTypeElements)
            {
                DataType dataType = null;
                if (xElement.Name == "ContractEntity")
                {
                    dataType = ParseEntity(xElement);
                }
                else if (xElement.Name == "ContractToken")
                {
                    dataType = ParseToken(xElement);
                }
                else if (xElement.Name == "ContractEnum")
                {
                    dataType = ParseEnum(xElement);
                }

                if (dataType != null)
                {
                    dataTypes.Add(dataType.Id, dataType);
                }
            }
            return dataTypes;
        }

        private static Entity ParseEntity(XElement xEntity)
        {
            Entity entity = new Entity();

            var xEntityElements = xEntity.Elements();
            FillCommonDataTypeProperties(xEntityElements, entity);

            foreach (var xEntityElement in xEntityElements)
            {
                if (xEntityElement.Name == "IsRootEntity")
                {
                    entity.IsRootEntity = bool.Parse(xEntityElement.Value);
                }
                if (xEntityElement.Name == "PrimitiveProperties" || xEntityElement.Name == "ReferenceProperties")
                {
                    entity.Properties = entity.Properties.Union(ParseEntityProperties(xEntityElement)).ToList();
                }
            }
            return entity;
        }

        private static IList<Property> ParseEntityProperties(XElement xProperties)
        {
            IList<Property> properties = new List<Property>();
            var xPropertiesElements = xProperties.Elements();

            foreach (var xPropertyElement in xPropertiesElements)
            {
                properties.Add(ParseEntityProperty(xPropertyElement));
            }

            return properties;
        }

        private static Token ParseToken(XElement xToken)
        {
            Token token = new Token();

            var xEntityDescendants = xToken.Descendants().ToList();
            FillCommonDataTypeProperties(xEntityDescendants, token);

            foreach (var xContractElement in xEntityDescendants)
            {
                if (xContractElement.Name == "PrimitiveContractProperty" || xContractElement.Name == "ReferenceContractProperty")
                    token.Properties.Add(ParseEntityProperty(xContractElement));
                else if (xContractElement.Name == "Symbol")
                    token.Symbol = xContractElement.Value;
                else if (xContractElement.Name == "Address")
                    token.Address = xContractElement.Value;
                else if (xContractElement.Name == "IsFungible")
                    token.IsFungible = bool.Parse(xContractElement.Value);
                else if (xContractElement.Name == "IsIssued")
                    token.IsIssued = bool.Parse(xContractElement.Value);
                else if (xContractElement.Name == "MintScript")
                    token.MintScript = xContractElement.Value;
                else if (xContractElement.Name == "TransferScript")
                    token.TransferScript = xContractElement.Value;
            }
            return token;
        }

        private static Enum ParseEnum(XElement xEnum)
        {
            var @enum = new Enum();

            var xEnumElements = xEnum.Elements();
            FillCommonDataTypeProperties(xEnumElements, @enum);

            foreach (var xContractElement in xEnumElements)
            {
                if (xContractElement.Name == "EnumValues")
                {
                    @enum.Values = ParseEnumValues(xContractElement);
                }
            }
            return @enum;
        }

        private static IList<string> ParseEnumValues(XElement xEnumValues)
        {
            var enumValues = new List<string>();
            foreach (var xEnumValue in xEnumValues.Elements())
            {
                if (xEnumValue.Name == "EnumValue")
                    enumValues.Add(xEnumValue.Value);
            }
            return enumValues;
        }

        private static void FillCommonDataTypeProperties(IEnumerable<XElement> xDataTypeDescendants, DataType dataType)
        {
            foreach (var xContractElement in xDataTypeDescendants)
            {
                if (xContractElement.Name == "Name")
                {
                    dataType.Name = RemoveWhitespaces(xContractElement.Value);
                }
                else if (xContractElement.Name == "Id")
                {
                    dataType.Id = xContractElement.Value;
                }
            }
        }

        private static Property ParseEntityProperty(XElement xProperty)
        {
            Property property = new Property();
            var xPropertyElements = xProperty.Elements();

            foreach (var xPropertyElement in xPropertyElements)
            {
                if (xPropertyElement.Name == "Id")
                {
                    property.Id = xPropertyElement.Value;
                }
                else if (xPropertyElement.Name == "Name")
                {
                    property.Name = xPropertyElement.Value;
                }
                else if (xPropertyElement.Name == "IsMandatory")
                {
                    property.IsMandatory = bool.Parse(xPropertyElement.Value);
                }

                else if (xPropertyElement.Name == "Type")
                {
                    if (System.Enum.TryParse(xPropertyElement.Value, out PropertyType propertyType))
                        property.PropertyType = propertyType;
                    else
                        throw new ParseException($"{xPropertyElement.Value} is not a valid property type");
                }
                else if (xPropertyElement.Name == "DataType")
                {
                    property.DataType = ParsePropertyDataType(xPropertyElement.Value);
                }
                else if (xPropertyElement.Name == "KeyDataType")
                {
                    property.KeyDataType = ParsePropertyDataType(xPropertyElement.Value);
                }
                else if (xPropertyElement.Name == "EntityId")
                {
                    property.ReferencedDataType = xPropertyElement.Value;
                }
            }
            return property;
        }

        private static PropertyDataType ParsePropertyDataType(string dataTypeValue)
        {
            switch (dataTypeValue.ToLower())
            {
                case "number":
                    return PropertyDataType.Int;
                case "bool":
                    return PropertyDataType.Bool;
                case "text":
                    return PropertyDataType.String;
                case "reference":
                    return PropertyDataType.Reference;
                case "address":
                    return PropertyDataType.Address;
                case "addresspayable":
                    return PropertyDataType.AddressPayable;
                case "positivenumber":
                    return PropertyDataType.Uint;
                case "datetime":
                    return PropertyDataType.DateTime;
            }
            throw new ParseException($"Cannot convert {dataTypeValue} to a valid datatype");
        }


        private static string RemoveWhitespaces(string str)
        {
            return string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

    }
}
