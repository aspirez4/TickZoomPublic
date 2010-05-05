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
using TickZoom.Api;

namespace TickZoom.TickUtil
{
	/// <summary>
	/// Description of BarImport.
	/// </summary>
	public class BarWriter : TickWriterDefault
	{
		TickImpl openTick = new TickImpl();
		TickImpl highTick = new TickImpl();
		TickImpl lowTick = new TickImpl();
		TickImpl closeTick = new TickImpl();
		TimeStamp timeStamp = new TimeStamp();
		
		public BarWriter(bool eraseFileToStart) : base( eraseFileToStart) {
			
		}
		
		public void AddBar(double time, double open, double high, double low, double close, int volume, int openInterest) {
			timeStamp.dInternal = time;
			closeTick.Initialize();
			closeTick.SetTime(timeStamp);
			closeTick.SetTrade(close, volume);
			timeStamp.AddMilliseconds(-1);
			highTick.Initialize();
			highTick.SetTime(timeStamp);
			highTick.SetTrade(high, 0);
			timeStamp.AddMilliseconds(-1);
			lowTick.Initialize();
			lowTick.SetTime(timeStamp);
			lowTick.SetTrade(low, 0);
			timeStamp.AddMilliseconds(-1);
			openTick.Initialize();
			openTick.SetTime(timeStamp);
			openTick.SetTrade(open, 0);
			Add(openTick);
			Add(lowTick);
			Add(highTick);
			Add(closeTick);
		}
	}
}
