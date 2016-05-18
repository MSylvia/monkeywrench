/*
 * DBHostTag.cs
 *
 * Authors:
 *   Matt Sylvia (matthew.sylvia@xamarin.com)
 *   
 * Copyright 2016 Xamarin Inc. (http://www.xamarin.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */


/*
 * This file has been generated. 
 * If you modify it you'll loose your changes.
 */


using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

#pragma warning disable 649

namespace MonkeyWrench.DataClasses
{
	public partial class DBHostTag : DBRecord
	{
		public const string TableName = "HostTag";

		public DBHostTag()
		{
		}

		public DBHostTag(IDataReader reader)
			: base(reader)
		{
		}
	}
}

