﻿/*
    Copyright (C) 2011-2012 de4dot@gmail.com

    This file is part of de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using de4dot.blocks;

namespace de4dot.code.deobfuscators.Confuser {
	// Since v1.7 r74708
	class ConstantsDecrypterV17 : ConstantsDecrypterBase {
		MethodDefinition initMethod;
		ConfuserVersion version = ConfuserVersion.Unknown;
		string resourceName;

		enum ConfuserVersion {
			Unknown,
			v17_r74708_normal,
			v17_r74708_dynamic,
			v17_r74708_native,
			v17_r74788_normal,
			v17_r74788_dynamic,
			v17_r74788_native,
			v17_r74816_normal,
			v17_r74816_dynamic,
			v17_r74816_native,
			v17_r75056_normal,
			v17_r75056_dynamic,
			v17_r75056_native,
		}

		class DecrypterInfoV17 : DecrypterInfo {
			public readonly ConfuserVersion version = ConfuserVersion.Unknown;
			public uint key4, key5;

			public DecrypterInfoV17(ConfuserVersion version, MethodDefinition decryptMethod) {
				this.version = version;
				this.decryptMethod = decryptMethod;
			}

			protected override bool initializeKeys() {
				if (!findKey0(decryptMethod, out key0))
					return false;
				if (!findKey1_v17(decryptMethod, out key1))
					return false;
				if (!findKey2Key3(decryptMethod, out key2, out key3))
					return false;
				if (!findKey4(decryptMethod, out key4))
					return false;
				if (!findKey5(decryptMethod, out key5))
					return false;

				return true;
			}

			static bool findKey1_v17(MethodDefinition method, out uint key) {
				var instrs = method.Body.Instructions;
				for (int i = 0; i < instrs.Count - 4; i++) {
					var stloc = instrs[i];
					if (!DotNetUtils.isStloc(stloc))
						continue;
					var ldci4 = instrs[i + 1];
					if (!DotNetUtils.isLdcI4(ldci4))
						continue;
					var ldcloc = instrs[i + 2];
					if (!DotNetUtils.isLdloc(ldcloc))
						continue;
					if (DotNetUtils.getLocalVar(method.Body.Variables, stloc) != DotNetUtils.getLocalVar(method.Body.Variables, ldcloc))
						continue;
					if (instrs[i + 3].OpCode.Code != Code.Xor)
						continue;
					if (!DotNetUtils.isStloc(instrs[i + 4]))
						continue;

					key = (uint)DotNetUtils.getLdcI4Value(ldci4);
					return true;
				}
				key = 0;
				return false;
			}

			bool findKey4(MethodDefinition method, out uint key) {
				switch (version) {
				case ConfuserVersion.v17_r74708_normal:
				case ConfuserVersion.v17_r74788_normal:
				case ConfuserVersion.v17_r74816_normal:
				case ConfuserVersion.v17_r75056_normal:
					return findKey4_normal(method, out key);
				case ConfuserVersion.v17_r74708_dynamic:
				case ConfuserVersion.v17_r74708_native:
				case ConfuserVersion.v17_r74788_dynamic:
				case ConfuserVersion.v17_r74788_native:
				case ConfuserVersion.v17_r74816_dynamic:
				case ConfuserVersion.v17_r74816_native:
				case ConfuserVersion.v17_r75056_dynamic:
				case ConfuserVersion.v17_r75056_native:
					return findKey4_other(method, out key);
				default:
					throw new ApplicationException("Invalid version");
				}
			}

			static bool findKey4_normal(MethodDefinition method, out uint key) {
				var instrs = method.Body.Instructions;
				for (int i = 0; i < instrs.Count - 5; i++) {
					if (!DotNetUtils.isLdloc(instrs[i]))
						continue;
					if (!DotNetUtils.isLdloc(instrs[i + 1]))
						continue;
					if (instrs[i + 2].OpCode.Code != Code.Add)
						continue;
					var ldci4 = instrs[i + 3];
					if (!DotNetUtils.isLdcI4(ldci4))
						continue;
					if (instrs[i + 4].OpCode.Code != Code.Mul)
						continue;
					if (!DotNetUtils.isStloc(instrs[i + 5]))
						continue;

					key = (uint)DotNetUtils.getLdcI4Value(ldci4);
					return true;
				}
				key = 0;
				return false;
			}

			static bool findKey4_other(MethodDefinition method, out uint key) {
				var instrs = method.Body.Instructions;
				for (int i = 0; i < instrs.Count; i++) {
					int index = ConfuserUtils.findCallMethod(instrs, i, Code.Callvirt, "System.Int32 System.IO.BinaryReader::ReadInt32()");
					if (index < 0)
						break;
					if (index + 1 >= instrs.Count)
						break;
					var ldci4 = instrs[index + 1];
					if (!DotNetUtils.isLdcI4(ldci4))
						continue;

					key = (uint)DotNetUtils.getLdcI4Value(ldci4);
					return true;
				}
				key = 0;
				return false;
			}

			bool findKey5(MethodDefinition method, out uint key) {
				switch (version) {
				case ConfuserVersion.v17_r74788_normal:
				case ConfuserVersion.v17_r74788_dynamic:
				case ConfuserVersion.v17_r74788_native:
				case ConfuserVersion.v17_r74816_normal:
				case ConfuserVersion.v17_r74816_dynamic:
				case ConfuserVersion.v17_r74816_native:
				case ConfuserVersion.v17_r75056_normal:
				case ConfuserVersion.v17_r75056_dynamic:
				case ConfuserVersion.v17_r75056_native:
					return findKey5_v17_r74788(method, out key);
				default:
					key = 0;
					return true;
				}
			}

			static bool findKey5_v17_r74788(MethodDefinition method, out uint key) {
				var instrs = method.Body.Instructions;
				for (int i = 0; i < instrs.Count; i++) {
					i = ConfuserUtils.findCallMethod(instrs, i, Code.Callvirt, "System.Reflection.Module System.Reflection.Assembly::GetModule(System.String)");
					if (i < 0)
						break;
					if (i + 1 >= instrs.Count)
						break;
					var ldci4 = instrs[i + 1];
					if (!DotNetUtils.isLdcI4(ldci4))
						continue;

					key = (uint)DotNetUtils.getLdcI4Value(ldci4);
					return true;
				}
				key = 0;
				return false;
			}
		}

		public override bool Detected {
			get { return initMethod != null; }
		}

		public ConstantsDecrypterV17(ModuleDefinition module, byte[] fileData, ISimpleDeobfuscator simpleDeobfuscator)
			: base(module, fileData, simpleDeobfuscator) {
		}

		static readonly string[] requiredLocalsCctor = new string[] {
			"System.Reflection.Assembly",
			"System.IO.Compression.DeflateStream",
			"System.Byte[]",
			"System.Int32",
		};
		public void find() {
			var cctor = DotNetUtils.getModuleTypeCctor(module);
			if (cctor == null)
				return;
			if (!new LocalTypes(cctor).all(requiredLocalsCctor))
				return;

			simpleDeobfuscator.deobfuscate(cctor, true);
			if (!add(ConstantsDecrypterUtils.findDictField(cctor, cctor.DeclaringType)))
				return;
			if (!add(ConstantsDecrypterUtils.findStreamField(cctor, cctor.DeclaringType)))
				return;

			var method = getDecryptMethod();
			if (method == null)
				return;

			resourceName = getResourceName(cctor);

			if (resourceName != null)
				initVersion(method, ConfuserVersion.v17_r75056_normal, ConfuserVersion.v17_r75056_dynamic, ConfuserVersion.v17_r75056_native);
			else if (DotNetUtils.callsMethod(method, "System.String System.Reflection.Module::get_ScopeName()"))
				initVersion(method, ConfuserVersion.v17_r74816_normal, ConfuserVersion.v17_r74816_dynamic, ConfuserVersion.v17_r74816_native);
			else if (DotNetUtils.callsMethod(method, "System.Reflection.Module System.Reflection.Assembly::GetModule(System.String)"))
				initVersion(method, ConfuserVersion.v17_r74788_normal, ConfuserVersion.v17_r74788_dynamic, ConfuserVersion.v17_r74788_native);
			else
				initVersion(method, ConfuserVersion.v17_r74708_normal, ConfuserVersion.v17_r74708_dynamic, ConfuserVersion.v17_r74708_native);

			initMethod = cctor;
		}

		void initVersion(MethodDefinition method, ConfuserVersion normal, ConfuserVersion dynamic, ConfuserVersion native) {
			if (DeobUtils.hasInteger(method, 0x100) &&
				DeobUtils.hasInteger(method, 0x10000) &&
				DeobUtils.hasInteger(method, 0xFFFF))
				version = normal;
			else if ((nativeMethod = findNativeMethod(method)) == null)
				version = dynamic;
			else
				version = native;
		}

		MethodDefinition getDecryptMethod() {
			foreach (var type in module.Types) {
				if (type.Attributes != (TypeAttributes.Abstract | TypeAttributes.Sealed))
					continue;
				if (!checkMethods(type.Methods))
					continue;
				foreach (var method in type.Methods) {
					if (!DotNetUtils.isMethod(method, "System.Object", "(System.UInt32,System.UInt32)"))
						continue;

					return method;
				}
			}
			return null;
		}

		protected override byte[] decryptData(DecrypterInfo info2, MethodDefinition caller, object[] args, out byte typeCode) {
			var info = (DecrypterInfoV17)info2;
			uint offs = info.calcHash(info2.decryptMethod.MetadataToken.ToUInt32() ^ (info2.decryptMethod.DeclaringType.MetadataToken.ToUInt32() * (uint)args[0])) ^ (uint)args[1];
			reader.BaseStream.Position = offs;
			typeCode = reader.ReadByte();
			if (typeCode != info.int32Type && typeCode != info.int64Type &&
				typeCode != info.singleType && typeCode != info.doubleType &&
				typeCode != info.stringType)
				throw new ApplicationException("Invalid type code");

			var encrypted = reader.ReadBytes(reader.ReadInt32());
			return decryptConstant(info, encrypted, offs, typeCode);
		}

		byte[] decryptConstant(DecrypterInfoV17 info, byte[] encrypted, uint offs, byte typeCode) {
			switch (info.version) {
			case ConfuserVersion.v17_r74708_normal: return decryptConstant_v17_r74708_normal(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r74708_dynamic: return decryptConstant_v17_r74708_dynamic(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r74708_native: return decryptConstant_v17_r74708_native(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r74788_normal: return decryptConstant_v17_r74788_normal(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r74788_dynamic: return decryptConstant_v17_r74788_dynamic(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r74788_native: return decryptConstant_v17_r74788_native(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r74816_normal: return decryptConstant_v17_r74788_normal(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r74816_dynamic: return decryptConstant_v17_r74788_dynamic(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r74816_native: return decryptConstant_v17_r74788_native(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r75056_normal: return decryptConstant_v17_r74788_normal(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r75056_dynamic: return decryptConstant_v17_r74788_dynamic(info, encrypted, offs, typeCode);
			case ConfuserVersion.v17_r75056_native: return decryptConstant_v17_r74788_native(info, encrypted, offs, typeCode);
			default:
				throw new ApplicationException("Invalid version");
			}
		}

		byte[] decryptConstant_v17_r74708_normal(DecrypterInfoV17 info, byte[] encrypted, uint offs, byte typeCode) {
			return ConfuserUtils.decrypt(info.key4 * (offs + typeCode), encrypted);
		}

		byte[] decryptConstant_v17_r74708_dynamic(DecrypterInfoV17 info, byte[] encrypted, uint offs, byte typeCode) {
			return decryptConstant_v17_r73740_dynamic(info, encrypted, offs, info.key4);
		}

		byte[] decryptConstant_v17_r74708_native(DecrypterInfoV17 info, byte[] encrypted, uint offs, byte typeCode) {
			return decryptConstant_v17_r73764_native(info, encrypted, offs, info.key4);
		}

		byte[] decryptConstant_v17_r74788_normal(DecrypterInfoV17 info, byte[] encrypted, uint offs, byte typeCode) {
			return ConfuserUtils.decrypt(info.key4 * (offs + typeCode), encrypted, getKey_v17_r74788(info));
		}

		byte[] decryptConstant_v17_r74788_dynamic(DecrypterInfoV17 info, byte[] encrypted, uint offs, byte typeCode) {
			return decryptConstant_v17_r73740_dynamic(info, encrypted, offs, info.key4, getKey_v17_r74788(info));
		}

		byte[] decryptConstant_v17_r74788_native(DecrypterInfoV17 info, byte[] encrypted, uint offs, byte typeCode) {
			return decryptConstant_v17_r73764_native(info, encrypted, offs, info.key4, getKey_v17_r74788(info));
		}

		byte[] getKey_v17_r74788(DecrypterInfoV17 info) {
			return module.GetSignatureBlob(info.decryptMethod.MetadataToken.ToUInt32() ^ info.key5);
		}

		public override void initialize() {
			if (resourceName != null)
				resource = DotNetUtils.getResource(module, resourceName) as EmbeddedResource;
			else
				resource = findResource(initMethod);
			if (resource == null)
				throw new ApplicationException("Could not find encrypted consts resource");

			findDecrypterInfos();
			initializeDecrypterInfos();

			setConstantsData(DeobUtils.inflate(resource.GetResourceData(), true));
		}

		void findDecrypterInfos() {
			foreach (var type in module.Types) {
				if (type.Attributes != (TypeAttributes.Abstract | TypeAttributes.Sealed))
					continue;
				if (!checkMethods(type.Methods))
					continue;
				foreach (var method in type.Methods) {
					if (!DotNetUtils.isMethod(method, "System.Object", "(System.UInt32,System.UInt32)"))
						continue;

					var info = new DecrypterInfoV17(version, method);
					add(info);
				}
			}
		}

		static bool checkMethods(IEnumerable<MethodDefinition> methods) {
			int numMethods = 0;
			foreach (var method in methods) {
				if (method.Name == ".ctor" || method.Name == ".cctor")
					return false;
				if (method.Attributes != (MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.CompilerControlled))
					return false;
				if (!DotNetUtils.isMethod(method, "System.Object", "(System.UInt32,System.UInt32)"))
					return false;

				numMethods++;
			}
			return numMethods > 0;
		}

		static string getResourceName(MethodDefinition method) {
			var instrs = method.Body.Instructions;
			for (int i = 0; i < instrs.Count; i++) {
				i = ConfuserUtils.findCallMethod(instrs, i, Code.Call, "System.Byte[] System.BitConverter::GetBytes(System.Int32)");
				if (i < 0)
					break;
				if (i == 0)
					continue;
				var ldci4 = instrs[i - 1];
				if (!DotNetUtils.isLdcI4(ldci4))
					continue;
				return Encoding.UTF8.GetString(BitConverter.GetBytes(DotNetUtils.getLdcI4Value(ldci4)));
			}
			return null;
		}
	}
}
