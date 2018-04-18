﻿using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace DBHelper
{
	public class UpdateBuilder<T> : QueryHelper<T> where T : class, new()
	{

		protected List<string> setList = new List<string>();
		//设置字段
		protected UpdateBuilder<T> SetField(string field, NpgsqlDbType dbType, object value, int size, Type specificType = null)
		{
			var param_name = ParamsIndex;
			return SetFieldBase(param_name, dbType, value, size, $"{field} = @{param_name}", specificType);
		}
		//字段自增
		protected UpdateBuilder<T> SetFieldIncrement(string field, object increment, int size)
		{
			var param_name = ParamsIndex;
			return SetFieldBase(param_name, NpgsqlDbType.Integer, increment, size, $"{field} = COALESCE({field} , 0) + @{param_name}");
		}
		protected UpdateBuilder<T> SetFieldDateTimeOperation(string field, TimeSpan timespan, bool isPlusElseMinus, int size)
		{
			var param_name = ParamsIndex;
			return SetFieldBase(param_name, NpgsqlDbType.Interval, timespan, size, $"{field} = COALESCE({field} , now()) {(isPlusElseMinus ? "+" : "-")} @{param_name}");
		}
		//数组字段的添加
		protected UpdateBuilder<T> SetFieldJoin(string field, NpgsqlDbType dbType, object value, int size, Type specificType = null)
		{
			var param_name = ParamsIndex;
			return SetFieldBase(param_name, dbType | NpgsqlDbType.Array, value, size, $"{field} = {field} || @{param_name}", specificType);
		}
		//数组字段的移除
		protected UpdateBuilder<T> SetFieldRemove(string field, NpgsqlDbType dbType, object value, int size, Type specificType = null)
		{
			var param_name = ParamsIndex;
			return SetFieldBase(param_name, dbType, value, size, $"{field} = array_remove({field}, @{param_name})", specificType);
		}
		//自定义设置
		public UpdateBuilder<T> SetDiy(List<NpgsqlParameter> list, string sqlStr)
		{
			CommandParams.AddRange(list);
			setList.Add(sqlStr);
			return this;
		}
		//底层设置字段
		protected UpdateBuilder<T> SetFieldBase(string field, NpgsqlDbType dbType, object value, int size, string sqlStr, Type specificType = null)
		{
			if (sqlStr.IndexOf("\'") != -1) throw new Exception("可能存在注入漏洞，不允许传递 ' 给参数 value，若使用正常字符串，请使用参数化传递。");
			AddParameter(field, dbType, value, size, specificType);
			setList.Add(sqlStr);
			return this;
		}
		public int Commit()
		{
			string tableName = MappingHelper.GetMapping(typeof(T));
			string sqltext = GetSqlText(tableName);
			return PgSqlHelper.ExecuteNonQuery(CommandType.Text, sqltext, CommandParams.ToArray());
		}
		public T CommitRet()
		{
			string tableName = MappingHelper.GetMapping(typeof(T));
			string sqltext = GetSqlText(tableName, true);
			return ExecuteNonQueryReader(sqltext);
		}

		private string GetSqlText(string tableName, bool IsReturn = false)
		{
			if (WhereList.Count < 1) throw new Exception("update语句必须带where条件");
			if (Field.IndexOf("a.update_time") > 0)
				SetField("update_time", NpgsqlDbType.Timestamp, DateTime.Now, -1, null);
			var ret = IsReturn ? $"RETURNING {Field}" : "";
			return $"UPDATE {tableName} a SET {string.Join(",", setList)} WHERE {string.Join("\nAND", WhereList)} {ret};";
		}
	}
}