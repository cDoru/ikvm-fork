﻿/*
  Copyright (C) 2008 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IKVM.Reflection.Emit.Writer;

namespace IKVM.Reflection.Emit
{
	public class SignatureHelper
	{
		internal const byte DEFAULT = 0x00;
		internal const byte GENERIC = 0x10;
		internal const byte HASTHIS = 0x20;
		internal const byte FIELD = 0x06;
		internal const byte PROPERTY = 0x08;
		internal const byte ELEMENT_TYPE_VOID = 0x01;
		internal const byte ELEMENT_TYPE_BOOLEAN = 0x02;
		internal const byte ELEMENT_TYPE_CHAR = 0x03;
		internal const byte ELEMENT_TYPE_I1 = 0x04;
		internal const byte ELEMENT_TYPE_U1 = 0x05;
		internal const byte ELEMENT_TYPE_I2 = 0x06;
		internal const byte ELEMENT_TYPE_U2 = 0x07;
		internal const byte ELEMENT_TYPE_I4 = 0x08;
		internal const byte ELEMENT_TYPE_U4 = 0x09;
		internal const byte ELEMENT_TYPE_I8 = 0x0a;
		internal const byte ELEMENT_TYPE_U8 = 0x0b;
		internal const byte ELEMENT_TYPE_R4 = 0x0c;
		internal const byte ELEMENT_TYPE_R8 = 0x0d;
		internal const byte ELEMENT_TYPE_STRING = 0x0e;
		internal const byte ELEMENT_TYPE_BYREF = 0x10;
		internal const byte ELEMENT_TYPE_VALUETYPE = 0x11;
		internal const byte ELEMENT_TYPE_CLASS = 0x12;
		internal const byte ELEMENT_TYPE_VAR = 0x13;
		internal const byte ELEMENT_TYPE_GENERICINST = 0x15;
		internal const byte ELEMENT_TYPE_I = 0x18;
		internal const byte ELEMENT_TYPE_OBJECT = 0x1c;
		internal const byte ELEMENT_TYPE_SZARRAY = 0x1d;
		internal const byte ELEMENT_TYPE_MVAR = 0x1e;
		internal const byte ELEMENT_TYPE_CMOD_REQD = 0x1f;
		internal const byte ELEMENT_TYPE_CMOD_OPT = 0x020;

		private enum GenericParameterType
		{
			None,
			Method,
			Type
		}

		internal static void WriteType(ModuleBuilder moduleBuilder, ByteBuffer bb, Type type)
		{
			WriteType(moduleBuilder, bb, type, GenericParameterType.None);
		}

		private static void WriteType(ModuleBuilder moduleBuilder, ByteBuffer bb, Type type, GenericParameterType genericParameter)
		{
			while (type.IsArray)
			{
				// check for non-szarrays
				if (!type.FullName.EndsWith("[]", StringComparison.Ordinal))
				{
					throw new NotImplementedException();
				}
				bb.Write(ELEMENT_TYPE_SZARRAY);
				type = type.GetElementType();
			}
			while (type.IsByRef)
			{
				bb.Write(ELEMENT_TYPE_BYREF);
				type = type.GetElementType();
			}
			if (type == typeof(void))
			{
				bb.Write(ELEMENT_TYPE_VOID);
			}
			else if (type == typeof(bool))
			{
				bb.Write(ELEMENT_TYPE_BOOLEAN);
			}
			else if (type == typeof(char))
			{
				bb.Write(ELEMENT_TYPE_CHAR);
			}
			else if (type == typeof(sbyte))
			{
				bb.Write(ELEMENT_TYPE_I1);
			}
			else if (type == typeof(byte))
			{
				bb.Write(ELEMENT_TYPE_U1);
			}
			else if (type == typeof(short))
			{
				bb.Write(ELEMENT_TYPE_I2);
			}
			else if (type == typeof(ushort))
			{
				bb.Write(ELEMENT_TYPE_U2);
			}
			else if (type == typeof(int))
			{
				bb.Write(ELEMENT_TYPE_I4);
			}
			else if (type == typeof(uint))
			{
				bb.Write(ELEMENT_TYPE_U4);
			}
			else if (type == typeof(long))
			{
				bb.Write(ELEMENT_TYPE_I8);
			}
			else if (type == typeof(ulong))
			{
				bb.Write(ELEMENT_TYPE_U8);
			}
			else if (type == typeof(float))
			{
				bb.Write(ELEMENT_TYPE_R4);
			}
			else if (type == typeof(double))
			{
				bb.Write(ELEMENT_TYPE_R8);
			}
			else if (type == typeof(string))
			{
				bb.Write(ELEMENT_TYPE_STRING);
			}
			else if (type == typeof(IntPtr))
			{
				bb.Write(ELEMENT_TYPE_I);
			}
			else if (type == typeof(object))
			{
				bb.Write(ELEMENT_TYPE_OBJECT);
			}
			else if (type.IsGenericParameter)
			{
				switch (genericParameter)
				{
					case GenericParameterType.Type:
						bb.Write(ELEMENT_TYPE_VAR);
						bb.WriteCompressedInt(type.GenericParameterPosition);
						break;
					case GenericParameterType.Method:
						bb.Write(ELEMENT_TYPE_MVAR);
						bb.WriteCompressedInt(type.GenericParameterPosition);
						break;
					default:
						throw new InvalidOperationException();
				}
			}
			else if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				WriteGenericSignature(moduleBuilder, bb, type.GetGenericTypeDefinition(), type.GetGenericArguments());
			}
			else if (!type.IsPrimitive)
			{
				if (type.IsValueType)
				{
					bb.Write(ELEMENT_TYPE_VALUETYPE);
				}
				else
				{
					bb.Write(ELEMENT_TYPE_CLASS);
				}
				bb.WriteTypeDefOrRefEncoded(moduleBuilder.GetTypeToken(type).Token);
			}
			else
			{
				throw new NotImplementedException(type.FullName);
			}
		}

		internal static void WriteCustomModifiers(ModuleBuilder moduleBuilder, ByteBuffer bb, byte mod, Type[] modifiers)
		{
			if (modifiers != null)
			{
				foreach (Type type in modifiers)
				{
					bb.Write(mod);
					bb.WriteTypeDefOrRefEncoded(moduleBuilder.GetTypeToken(type).Token);
				}
			}
		}

		internal static void WriteFieldSig(ModuleBuilder moduleBuilder, ByteBuffer bb, Type fieldType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		{
			bb.Write(FIELD);
			WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_REQD, requiredCustomModifiers);
			WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_OPT, optionalCustomModifiers);
			WriteType(moduleBuilder, bb, fieldType);
		}

		internal static void WriteMethodSig(ModuleBuilder moduleBuilder, ByteBuffer bb, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
		{
			if ((callingConvention & ~CallingConventions.HasThis) != CallingConventions.Standard)
			{
				throw new NotImplementedException();
			}
			byte first = DEFAULT;
			if ((callingConvention & CallingConventions.HasThis) != 0)
			{
				first |= HASTHIS;
			}
			parameterTypes = parameterTypes ?? Type.EmptyTypes;
			bb.Write(first);
			bb.WriteCompressedInt(parameterTypes.Length);
			// RetType
			WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_REQD, returnTypeRequiredCustomModifiers);
			WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_OPT, returnTypeOptionalCustomModifiers);
			WriteType(moduleBuilder, bb, returnType ?? typeof(void));
			// Param
			for (int i = 0; i < parameterTypes.Length; i++)
			{
				if (parameterTypeRequiredCustomModifiers != null && parameterTypeRequiredCustomModifiers.Length > i)
				{
					WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_REQD, parameterTypeRequiredCustomModifiers[i]);
				}
				if (parameterTypeOptionalCustomModifiers != null && parameterTypeOptionalCustomModifiers.Length > i)
				{
					WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_OPT, parameterTypeOptionalCustomModifiers[i]);
				}
				WriteType(moduleBuilder, bb, parameterTypes[i]);
			}
		}

		private static MethodBase GetMethodOnTypeDefinition(MethodBase method)
		{
			Type type = method.DeclaringType;
			if (type != null && type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				// this trick allows us to go from the method on the generic type instance, to the equivalent method on the generic type definition
				method = method.Module.ResolveMethod(method.MetadataToken);
			}
			return method;
		}

		internal static void WriteMethodSig(ModuleBuilder moduleBuilder, ByteBuffer bb, MethodBase methodOnTypeInstance)
		{
			Debug.Assert(!methodOnTypeInstance.IsGenericMethod || methodOnTypeInstance.IsGenericMethodDefinition);
			MethodBase method = GetMethodOnTypeDefinition(methodOnTypeInstance);
			ParameterInfo returnParameter = null;
			ParameterInfo[] parameters = method.GetParameters();
			if (method is MethodInfo)
			{
				returnParameter = ((MethodInfo)method).ReturnParameter;
			}
			bool methodIsGeneric = false;
			ParameterInfo methodOnTypeInstanceReturnParameter = returnParameter;
			ParameterInfo[] methodOnTypeInstanceParameters = parameters;
			if (methodOnTypeInstance.IsGenericMethodDefinition)
			{
				methodIsGeneric = true;
				methodOnTypeInstanceReturnParameter = ((MethodInfo)methodOnTypeInstance).ReturnParameter;
				methodOnTypeInstanceParameters = methodOnTypeInstance.GetParameters();
			}
			byte first = DEFAULT;
			if (!method.IsStatic)
			{
				first |= HASTHIS;
			}
			if (method.IsGenericMethod)
			{
				first |= GENERIC;
			}
			bb.Write(first);
			if (method.IsGenericMethod)
			{
				bb.WriteCompressedInt(method.GetGenericArguments().Length);
			}
			bb.WriteCompressedInt(parameters.Length);
			// RetType
			if (returnParameter == null)
			{
				WriteType(moduleBuilder, bb, typeof(void));
			}
			else
			{
				WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_REQD, returnParameter.GetRequiredCustomModifiers());
				WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_OPT, returnParameter.GetOptionalCustomModifiers());
				WriteMethodParameterType(moduleBuilder, bb, returnParameter.ParameterType, methodOnTypeInstanceReturnParameter.ParameterType, methodIsGeneric);
			}
			// Param
			for (int i = 0; i < parameters.Length; i++)
			{
				WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_REQD, parameters[i].GetRequiredCustomModifiers());
				WriteCustomModifiers(moduleBuilder, bb, ELEMENT_TYPE_CMOD_OPT, parameters[i].GetOptionalCustomModifiers());
				WriteMethodParameterType(moduleBuilder, bb, parameters[i].ParameterType, methodOnTypeInstanceParameters[i].ParameterType, methodIsGeneric);
			}
		}

		private static bool IsGenericParameter(Type type)
		{
			while (type.HasElementType)
			{
				type = type.GetElementType();
			}
			return type.IsGenericParameter;
		}

		private static void WriteMethodParameterType(ModuleBuilder moduleBuilder, ByteBuffer bb, Type type1, Type type2, bool methodIsGeneric)
		{
			if (methodIsGeneric && IsGenericParameter(type2))
			{
				WriteType(moduleBuilder, bb, type1, GenericParameterType.Method);
			}
			else if (IsGenericParameter(type1))
			{
				WriteType(moduleBuilder, bb, type1, GenericParameterType.Type);
			}
			else
			{
				WriteType(moduleBuilder, bb, type1);
			}
		}

		internal static void WriteGenericSignature(ModuleBuilder moduleBuilder, ByteBuffer bb, Type type, Type[] typeArguments)
		{
			Debug.Assert(type.IsGenericTypeDefinition);
			bb.Write(ELEMENT_TYPE_GENERICINST);
			WriteType(moduleBuilder, bb, type);
			bb.WriteCompressedInt(typeArguments.Length);
			foreach (Type t in typeArguments)
			{
				WriteType(moduleBuilder, bb, t);
			}
		}
	}
}