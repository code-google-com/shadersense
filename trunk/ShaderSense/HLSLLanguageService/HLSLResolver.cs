/**************************************************
 * 
 * Copyright 2009 Garrett Kiel, Cory Luitjohan, Feng Cao, Phil Slama, Ed Han, Michael Covert
 * 
 * This file is part of Shader Sense.
 *
 *   Shader Sense is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Shader Sense is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with Shader Sense.  If not, see <http://www.gnu.org/licenses/>.
 *
 *************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Babel
{
    /* HLSLResolver
     * Uses the data from the most recent parse to determine what sorts of values match various criteria.
     * For example, FindCompletions returns all values that might be used as a completion.
     */
	public class HLSLResolver : Babel.IASTResolver
	{
		#region IASTResolver Members
        public Source _source;

        //Gets a list of things to put in the auto-completion list, including keywords, intrinsics,
        //variables, types, and functions
		public IList<Babel.HLSLDeclaration> FindCompletions(object result, int line, int col)
        {
            //  Preparation
            List<Babel.HLSLDeclaration> declarations = new List<Babel.HLSLDeclaration>();
            string currentText = result.ToString();

            //  Adding predefined keyword commands
            foreach (string command in Babel.Lexer.Scanner.Commands)
            {
                if (currentText == string.Empty || command.StartsWith(currentText, StringComparison.CurrentCultureIgnoreCase))
                {

                    declarations.Add(new Babel.HLSLDeclaration(Babel.Lexer.Scanner.GetDescriptionForTokenValue(command), command, 0, command));
                }
            }
            // Add predefined intrinsics
            foreach (string intrin in Babel.Lexer.Scanner.Intrinsics)
            {
                if (currentText == string.Empty || intrin.StartsWith(currentText, StringComparison.CurrentCultureIgnoreCase))
                {
                    declarations.Add(new Babel.HLSLDeclaration(Babel.Lexer.Scanner.GetDescriptionForTokenValue(intrin), intrin, 6 * 25, intrin));
                }
            }

            //  Add variable declarations
            Parser.Parser.CodeScope curCS = HLSLScopeUtils.GetCurrentScope(Parser.Parser.programScope, line, col);
            if (curCS == null)
                curCS = Parser.Parser.programScope;

            Dictionary<string, Parser.Parser.VarDecl> vars = new Dictionary<string,Babel.Parser.Parser.VarDecl>();
            HLSLScopeUtils.GetVarDecls(curCS, vars);

            //Add the variables to the list
            foreach (KeyValuePair<string, Parser.Parser.VarDecl> kv in vars)
            {
                if (currentText == string.Empty || kv.Key.StartsWith(currentText, StringComparison.CurrentCultureIgnoreCase))
                    declarations.Add(kv.Value.varDeclaration);
            }

            //  Add struct declarations
            foreach (HLSLDeclaration d in Parser.Parser.structDecls)
            {
                if (currentText == string.Empty || d.Name.StartsWith(currentText))
                {
                    declarations.Add(d);
                }
            }

            //  Add type definitions
            foreach (HLSLDeclaration d in Parser.Parser.typedefTypes)
            {
                if (currentText == string.Empty || d.Name.StartsWith(currentText))
                {
                    declarations.Add(d);
                }
            }
           
            //  Add function declarations
            foreach (HLSLFunction method in Parser.Parser.methods)
            {
                if (currentText == string.Empty || method.Name.StartsWith(currentText, true, null))
                {   
                    declarations.Add(methodToDeclaration(method));
                }
            }

            //  Sort the declarations
            declarationComparer dc = new declarationComparer();
            declarations.Sort(dc);

            return declarations;
        }

        //a comparer for comparing the names in two declarations
        private class declarationComparer : IComparer<HLSLDeclaration>
        {
            public int Compare(HLSLDeclaration a, HLSLDeclaration b)
            {
                return a.Name.CompareTo(b.Name);
            }

        }

        public void FindMembersWrapper(string tempDir, int line, int col)
        {
            string oldDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            FindMembers(null, line, col);

            Directory.SetCurrentDirectory(oldDir);
        }

        //Gets a list of member variables of the struct variable that came before the dot 
        //at line 'line' and column 'col'
		public IList<Babel.HLSLDeclaration> FindMembers(object result, int line, int col)
		{
            string TestFileOutputName = "MemberCompleteOutput.txt";
            FileStream fw = new FileStream(TestFileOutputName, FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fw);

			// ManagedMyC.Parser.AAST aast = result as ManagedMyC.Parser.AAST;
			List<Babel.HLSLDeclaration> members = new List<Babel.HLSLDeclaration>();

            //string currentText = result.ToString();

            //foreach (string state in aast.startStates.Keys)
            //    members.Add(new Declaration(state, state, 0, state));
            KeyValuePair<TextSpan, Parser.LexValue> var = new KeyValuePair<TextSpan,Babel.Parser.LexValue>( new TextSpan(), new Babel.Parser.LexValue());
            foreach (KeyValuePair<TextSpan, Parser.LexValue> kv in Parser.Parser.structVars)
            {
                if( TextSpanHelper.IsAfterEndOf(kv.Key, line, col) && TextSpanHelper.EndsAfterEndOf(kv.Key, var.Key) )
                    var = kv;
            }
            string token = var.Value.str;

            string varType = null;
            if( token != null )
            {
                Dictionary<string, Parser.Parser.VarDecl> vars = new Dictionary<string, Babel.Parser.Parser.VarDecl>();
                Parser.Parser.CodeScope curCS = HLSLScopeUtils.GetCurrentScope(Parser.Parser.programScope, line, col);
                if (curCS == null)
                    curCS = Parser.Parser.programScope;
                HLSLScopeUtils.GetVarDecls(curCS, vars);
                foreach (KeyValuePair<string, Parser.Parser.VarDecl> kv in vars)
                {
                    if (kv.Key.Equals(token))
                    {
                        varType = kv.Value.varDeclaration.Description;
                        break;
                    }
                }
            }

            if (varType != null)
            {
                Parser.Parser.StructMembers sm;
                if (Parser.Parser.structMembers.TryGetValue(varType, out sm))
                    members.AddRange(sm.structMembers);
            }

            foreach(HLSLDeclaration h in members)
            {
                sw.WriteLine(h.DisplayText);
            }

            sw.Close();
			return members;
		}

        //find guick info  right now returns nothing (unimplemented)
		public string FindQuickInfo(object result, int line, int col)
		{
			return "unknown";
		}

        //Find the method with the given name at the location
		public IList<Babel.HLSLFunction> FindMethods(object result, int line, int col, string name)
		{
            IList<Babel.HLSLFunction> matchingMethods = new List<Babel.HLSLFunction>();
            foreach (HLSLFunction method in Parser.Parser.methods)
            {
                if (method.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)) 
                {
                    matchingMethods.Add(method);
                }
            }
			return matchingMethods;
		}

        //convert a method to a declaration
        public HLSLDeclaration methodToDeclaration(Babel.HLSLFunction method)
        {
            HLSLDeclaration dec = new HLSLDeclaration();
            dec.Name = method.Name;
            dec.Glyph = Parser.Parser.GLYPH_TYPE_FUNCTION;
            dec.DisplayText = method.Name;
            dec.Description = method.ToString();
            return dec;
        }

		#endregion
	}
}