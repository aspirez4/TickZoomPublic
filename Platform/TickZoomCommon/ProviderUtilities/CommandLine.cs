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

namespace TickZoom.Common
{
	public class CommandLineProcess : ProviderService
	{
		ServiceConnection connection;
		
		public CommandLineProcess()
		{
		}
		
		/// <summary>
		/// Run this service.
		/// </summary>
		public void Run(string[] args)
		{
        	if( args.Length != 1) {
        		throw new ApplicationException("Command line must have one argument of the port number on which to listen.");
        	}
        	connection.SetAddress("127.0.0.1",Convert.ToUInt16(args[0]));
        	connection.OnRun();
		}
		
		public ServiceConnection Connection {
			get { return connection; }
			set { connection = value; }
		}
	}
}
