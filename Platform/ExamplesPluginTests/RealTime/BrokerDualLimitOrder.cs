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
using System.Configuration;
using Loaders;
using NUnit.Framework;
using System.IO;
using TickZoom;
using TickZoom.Api;
using TickZoom.Common;
using ZedGraph;

namespace MockProvider
{

	
	[TestFixture]
	public class BrokerDualLimitOrder : DualStrategyLimitOrder {
		
		public BrokerDualLimitOrder() {
			ConfigurationManager.AppSettings.Set("ProviderAddress","InProcess");
			SyncTicks.Enabled = true;
			DeleteFiles();
			CreateStarterCallback = CreateStarter;
			Symbols = "USD/JPY,EUR/USD";
			MatchTestResultsOf(typeof(DualStrategyLimitOrder));
			
//			BreakPoint.SetEngineConstraint();
//			BreakPoint.SetTickBreakPoint("2009-06-09 10:49:21.502");
//			BreakPoint.SetBarBreakPoint(15);
//			BreakPoint.SetSymbolConstraint("EUR/USD");
			ShowCharts = false;
			StoreKnownGood = false;
		}
		
		public Starter CreateStarter()
		{
			return new RealTimeStarter();
		}
		
		private void DeleteFiles() {
			while( true) {
				try {
					string appData = Factory.Settings["AppDataFolder"];
		 			File.Delete( appData + @"\TestServerCache\EURUSD_Tick.tck");
		 			File.Delete( appData + @"\TestServerCache\USDJPY_Tick.tck");
					break;
				} catch( Exception) {
				}
			}
		}
		
		[Test]
		public void CheckMockTradeCount() {
			Assert.AreEqual(77,SyncTicks.MockTradeCount);
		}
	}
}
