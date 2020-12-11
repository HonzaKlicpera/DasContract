using DasContract.Abstraction.Exceptions;
using DasContract.Abstraction.Processes.Events;
using DasContract.Abstraction.Processes.Gateways;
using DasContract.Abstraction.Processes.Tasks;
using DasContract.Abstraction.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DasContract.Abstraction.Processes
{
    public class ProcessParser
    {
        public const string BPMNNS = "{http://www.omg.org/spec/BPMN/20100524/MODEL}";
        public const string CAMNS = "{http://camunda.org/schema/1.0/bpmn}";
        public const string XMLNS = "{http://www.w3.org/2001/XMLSchema-instance}";

        public static IList<Process> ParseProcesses(IEnumerable<XElement> xProcesses)
        {
            var processes = new List<Process>();
            foreach(var xProcess in xProcesses)
            {
                processes.Add(ParseProcess(xProcess));
            }
            return processes;
        }

        public static Process ParseProcess(XElement xProcess)
        {
            var process = new Process();

            process.Id = xProcess.Element("Id").Value;
            var isExecutable = xProcess.Element("IsExecutable");
            if (isExecutable != null)
                process.IsExecutable = bool.Parse(isExecutable.Value);

            var xProcessDescendants = xProcess.Descendants();
            foreach (var xProcessDescendant in xProcessDescendants)
            {
                if (xProcessDescendant.Name == "ContractSequenceFlow")
                {
                    var sequenceFlow = CreateSequenceFlow(xProcessDescendant);
                    process.SequenceFlows.Add(sequenceFlow.Id, sequenceFlow);
                }
                else if (xProcessDescendant.Name == "ContractProcessElement")
                {
                    var processElement = CreateProcessElement(xProcessDescendant);
                    process.ProcessElements.Add(processElement.Id, processElement);
                }
            }
            return process;
        }

        static SequenceFlow CreateSequenceFlow(XElement xSequenceFlow)
        {
            var sequenceFlow = new SequenceFlow();

            var xIdElement = xSequenceFlow.Element("Id");
            var xNameElement = xSequenceFlow.Element("Name");
            var xSourceElement = xSequenceFlow.Element("SourceId");
            var xTargetElement = xSequenceFlow.Element("TargetId");

            if (xIdElement != null)
                sequenceFlow.Id = xIdElement.Value;
            else
                throw new InvalidElementException($"Sequence flow at line {ContractParser.GetLineNumber(xSequenceFlow)} is missing an ID");

            if (xSourceElement != null)
                sequenceFlow.SourceId = xSourceElement.Value;
            else
                throw new InvalidElementException($"Sequence flow at line {ContractParser.GetLineNumber(xSequenceFlow)} is missing a source");

            if (xTargetElement != null)
                sequenceFlow.TargetId = xTargetElement.Value;
            else
                throw new InvalidElementException($"Sequence flow at line {ContractParser.GetLineNumber(xSequenceFlow)} is missing a target");

            if (xNameElement != null)
            {
                sequenceFlow.Name = xNameElement.Value;
                sequenceFlow.Condition = xNameElement.Value;
            }
            return sequenceFlow;
        }


        static ProcessElement CreateProcessElement(XElement xElement)
        {
            ProcessElement processElement;
            var xElementAttribute = xElement.Attribute(XMLNS + "type");

            if (xElementAttribute == null)
                throw new InvalidElementException($"Mandatory attribute for a process element not found at line {ContractParser.GetLineNumber(xElement)}");

            switch (xElementAttribute.Value)
            {
                case "ContractBusinessRuleTask":
                    processElement = CreateBusinessRuleTask(xElement);
                    break;
                case "ContractScriptActivity":
                    processElement = CreateScriptTask(xElement);
                    break;
                case "ContractServiceActivity":
                    processElement = CreateServiceTask(xElement);
                    break;
                case "ContractUserActivity":
                    processElement = CreateUserTask(xElement);
                    break;
                case "ContractStartEvent":
                    processElement = CreateStartEvent(xElement);
                    break;
                case "ContractEndEvent":
                    processElement = CreateEndEvent(xElement);
                    break;
                case "ContractExclusiveGateway":
                    processElement = CreateExclusiveGateway(xElement);
                    break;
                case "ContractParallelGateway":
                    processElement = CreateParallelGateway(xElement);
                    break;
                case "ContractTimerBoundaryEvent":
                    processElement = CreateTimeBoundaryEvent(xElement);
                    break;
                case "ContractCallActivity":
                    processElement = CreateCallActivity(xElement);
                    break;
                default:
                    throw new InvalidElementException($"{xElementAttribute.Value} is not a valid process element type at line {ContractParser.GetLineNumber(xElement)}");
            }

            

            return processElement;
        }

        static IList<string> GetDescendantList(XElement xElement, string descendantName)
        {
            var descendants = xElement.Descendants(descendantName).First().Descendants("string");
            IList<string> descendantList = new List<string>();

            foreach (var e in descendants)
            {
                descendantList.Add(e.Value);
            }
            return descendantList;
        }

        static void FillCommonProcessElementAttributes(ProcessElement processElement, XElement xElement)
        {
            processElement.Id = GetProcessId(xElement);
            processElement.Name = RemoveWhitespaces(GetProcessName(xElement));
            processElement.Incoming = GetDescendantList(xElement, "Incoming");
            processElement.Outgoing = GetDescendantList(xElement, "Outgoing");
        }

        static void FillCommonTaskAttributes(Task task, XElement xElement)
        {
            FillCommonProcessElementAttributes(task, xElement);

            var xInstanceType = xElement.Element("InstanceType");
            if (xInstanceType == null)
                task.InstanceType = InstanceType.Single;
            else if (Enum.TryParse(xInstanceType.Value, out InstanceType parsedInstanceType))
                task.InstanceType = parsedInstanceType;
            else
                throw new InvalidElementException($"{xInstanceType.Value} is not a valid instance type value at line" +
                    $"{ContractParser.GetLineNumber(xInstanceType)}");

            var xLoopCardinality = xElement.Element("LoopCardinality");
            if(xLoopCardinality != null)
            {
                if (int.TryParse(xLoopCardinality.Value, out var parsedLoopCardinality))
                    task.LoopCardinality = parsedLoopCardinality;
                else
                    throw new InvalidElementException($"loop cardinality at {ContractParser.GetLineNumber(xInstanceType)} " +
                        $"must be an integer");
            }

            var xLoopCollection = xElement.Element("LoopCollection");
            if (xLoopCollection != null)
                task.LoopCollection = xLoopCollection.Value;
        }

        static void FillCommonPayableTaskAttributes(PayableTask payableTask, XElement xElement)
        {
            FillCommonTaskAttributes(payableTask, xElement);

            var xTokenOperationType = xElement.Element("OperationType");
            if (xTokenOperationType == null)
                payableTask.OperationType = TokenOperationType.None;
            else if (Enum.TryParse(xTokenOperationType.Value, out TokenOperationType parsedTokenOperationType))
                payableTask.OperationType = parsedTokenOperationType;
            else
                throw new InvalidElementException($"{xTokenOperationType.Value} is not a valid token operation type value at line" +
                    $"{ContractParser.GetLineNumber(xTokenOperationType)}");
        }

        static string GetAttachedToElement(XElement xElement, bool mandatory = true)
        {
            var xAttachedTo = xElement.Element("AttachedTo");
            if (xAttachedTo != null)
                return xAttachedTo.Value;
            if (mandatory)
                throw new InvalidElementException($"Element at line {ContractParser.GetLineNumber(xElement)} " +
                    $"is missing attached to definition");
            return null;
        }

        static CallActivity CreateCallActivity(XElement xElement)
        {
            var callActivity = new CallActivity();
            FillCommonTaskAttributes(callActivity, xElement);

            var xCalledElement = xElement.Element("CalledElement");
            if(xCalledElement == null)
                throw new InvalidElementException($"CallActivity at line {ContractParser.GetLineNumber(xElement)} " +
                    $"is missing called element definition");
            callActivity.CalledElement = xCalledElement.Value;

            return callActivity;
        }

        static TimerBoundaryEvent CreateTimeBoundaryEvent(XElement xElement)
        {
            var timerBoundaryEvent = new TimerBoundaryEvent();

            FillCommonProcessElementAttributes(timerBoundaryEvent, xElement);
            timerBoundaryEvent.AttachedTo = GetAttachedToElement(xElement);

            var xTimerDefinitionType = xElement.Element("DefinitionType");
            if (xTimerDefinitionType == null)
                throw new InvalidElementException($"Timer event at line {ContractParser.GetLineNumber(xElement)} " +
                    $"is missing timer definition type");
            if (Enum.TryParse(xTimerDefinitionType.Value, out TimerDefinitionType parsedDefinitionType))
                timerBoundaryEvent.TimerDefinitionType = parsedDefinitionType;
            else
                throw new InvalidElementException($"{xTimerDefinitionType.Value} is not a valid timer definition type at " +
                    $"line {ContractParser.GetLineNumber(xTimerDefinitionType)}");

            var xTimerDefinition = xElement.Element("Definition");
            if(xTimerDefinition == null)
                throw new InvalidElementException($"Timer event at line {ContractParser.GetLineNumber(xElement)} " +
                    $"is missing timer definition");
            timerBoundaryEvent.TimerDefinition = xTimerDefinition.Value;

            return timerBoundaryEvent;
        }

        static ScriptTask CreateScriptTask(XElement xElement)
        {
            var task = new ScriptTask();
            FillCommonPayableTaskAttributes(task, xElement);

            var xScript = xElement.Element("Script");
            if (xScript != null)
                task.Script = xScript.Value;
            else
                throw new InvalidElementException("script task " + task.Id + " must contain a script");

            return task;
        }

        static BusinessRuleTask CreateBusinessRuleTask(XElement xElement)
        {
            var task = new BusinessRuleTask();
            task.Id = GetProcessId(xElement);
            //TODO: Set definition
            return task;
        }

        static ServiceTask CreateServiceTask(XElement xElement)
        {
            var task = new ServiceTask();
            task.Id = GetProcessId(xElement);
            //TODO: Set implementation and configuration
            return task;
        }

        static UserTask CreateUserTask(XElement xElement)
        {
            var task = new UserTask();
            FillCommonPayableTaskAttributes(task, xElement);
            task.Assignee = GetProcessAssignee(xElement);

            var xScript = xElement.Element("Script");
            if (xScript != null)
                task.ValidationScript = xScript.Value;

            var formElement = xElement.Descendants("Form").FirstOrDefault();
            if (formElement != null)
            {
                UserForm form = new UserForm();
                form.Id = formElement.Descendants("Id").FirstOrDefault().Value;
                form.Fields = new List<FormField>();
                foreach (var f in formElement.Descendants("ContractFormField"))
                {
                    FormField field = new FormField();
                    field.Id = f.Descendants("Id").FirstOrDefault().Value;
                    field.DisplayName = RemoveWhitespaces(f.Descendants("Name").FirstOrDefault().Value);
                    var readOnly = f.Descendants("ReadOnly").FirstOrDefault().Value;
                    if (readOnly == "true") field.IsReadOnly = true;
                    else if (readOnly == "false") field.IsReadOnly = false;
                    field.PropertyExpression = f.Descendants("PropertyId").FirstOrDefault().Value;
                    form.Fields.Add(field);
                }
                task.Form = form;
            }
            return task;
        }

        static ExclusiveGateway CreateExclusiveGateway(XElement xElement)
        {
            var gateway = new ExclusiveGateway();
            FillCommonProcessElementAttributes(gateway, xElement);
            return gateway;
        }

        static ParallelGateway CreateParallelGateway(XElement xElement)
        {
            var gateway = new ParallelGateway();
            FillCommonProcessElementAttributes(gateway, xElement);
            return gateway;
        }

        static EndEvent CreateEndEvent(XElement xElement)
        {
            var endEvent = new EndEvent();
            FillCommonProcessElementAttributes(endEvent, xElement);

            return endEvent;
        }

        static StartEvent CreateStartEvent(XElement xElement)
        {
            var startEvent = new StartEvent();
            FillCommonProcessElementAttributes(startEvent, xElement);
            return startEvent;
        }

        static string GetProcessId(XElement xElement)
        {
            var xIdElement = xElement.Element("Id");
            if (xIdElement == null)
                throw new InvalidElementException($"Id not set for process element on line {ContractParser.GetLineNumber(xElement)}");
            return xIdElement.Value;
        }

        static string RemoveWhitespaces(string str)
        {
            return string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

        static string GetProcessName(XElement xElement, bool fullName=false)
        {
            var nameAttribute = xElement.Descendants("Name").FirstOrDefault();

            if (nameAttribute != null && (!nameAttribute.Value.Contains("]") || fullName))
                return nameAttribute.Value;
            else if (nameAttribute != null)
                return Regex.Replace(nameAttribute.Value.Split(']')[1], @"\s+", "");
            return "";
        }

        static ProcessUser GetProcessAssignee(XElement xElement)
        {
            ProcessUser user = new ProcessUser();
            var processName = GetProcessName(xElement, true);
            if (processName.Contains("[") && processName.Contains("]"))
            {
                string address = processName.Split('[')[1].Split(']')[0];
                if (address.StartsWith("0x"))
                    user.Address = address;
                else
                    user.Name = address;
            }
            return user;
        }

    }
}
