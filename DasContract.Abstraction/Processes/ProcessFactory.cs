using DasContract.Abstraction.Exceptions;
using DasContract.Abstraction.Processes.Events;
using DasContract.Abstraction.Processes.Gateways;
using DasContract.Abstraction.Processes.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DasContract.Abstraction.Processes
{
    public class ProcessFactory
    {
        public const string BPMNNS = "{http://www.omg.org/spec/BPMN/20100524/MODEL}";
        public const string CAMNS = "{http://camunda.org/schema/1.0/bpmn}";
        public static Process FromBPMN(string processXml)
        {
            return FromBPMN(XElement.Parse(processXml));
        }

        public static Process FromBPMN(XElement bpmnXElement)
        {
            Process process = new Process();
            var processElements = bpmnXElement.Descendants().ToList();

            foreach (var e in processElements)
            {
                if (e.Name == BPMNNS + "sequenceFlow")
                {
                    var sequenceFlow = CreateSequenceFlow(e);
                    process.SequenceFlows.Add(sequenceFlow.Id, sequenceFlow);
                }
                else
                {
                    var processElement = CreateProcessElement(e);
                    if (processElement != null)
                        process.ProcessElements.Add(processElement.Id, processElement);
                }

            }

            return process;

        }

        static SequenceFlow CreateSequenceFlow(XElement sequenceFlowXElement)
        {
            var sequenceFlow = new SequenceFlow();

            var idAttr = sequenceFlowXElement.Attribute("id");
            var nameAtrr = sequenceFlowXElement.Attribute("name");
            var sourceIdAttr = sequenceFlowXElement.Attribute("sourceRef");
            var targetIdAttr = sequenceFlowXElement.Attribute("targetRef");

            if (idAttr != null)
                sequenceFlow.Id = idAttr.ToString();
            else
                throw new InvalidElementException("ID must be set on every element");

            if (sourceIdAttr != null)
                sequenceFlow.SourceId = sourceIdAttr.ToString();
            else
                throw new InvalidElementException("Sequence " + idAttr + " does not have a source");

            if (targetIdAttr != null)
                sequenceFlow.TargetId = targetIdAttr.ToString();
            else
                throw new InvalidElementException("Sequence " + idAttr + " does not have a target");

            if (nameAtrr != null)
                sequenceFlow.Name = nameAtrr.ToString();

            return sequenceFlow;
        }

        static ProcessElement CreateProcessElement(XElement xElement)
        {
            ProcessElement processElement;

            switch (xElement.Name.ToString())
            {
                case BPMNNS + "businessRuleTask":
                    processElement = CreateBusinessRuleTask(xElement);
                    break;
                case BPMNNS + "scriptTask":
                    processElement = CreateScriptTask(xElement);
                    break;
                   
                default:
                    processElement = null;
                    break;
            }
            return processElement;
        }

        static ScriptTask CreateScriptTask(XElement xElement)
        {
            var task = new ScriptTask();
            task.Id = GetProcessId(xElement);

            var scriptList = xElement.Descendants(BPMNNS + "script").ToList();
            if (scriptList.Count == 1)
                task.Script = scriptList.First().Value;
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
            task.Id = GetProcessId(xElement);
            //TODO: Set Task attributes
            return task;
        }

        static ExclusiveGateway CreateExclusiveGateway(XElement xElement)
        {
            var gateway = new ExclusiveGateway();
            gateway.Id = GetProcessId(xElement);

            return gateway;
        }

        static ParallelGateway CreateParalellGateway(XElement xElement)
        {
            var gateway = new ParallelGateway();
            gateway.Id = GetProcessId(xElement);

            return gateway;
        }

        static EndEvent CreateEndEvent(XElement xElement)
        {
            var endEvent = new EndEvent();
            endEvent.Id = GetProcessId(xElement);

            return endEvent;
        }

        static StartEvent CreateStartEvent(XElement xElement)
        {
            var startEvent = new StartEvent();
            startEvent.Id = GetProcessId(xElement);
            //TODO: User Form
            return startEvent;
        }

        static string GetProcessId(XElement xElement)
        {
            if(xElement.Attribute("id") == null)
                throw new InvalidElementException("ID must be set on every element");

            return xElement.Attribute("id").ToString();
        }

    }
}
