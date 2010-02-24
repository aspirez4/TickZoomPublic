﻿#region Copyright
/*
 * Copyright 2008 M. Wayne Walter
 * Software: TickZoom Trading Platform
 * User: Wayne Walter
 * 
 * You can use and modify this software under the terms of the
 * TickZOOM General Public License Version 1.0 or (at your option)
 * any later version.
 * 
 * Businesses are restricted to 30 days of use.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * TickZOOM General Public License for more details.
 *
 * You should have received a copy of the TickZOOM General Public
 * License along with this program.  If not, see
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

using NUnit.Framework;
using System.Collections.Generic;
using TickZoom.Api;
using TickZoom.InteractiveBrokers;

namespace TickZoom.Test
{
	[TestFixture]
	public class TestOrders
	{
		private List<LogicalOrder> orders = new List<LogicalOrder>();
		private static readonly Log log = Factory.Log.GetLogger(typeof(TestOrders));
		private static readonly bool debug = log.IsDebugEnabled;		
		private Provider provider;
		protected SymbolInfo symbol;
		protected VerifyFeed verify;
		private bool localFlag = true;
			
		[TestFixtureSetUp]
		public virtual void Init()
		{
			string appData = Factory.Settings["AppDataFolder"];
			File.Delete( appData + @"\Logs\IBProviderTests.log");
			File.Delete( appData + @"\Logs\IBProviderService.log");
	  		symbol = Factory.Symbol.LookupSymbol("CSCO");
		}
		
		[TestFixtureTearDown]
		public void Dispose()
		{
		}
		
		public void CreateProvider() {
			if( localFlag) {
				provider = new IBInterface();
			} else {
				provider = Factory.Provider.ProviderProcess("127.0.0.1",6492,"IBProviderService.exe");
			}
			verify = new VerifyFeed();
			provider.Start(verify);
		}
		
		[SetUp]
		public void Setup() {
			orders.Clear();
			CreateProvider();
		}
		
		[TearDown]
		public void TearDown() { 
	  		provider.Stop(verify);	
	  		provider.Stop();	
		}
		
		[Test]
		public void TestMarkOrder() {
			if(debug) log.Debug("===DemoConnectionTest===");
  			provider.StartSymbol(verify,symbol,TimeStamp.MinValue);
  			provider.PositionChange(verify,symbol,150,null);
  			long count = verify.Verify(2,AssertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			Thread.Sleep(500);
  			provider.PositionChange(verify,symbol,0,null);
  			count = verify.Verify(2,AssertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			Thread.Sleep(500);
  			provider.PositionChange(verify,symbol,100,null);
  			count = verify.Verify(2,AssertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			Thread.Sleep(500);
  			provider.PositionChange(verify,symbol,0,null);
  			count = verify.Verify(2,AssertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
		}
		
		[Test]
		public void TestLogicalOrders() {
  			provider.StartSymbol(verify,symbol,TimeStamp.MinValue);
  			provider.PositionChange(verify,symbol,0,orders);
			CreateLogicalEntry(OrderType.BuyLimit,15.12,1000);
			CreateLogicalEntry(OrderType.SellLimit,34.12,1000);
			CreateLogicalExit(OrderType.SellLimit,40.12);
			CreateLogicalExit(OrderType.SellStop,5.12);
			CreateLogicalExit(OrderType.BuyLimit,10.12);
			CreateLogicalExit(OrderType.BuyStop,45.12);
  			provider.PositionChange(verify,symbol,0,orders);
  			long count = verify.Verify(2,AssertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			Thread.Sleep(2000);
		}
			
		
		public virtual void AssertTick( TickIO tick, TickIO lastTick, ulong symbol) {
        	Assert.IsFalse(tick.IsQuote);
        	if( tick.IsQuote) {
	        	Assert.Greater(tick.Bid,0);
	        	Assert.Greater(tick.BidLevel(0),0);
	        	Assert.Greater(tick.Ask,0);
	        	Assert.Greater(tick.AskLevel(0),0);
        	}
        	Assert.IsTrue(tick.IsTrade);
        	if( tick.IsTrade) {
	        	Assert.Greater(tick.Price,0);
    	    	Assert.Greater(tick.Size,0);
        	}
    		Assert.IsTrue(tick.Time>=lastTick.Time,"tick.Time > lastTick.Time");
    		Assert.AreEqual(symbol,tick.lSymbol);
		}
			
		public LogicalOrder CreateLogicalEntry(OrderType type, double price, int size) {
			LogicalOrder logical = Factory.Engine.LogicalOrder(symbol,null);
			logical.IsActive = true;
			logical.TradeDirection = TradeDirection.Entry;
			logical.Type = type;
			logical.Price = price;
			logical.Positions = size;
			orders.Add(logical);
			return logical;
		}
		
		public LogicalOrder CreateLogicalExit(OrderType type, double price) {
			LogicalOrder logical = Factory.Engine.LogicalOrder(symbol,null);
			logical.IsActive = true;
			logical.TradeDirection = TradeDirection.Exit;
			logical.Type = type;
			logical.Price = price;
			orders.Add(logical);
			return logical;
		}
	}
}
