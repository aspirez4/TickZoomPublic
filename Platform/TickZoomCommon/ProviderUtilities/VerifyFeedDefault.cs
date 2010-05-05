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
using System.Threading;

using TickZoom.Api;

namespace TickZoom.Common
{
	public class VerifyFeedDefault : Receiver, VerifyFeed, IDisposable
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(VerifyFeed));
		private static readonly bool debug = log.IsDebugEnabled;
		private TickQueue tickQueue = Factory.TickUtil.TickQueue(typeof(VerifyFeed));
		private volatile bool isRealTime = false;
		private SimpleLock syncTicks;
		private volatile ReceiverState receiverState = ReceiverState.Ready;
		private Task task;
		private static object taskLocker = new object();

		public TickQueue TickQueue {
			get { return tickQueue; }
		}

		public ReceiverState OnGetReceiverState(SymbolInfo symbol)
		{
			return receiverState;
		}

		public VerifyFeedDefault()
		{
			tickQueue.StartEnqueue = Start;
		}

		public void Start()
		{
		}
		
		public long VerifyEvent(Action<SymbolInfo,int,object> assertTick, SymbolInfo symbol, int timeout)
		{
			return VerifyEvent(1, assertTick, symbol, timeout);
		}
		
		public long Verify(Action<TickIO, TickIO, ulong> assertTick, SymbolInfo symbol, int timeout)
		{
			return Verify(2, assertTick, symbol, timeout);
		}
		TickIO lastTick = Factory.TickUtil.TickIO();
		
		int countLog = 0;
		TickBinary tickBinary = new TickBinary();
		TickIO tickIO = Factory.TickUtil.TickIO();
		public long Verify(int expectedCount, Action<TickIO, TickIO, ulong> assertTick, SymbolInfo symbol, int timeout)
		{
			if (debug) log.Debug("VerifyFeed");
			syncTicks = SyncTicks.GetTickSync(symbol.BinaryIdentifier);
			int startTime = Environment.TickCount;
			count = 0;
			while (Environment.TickCount - startTime < timeout * 1000) {
				try { 
					if( tickQueue.TryDequeue(ref tickBinary)) {
						tickIO.Inject(tickBinary);
						if (debug && countLog < 5) {
							log.Debug("Received a tick " + tickIO);
							countLog++;
						}
						startTime = Environment.TickCount;
						count++;
						if (count > 0) {
							assertTick(tickIO, lastTick, symbol.BinaryIdentifier);
						}
						lastTick.Copy(tickIO);
						syncTicks.Unlock();
						if (count >= expectedCount) {
							break;
						}
					} else {
						Thread.Sleep(100);
					}
				} catch (QueueException ex) {
					if( HandleQueueException(ex)) {
						break;
					}
				}
			}
			return count;
		}
		
		public ReceiverState VerifyState(ReceiverState expectedState, SymbolInfo symbol, int timeout) {
			if (debug) log.Debug("VerifyFeed");
			syncTicks = SyncTicks.GetTickSync(symbol.BinaryIdentifier);
			int startTime = Environment.TickCount;
			count = 0;
			TickBinary binary = new TickBinary();
			while (Environment.TickCount - startTime < timeout * 1000) {
				try { 
					if( !tickQueue.TryDequeue(ref binary)) {
						Thread.Sleep(100);
					}
				} catch (QueueException ex) {
					if( HandleQueueException(ex)) {
						break;
					}
				}
				if( receiverState == expectedState) {
					break;
				}
			}
			return receiverState;
		}
		
		public long VerifyEvent(int expectedCount, Action<SymbolInfo,int,object> assertEvent, SymbolInfo symbol, int timeout)
		{
			if (debug) log.Debug("VerifyEvent");
			int startTime = Environment.TickCount;
			count = 0;
			while (Environment.TickCount - startTime < timeout * 1000) {
				try {
					// Remove ticks just so as to get to the event we want to see.
					if( tickQueue.TryDequeue(ref tickBinary)) {
						if (customEventType> 0) {
							assertEvent(customEventSymbol,customEventType,customEventDetail);
							count++;
						} else {
							Thread.Sleep(10);
						}
						if (count >= expectedCount) {
							break;
						}
					} else {
						Thread.Sleep(100);
					}
				} catch (QueueException ex) {
					if( HandleQueueException(ex)) {
						break;
					}
				}
			}
			return count;
		}
		
		public double VerifyPosition(double expectedPosition, SymbolInfo symbol, int timeout)
		{
			if (debug)
				log.Debug("VerifyFeed");
			int startTime = Environment.TickCount;
			count = 0;
			double position;
			TickBinary binary = new TickBinary();
			while (Environment.TickCount - startTime < timeout * 1000) {
				try { 
					if( !tickQueue.TryDequeue(ref binary)) {
						Thread.Sleep(10);
					}
				} catch (QueueException ex) {
					if( HandleQueueException(ex)) {
						break;
					}
				}
				if( actualPositions.TryGetValue(symbol.BinaryIdentifier,out position)) {
					if( position == expectedPosition) {
						return expectedPosition;
					}
				}
			}
			if( actualPositions.TryGetValue(symbol.BinaryIdentifier,out position)) {
				return position;
			} else {
				throw new ApplicationException("Position was never set via call back.");
			}
		}

		private bool HandleQueueException( QueueException ex) {
			log.Notice("QueueException: " + ex.EntryType);
			switch (ex.EntryType) {
				case EventType.StartHistorical:
					receiverState = ReceiverState.Historical;
					isRealTime = false;
					break;
				case EventType.StartRealTime:
					receiverState = ReceiverState.RealTime;
					isRealTime = true;
					break;
				case EventType.EndHistorical:
					receiverState = ReceiverState.Ready;
					isRealTime = false;
					break;
				case EventType.EndRealTime:
					receiverState = ReceiverState.Ready;
					isRealTime = false;
					break;
				case EventType.Terminate:
					receiverState = ReceiverState.Stop;
					isRealTime = false;
					return true;
				default:
					throw new ApplicationException("Unexpected QueueException: " + ex.EntryType);
			}
			return false;
		}
		
		volatile int count = 0;
		int startTime;
		public void StartTimeTheFeed()
		{
			startTime = Environment.TickCount;
			count = 0;
			countLog = 0;
			task = Factory.Parallel.Loop(this, TimeTheFeedTask);
		}

		public int EndTimeTheFeed(int expectedTickCount, int timeoutSeconds)
		{
			int end = startTime + timeoutSeconds * 1000;
			while( count < expectedTickCount && Environment.TickCount < end) {
				Thread.Sleep(100);
			}
			log.Notice("Last tick received: " + tickIO.ToPosition());
			Factory.TickUtil.TickQueue("Stats").LogStats();
			Dispose();
			return count;
		}

		public Yield TimeTheFeedTask()
		{
			lock(taskLocker) {
				if( isDisposed) {
					return Yield.Terminate;
				}
				try {
					if (!tickQueue.TryDequeue(ref tickBinary)) {
						return Yield.NoWork.Repeat;
					}
#if DEBUG					
					if( isRealTime && count % 10  == 0) {
#else
					if( isRealTime ) {
#endif
						Thread.Sleep(2);
					}
					tickIO.Inject(tickBinary);
					if (debug && count < 5) {
						log.Debug("Received a tick " + tickIO);
						countLog++;
					}
					if( count == 0) {
						log.Notice("First tick received: " + tickIO.ToPosition());
					}
					count++;
					if (count % 1000000 == 0) {
						log.Notice("Read " + count + " ticks");
					}
					return Yield.DidWork.Repeat;
				} catch (QueueException ex) {
					HandleQueueException(ex);
				}
				return Yield.NoWork.Repeat;
			}
		}

		public bool OnRealTime(SymbolInfo symbol)
		{
			try {
				return tickQueue.TryEnQueue(EventType.StartRealTime, symbol);
			} catch (QueueException) {
				// Queue was already ended.
			}
			return true;
		}

		public bool OnHistorical(SymbolInfo symbol)
		{
			try {
				return tickQueue.TryEnQueue(EventType.StartHistorical, symbol);
			} catch (QueueException) {
				// Queue was already ended.
			}
			return true;
		}

		public bool OnSend(ref TickBinary o)
		{
			try {
				return tickQueue.TryEnQueue(ref o);
			} catch (QueueException) {
				// Queue already terminated.
			}
			return true;
		}

		Dictionary<ulong, double> actualPositions = new Dictionary<ulong, double>();

		public double GetPosition(SymbolInfo symbol)
		{
			return actualPositions[symbol.BinaryIdentifier];
		}

		public bool OnPositionChange(SymbolInfo symbol, LogicalFillBinary fill)
		{
			log.Info("Got Logical Fill of " + symbol + " at " + fill.Price + " for " + fill.Position);
			actualPositions[symbol.BinaryIdentifier] = fill.Position;
			return true;
		}

		public bool OnStop()
		{
			Dispose();
			return true;
		}

		public bool OnError(string error)
		{
			log.Error(error);
			Dispose();
			return true;
		}
		
 		private volatile bool isDisposed = false;
	    public void Dispose() 
	    {
	        Dispose(true);
	        GC.SuppressFinalize(this);      
	    }
	
	    protected virtual void Dispose(bool disposing)
	    {
       		if( !isDisposed) {
	    		lock( taskLocker) {
		            isDisposed = true;   
		            if (disposing) {
		            	if( task != null) {
			            	task.Stop();
			            	task.Join();
							tickQueue.Terminate();
		            	}
		            }
		            task = null;
		            // Leave tickQueue set so any extraneous
		            // events will see the queue is already terminated.
//		            tickQueue = null;
	    		}
    		}
	    }
	    
		public bool OnEndHistorical(SymbolInfo symbol)
		{
			try {
				return tickQueue.TryEnQueue(EventType.EndHistorical, symbol);
			} catch (QueueException) {
				// Queue was already ended.
			}
			return true;
		}

		public bool OnEndRealTime(SymbolInfo symbol)
		{
			try {
				return tickQueue.TryEnQueue(EventType.EndRealTime, symbol);
			} catch (QueueException) {
				// Queue was already ended.
			}
			return true;
		}
		
		public bool IsRealTime {
			get { return isRealTime; }
		}
		
		volatile SymbolInfo customEventSymbol;
		volatile int customEventType;
		volatile object customEventDetail;
		public bool OnCustomEvent(SymbolInfo symbol, int eventType, object eventDetail) {
			customEventSymbol = symbol;
			customEventType = eventType;
			customEventDetail = eventDetail;
			return true;
		}
		
		public bool OnEvent(SymbolInfo symbol, int eventType, object eventDetail) {
			if( isDisposed) return false;
			bool result = false;
			try {
				switch( (EventType) eventType) {
					case EventType.Tick:
						TickBinary binary = (TickBinary) eventDetail;
						result = OnSend(ref binary);
						break;
					case EventType.EndHistorical:
						result = OnEndHistorical(symbol);
						break;
					case EventType.StartRealTime:
						result = OnRealTime(symbol);
						break;
					case EventType.StartHistorical:
						result = OnHistorical(symbol);
						break;
					case EventType.EndRealTime:
						result = OnEndRealTime(symbol);
						break;
					case EventType.Error:
						result = OnError((string)eventDetail);
						break;
					case EventType.LogicalFill:
						result = OnPositionChange(symbol,(LogicalFillBinary)eventDetail);
						break;
					case EventType.Terminate:
						result = OnStop();
			    		break;
					case EventType.Initialize:
					case EventType.Open:
					case EventType.Close:
					case EventType.PositionChange:
			    		throw new ApplicationException("Unexpected EventType: " + eventType);
					default:
			    		result = OnCustomEvent(symbol,eventType,eventDetail);
			    		break;
				}
				return result;
			} catch( QueueException) {
				log.Warn("Already terminated.");
			}
			return false;
		}
		
		public TickIO LastTick {
			get { return lastTick; }
		}
	}
}
