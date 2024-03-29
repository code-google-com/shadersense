/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;

namespace Babel
{   /* HLSLDeclaration
     * A struct storing all the important values for a Declaration.
     * This includes description, display text, glyph, and a name.
     */
	public struct HLSLDeclaration
	{
		public HLSLDeclaration(string description, string displayText, int glyph, string name)
		{
			this.Description = description;
			this.DisplayText = displayText;
			this.Glyph = glyph;
			this.Name = name;
		}

		public string Description;
		public string DisplayText;
		public int Glyph;
		public string Name;
	}
}