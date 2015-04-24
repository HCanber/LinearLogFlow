using System;
using System.Collections.Generic;
using System.Linq;
using LinearLogFlow.Properties;
using LogFlow;
using NLog;

namespace LinearLogFlow
{
	// ReSharper disable once UnusedMember.Global
	public class FlowFactoryStarter : IFlowFactory
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		public IEnumerable<Flow> CreateFlows()
		{
			var typeName = Settings.Default.FlowFactory;
			var flowFactoryType = Type.GetType(typeName);
			if(flowFactoryType == null)
			{
				_log.Error("Unable to create the {0} type: {1}", typeof(IFlowFactory).Name, typeName);
				return Enumerable.Empty<Flow>();
			}
			if(flowFactoryType.GetConstructor(new Type[0]) == null)
			{
				_log.Error("Unable to create the {0} type: {1}. It's missing a public default constructor.", typeof(IFlowFactory).Name, typeName);
				return Enumerable.Empty<Flow>();
			}
			_log.Debug("Creating the {0}: {1}", typeof(IFlowFactory).Name, typeName);
			var flowFactory = (IFlowFactory)Activator.CreateInstance(flowFactoryType);

			_log.Trace("Calling CreateFlows on {0}", typeName);
			return flowFactory.CreateFlows();
		}
	}
}