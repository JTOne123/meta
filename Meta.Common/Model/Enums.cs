﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Common.Model
{
	public enum UnionEnum
	{
		INNER_JOIN = 1, LEFT_JOIN, RIGHT_JOIN, LEFT_OUTER_JOIN, RIGHT_OUTER_JOIN
	}
	public enum PipeReturnType
	{
		One = 1, List, Rows
	}
}
