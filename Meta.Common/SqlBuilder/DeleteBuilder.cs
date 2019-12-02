﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Common.SqlBuilder
{
	public class DeleteBuilder : WhereBase<DeleteBuilder>
	{
		/// <summary>
		/// 初始化Table
		/// </summary>
		/// <param name="table"></param>
		public DeleteBuilder(string table) : base(table) { }
		public DeleteBuilder(string table, string alias) : base(table, alias) { }
		public DeleteBuilder() { }
		public new int ToRows() => base.ToRows();

		#region Override
		public override string ToString() => base.ToString();
		public override string GetCommandTextString()
		{
			if (WhereList.Count < 1) throw new ArgumentNullException(nameof(WhereList));
			return $"DELETE FROM {MainTable} {MainAlias} WHERE {string.Join("\nAND", WhereList)}";
		}
		#endregion
	}
}
