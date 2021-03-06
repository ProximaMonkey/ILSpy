﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// A merged namespace.
	/// </summary>
	public sealed class MergedNamespace : INamespace
	{
		readonly string externAlias;
		readonly ICompilation compilation;
		readonly INamespace parentNamespace;
		readonly INamespace[] namespaces;
		Dictionary<string, INamespace> childNamespaces;
		
		/// <summary>
		/// Creates a new merged root namespace.
		/// </summary>
		/// <param name="compilation">The main compilation.</param>
		/// <param name="namespaces">The individual namespaces being merged.</param>
		/// <param name="externAlias">The extern alias for this namespace.</param>
		public MergedNamespace(ICompilation compilation, INamespace[] namespaces, string externAlias = null)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (namespaces == null)
				throw new ArgumentNullException("namespaces");
			this.compilation = compilation;
			this.namespaces = namespaces;
			this.externAlias = externAlias;
		}
		
		/// <summary>
		/// Creates a new merged child namespace.
		/// </summary>
		/// <param name="parentNamespace">The parent merged namespace.</param>
		/// <param name="namespaces">The individual namespaces being merged.</param>
		public MergedNamespace(INamespace parentNamespace, INamespace[] namespaces)
		{
			if (parentNamespace == null)
				throw new ArgumentNullException("parentNamespace");
			if (namespaces == null)
				throw new ArgumentNullException("namespaces");
			this.parentNamespace = parentNamespace;
			this.namespaces = namespaces;
			this.compilation = parentNamespace.Compilation;
			this.externAlias = parentNamespace.ExternAlias;
		}
		
		public string ExternAlias {
			get { return externAlias; }
		}
		
		public string FullName {
			get { return namespaces[0].FullName; }
		}
		
		public string Name {
			get { return namespaces[0].Name; }
		}
		
		public INamespace ParentNamespace {
			get { return parentNamespace; }
		}
		
		public IEnumerable<ITypeDefinition> Types {
			get {
				return namespaces.SelectMany(ns => ns.Types);
			}
		}
		
		public ICompilation Compilation {
			get { return compilation; }
		}
		
		public IEnumerable<INamespace> ChildNamespaces {
			get { return GetChildNamespaces().Values; }
		}
		
		public INamespace GetChildNamespace(string name)
		{
			INamespace ns;
			if (GetChildNamespaces().TryGetValue(name, out ns))
				return ns;
			else
				return null;
		}
		
		Dictionary<string, INamespace> GetChildNamespaces()
		{
			var result = this.childNamespaces;
			if (result != null) {
				LazyInit.ReadBarrier();
				return result;
			} else {
				result = new Dictionary<string, INamespace>(compilation.NameComparer);
				foreach (var g in namespaces.SelectMany(ns => ns.ChildNamespaces).GroupBy(ns => ns.Name, compilation.NameComparer)) {
					result.Add(g.Key, new MergedNamespace(this, g.ToArray()));
				}
				return LazyInit.GetOrSet(ref this.childNamespaces, result);
			}
		}
		
		public ITypeDefinition GetTypeDefinition(string name, int typeParameterCount)
		{
			ITypeDefinition anyTypeDef = null;
			foreach (var ns in namespaces) {
				ITypeDefinition typeDef = ns.GetTypeDefinition(name, typeParameterCount);
				if (typeDef != null) {
					if (typeDef.IsPublic || (typeDef.IsInternal && typeDef.ParentAssembly.InternalsVisibleTo(compilation.MainAssembly))) {
						// Prefer accessible types over non-accessible types.
						return typeDef;
					}
					anyTypeDef = typeDef;
				}
			}
			return anyTypeDef;
		}
		
		public override string ToString()
		{
			return string.Format("[MergedNamespace {0}{1} (from {2} assemblies)]", externAlias != null ? externAlias + "::" : null, this.FullName, this.namespaces.Length);
		}
	}
}
