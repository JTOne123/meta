﻿using Meta.Common.Extensions;
using Meta.Common.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Meta.Common.DBHelper
{
	public static class PgSqlHelper
	{
		public const string SlaveSuffix = "-slave";
		public class Execute : PgExecute
		{
			public Execute(string connectionString, ILogger logger, Action<NpgsqlConnection> mapAction)
				: base(connectionString, logger, mapAction) { }
		}
		static readonly Dictionary<string, List<Execute>> executeDictString = new Dictionary<string, List<Execute>>();
		/// <summary>
		/// 获取连接实例
		/// </summary>
		/// <param name="type">数据库类型</param>
		/// <returns>对应实例</returns>
		static PgExecute GetExecute(string type)
		{
			if (executeDictString.ContainsKey(type))
			{
				if (executeDictString[type].Count == 0)
					if (type.EndsWith(SlaveSuffix))
						return GetExecute(type.Replace(SlaveSuffix, string.Empty));

				var execute = executeDictString[type];
				if (executeDictString[type].Count == 1)
					return executeDictString[type][0];

				else if (executeDictString[type].Count > 1)
					return executeDictString[type].OrderBy(f => f.Pool.Wait.Count).First();
			}
			else if (type.EndsWith(SlaveSuffix))
				return GetExecute(type.Replace(SlaveSuffix, string.Empty));
			throw new ArgumentNullException($"not exist {type} execute");
		}
		/// <summary>
		/// 初始化一主多从数据库连接
		/// </summary>
		/// <param name="connectionString">主库</param>
		/// <param name="logger"></param>
		/// <param name="slaveConnectionString">从库</param>
		public static void InitDBConnectionOption(params BaseDbOption[] options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.Count() == 0)
				throw new ArgumentOutOfRangeException(nameof(options));
			foreach (var item in options)
				InitDB(item.ConnectionString, item.Logger, item.SlaveConnectionString, item.MapAction, item.TypeName);
		}
		private static void InitDB(string connectionString, ILogger logger, string[] slaveConnectionString, Action<NpgsqlConnection> mapAction, string type)
		{
			if (string.IsNullOrEmpty(connectionString))
				throw new ArgumentNullException($"{type} Connection String is null");
			executeDictString[type] = new List<Execute> { new Execute(connectionString, logger, mapAction) };
			if (slaveConnectionString?.Length > 0)
			{
				executeDictString[type + SlaveSuffix] = new List<Execute>();
				foreach (var item in slaveConnectionString)
					executeDictString[type + SlaveSuffix].Add(new Execute(item, logger, mapAction));
			}
		}
		/// <summary>
		/// 查询单个元素
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>返回(0,0)值</returns>
		public static object ExecuteScalar(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteScalar(cmdType, cmdText, cmdParams);
		/// <summary>
		/// 执行NonQuery
		/// </summary>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteNonQuery(cmdType, cmdText, cmdParams);
		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdType">命令类型</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		public static void ExecuteDataReader(Action<NpgsqlDataReader> action, CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			GetExecute(type).ExecuteDataReader(action, cmdType, cmdText, cmdParams);
		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master")
		{
			var list = new List<T>();
			ExecuteDataReader(dr =>
			{
				list.Add(dr.ReaderToModel<T>());
			}, cmdType, cmdText, cmdParams, type);
			return list;
		}
		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>实体</returns>
		public static T ExecuteDataReaderModel<T>(CommandType cmdType, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master")
		{
			var list = ExecuteDataReaderList<T>(cmdType, cmdText, cmdParams, type);
			return list.Count > 0 ? list[0] : default;
		}

		#region overload
		/// <summary>
		/// DataReader
		/// </summary>
		/// <param name="action">逐行Reader委托</param>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		public static void ExecuteDataReader(Action<NpgsqlDataReader> action, string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteDataReader(action, CommandType.Text, cmdText, cmdParams, type);
		/// <summary>
		/// 重构Type为Text
		/// </summary>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>修改行数</returns>
		public static int ExecuteNonQuery(string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteNonQuery(CommandType.Text, cmdText, cmdParams, type);
		/// <summary>
		/// 重构Type为Text
		/// </summary>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns></returns>
		public static object ExecuteScalar(string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteScalar(CommandType.Text, cmdText, cmdParams, type);


		/// <summary>
		/// 查询多行
		/// </summary>
		/// <typeparam name="T">列表类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>列表</returns>
		public static List<T> ExecuteDataReaderList<T>(string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteDataReaderList<T>(CommandType.Text, cmdText, cmdParams, type);

		/// <summary>
		/// 查询一行
		/// </summary>
		/// <typeparam name="T">实体类型</typeparam>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>实体</returns>
		public static T ExecuteDataReaderModel<T>(string cmdText, NpgsqlParameter[] cmdParams = null, string type = "master") =>
			ExecuteDataReaderModel<T>(CommandType.Text, cmdText, cmdParams, type);

		#endregion
		/// <summary>
		/// DataReader pipe
		/// </summary>
		/// <param name="cmdText">sql语句</param>
		/// <param name="cmdParams">sql参数</param>
		/// <param name="type">数据库类型</param>
		/// <returns>实体</returns>
		public static object[] ExecuteDataReaderPipe(CommandType cmdType, IEnumerable<IBuilder> builders, string type = "master")
		{
			if ((builders?.Count() ?? 0) == 0)
				throw new ArgumentNullException("buiders is null");
			object[] results = new object[builders.Count()];
			List<NpgsqlParameter> paras = new List<NpgsqlParameter>();
			int _paramsCount = 0;
			string ParamsIndex()
			{
				return "p" + _paramsCount++.ToString().PadLeft(6, '0');
			}
			var cmdText = string.Empty;
			foreach (var item in builders)
			{
				var itemCmdText = item.GetCommandTextString();

				foreach (var p in item.Params)
				{
					var newParaName = $"@{ParamsIndex()}";
					itemCmdText = itemCmdText.Replace($"@{p.ParameterName}", newParaName);
					p.ParameterName = newParaName;
				}
				paras.AddRange(item.Params);
				cmdText += itemCmdText + ";";
			}
			GetExecute(type).ExecuteDataReaderBase(dr =>
			{
				for (int i = 0; i < results.Length; i++)
				{
					var item = builders.ElementAt(i);
					List<object> list = new List<object>();
					while (dr.Read())
						list.Add(dr.ReaderToModel(item.Type));

					if (item.IsList)
						results[i] = list.ToArray();
					else
						results[i] = list.Count > 0 ? list[0] : null;

					dr.NextResult();
				}
			}, cmdType, cmdText, paras.ToArray());
			return results;
		}

		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <param name="action">Action委托</param>
		/// <param name="type">数据库类型</param>
		public static void Transaction(Action action, string type = "master")
		{
			try
			{
				GetExecute(type).BeginTransaction();
				action?.Invoke();
				GetExecute(type).CommitTransaction();
			}
			catch (Exception e)
			{
				GetExecute(type).RollBackTransaction();
				throw e;
			}
		}
		/// <summary>
		/// 事务 (暂不支持分布式事务)
		/// </summary>
		/// <remarks>func返回false, 则回滚事务</remarks>
		/// <param name="action">Func委托</param>
		/// <param name="type">数据库类型</param>
		public static void Transaction(Func<bool> func, string type = "master")
		{
			if (func == null)
				throw new NotSupportedException("func is require");
			try
			{
				GetExecute(type).BeginTransaction();
				bool f = func.Invoke();
				if (f == true)
					GetExecute(type).CommitTransaction();
				else
					GetExecute(type).RollBackTransaction();
			}
			catch (Exception e)
			{
				GetExecute(type).RollBackTransaction();
				throw e;
			}
		}
	}
}