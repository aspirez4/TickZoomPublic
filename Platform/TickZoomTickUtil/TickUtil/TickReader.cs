#region Copyright
/*
 * Software: TickZoom Trading Platform
 * Copyright 2009 M. Wayne Walter
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * Business use restricted to 30 days except as otherwise stated in
 * in your Service Level Agreement (SLA).
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see <http://www.tickzoom.org/wiki/Licenses>
 * or write to Free Software Foundation, Inc., 51 Franklin Street,
 * Fifth Floor, Boston, MA  02110-1301, USA.
 * 
 */
#endregion

using System;
using System.Collections.Generic;
using TickZoom.Api;

//using TickZoom.Api;

namespace TickZoom.TickUtil
{

	/// <summary>
	/// Description of TickArray.
	/// </summary>
	public class TickReader : Reader<TickImpl>, Provider {
   		static readonly Log log = Factory.Log.GetLogger(typeof(TickReader));
   		static readonly bool debug = log.IsDebugEnabled;
   		TimeStamp startTime = TimeStamp.MinValue; 
   		TimeStamp endTime = TimeStamp.MaxValue;
   		double startDouble = double.MinValue;
   		double endDouble = double.MaxValue;
		DataReceiverDefault receiverInternal;
   		
		public TickQueue ReadQueue {
			get {
				if( receiverInternal == null) {
					if( debug) log.Debug("ReadQueue was called. Setting up internal data receiver.");
					receiverInternal = new DataReceiverDefault(this);
				}
				return receiverInternal.ReadQueue;
			}
		}
		
		public sealed override bool IsAtEnd(TickBinary tick) {
			return tick.UtcTime >= (long)endTime;
		}
		
		public sealed override bool IsAtStart(TickBinary tick) {
			return tick.UtcTime > (long)startTime;
		}
		
		public TimeStamp StartTime {
			get { return startTime; }
			set { startTime = value;
			      startDouble = startTime.Internal; }
		}
   		
		public TimeStamp EndTime {
			get { return endTime; }
			set { endTime = value;
			      endDouble = endTime.Internal; }
		}

        public void StartSymbol(Receiver receiver, SymbolInfo symbol, object eventDetail)
		{
//			receiverInternal = receiver;
//			if( receiverInternal != receiver) {
//				throw new ApplicationException( "TickReader only supports one receiver.");
//			}
        	StartSymbolDetail detail = (StartSymbolDetail) eventDetail;
        	
        	if( !symbol.Equals(Symbol)) {
				throw new ApplicationException( "Mismatching symbol.");
			}
			if( detail.LastTime != StartTime) {
				throw new ApplicationException( "Mismatching start time. Expected: " + StartTime + " but was " + detail.LastTime);
			}
		}
		
		public void StopSymbol(Receiver receiver, SymbolInfo symbol)
		{
			
		}
		
		public void PositionChange(Receiver receiver, SymbolInfo symbol, double position, IList<LogicalOrder> orders)
		{
			throw new NotImplementedException();
		}
		
		public void SendEvent( Receiver receiver, SymbolInfo symbol, int eventType, object eventDetail) {
			switch( (EventType) eventType) {
				case EventType.Connect:
					Start(receiver);
					break;
				case EventType.Disconnect:
					Stop(receiver);
					break;
				case EventType.StartSymbol:
					StartSymbol(receiver, symbol, eventDetail);
					break;
				case EventType.StopSymbol:
					StopSymbol(receiver, symbol);
					break;
				case EventType.PositionChange:
					PositionChangeDetail positionChange = (PositionChangeDetail) eventDetail;
					PositionChange(receiver,symbol,positionChange.Position,positionChange.Orders);
					break;
				case EventType.Terminate:
					Dispose();
					break;
				default:
					throw new ApplicationException("Unexpected event type: " + (EventType) eventType);
			}
		}
	}
}