﻿using Meta.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NpgsqlTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using Meta.xUnitTest.DAL;

namespace Meta.xUnitTest.Model
{
	[Mapping("student"), JsonObject(MemberSerialization.OptIn)]
	public partial class StudentModel
	{
		#region Properties
		/// <summary>
		/// 学号
		/// </summary>
		[JsonProperty] public string Stu_no { get; set; }
		[JsonProperty] public Guid Grade_id { get; set; }
		[JsonProperty] public Guid People_id { get; set; }
		[JsonProperty] public DateTime Create_time { get; set; }
		[JsonProperty] public Guid Id { get; set; }
		#endregion

		#region Foreign Key
		private ClassGradeModel _getClassGrade = null;
		public ClassGradeModel GetClassGrade => _getClassGrade ??= ClassGrade.GetItem(Grade_id);

		private PeopleModel _getPeople = null;
		public PeopleModel GetPeople => _getPeople ??= People.GetItem(People_id);
		#endregion

		#region Update/Insert
		public Student.StudentUpdateBuilder Update => DAL.Student.Update(this);

		public int Delete() => DAL.Student.Delete(this);
		public int Commit() => DAL.Student.Commit(this);
		public StudentModel Insert() => DAL.Student.Insert(this);
		#endregion

		public override string ToString() => JsonConvert.SerializeObject(this);
		public static StudentModel Parse(string json) => string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<StudentModel>(json);
	}
}
