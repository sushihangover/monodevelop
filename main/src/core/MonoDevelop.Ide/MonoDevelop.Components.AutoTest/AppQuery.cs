﻿//
// AppQuery.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Text;
using Gtk;
using MonoDevelop.Components.AutoTest.Operations;
using MonoDevelop.Components.AutoTest.Results;

namespace MonoDevelop.Components.AutoTest
{
	public class AppQuery : MarshalByRefObject
	{
		AppResult rootNode;
		List<Operation> operations = new List<Operation> ();

		public AutoTestSessionDebug SessionDebug { get; set; }

		AppResult GenerateChildrenForContainer (Gtk.Container container, List<AppResult> resultSet)
		{
			AppResult firstChild = null, lastChild = null;

			foreach (var child in container.Children) {
				AppResult node = new GtkWidgetResult (child);
				resultSet.Add (node);

				// FIXME: Do we need to recreate the tree structure of the AppResults?
				if (firstChild == null) {
					firstChild = node;
					lastChild = node;
				} else {
					lastChild.NextSibling = node;
					node.PreviousSibling = lastChild;
					lastChild = node;
				}

				if (child is Gtk.Container) {
					AppResult children = GenerateChildrenForContainer ((Gtk.Container)child, resultSet);
					node.FirstChild = children;
				}
			}

			return firstChild;
		}

		List<AppResult> ResultSetFromWindowList (Gtk.Window[] windows)
		{
			// null for AppResult signifies root node
			rootNode = new GtkWidgetResult (null);
			List<AppResult> fullResultSet = new List<AppResult> ();

			// Build the tree and full result set recursively
			AppResult lastChild = null;
			foreach (var window in windows) {
				AppResult node = new GtkWidgetResult (window);
				fullResultSet.Add (node);

				if (rootNode.FirstChild == null) {
					rootNode.FirstChild = node;
					lastChild = node;
				} else {
					// Add the new node into the chain
					lastChild.NextSibling = node;
					node.PreviousSibling = lastChild;
					lastChild = node;
				}

				// Create the children list and link them onto the node
				AppResult children = GenerateChildrenForContainer ((Gtk.Container) window, fullResultSet);
				node.FirstChild = children;
			}

			return fullResultSet;
		}

		public AppQuery ()
		{
		}

		public AppResult[] Execute ()
		{
			List<AppResult> resultSet = ResultSetFromWindowList (Gtk.Window.ListToplevels ());
			foreach (var subquery in operations) {
				// Some subqueries can select different results
				resultSet = subquery.Execute (resultSet);

				if (resultSet.Count == 0) {
					break;
				}
			}

			AppResult[] results = new AppResult[resultSet.Count];
			resultSet.CopyTo (results);

			return results;
		}

		public AppQuery Marked (string mark)
		{
			operations.Add (new MarkedOperation (mark));
			return this;
		}

		public AppQuery CheckType (Type desiredType)
		{
			operations.Add (new TypeOperation (desiredType));
			return this;
		}

		public AppQuery Button ()
		{
			return CheckType (typeof(Button));
		}

		public AppQuery Textfield ()
		{
			return CheckType (typeof(Entry));
		}

		public AppQuery CheckButton ()
		{
			return CheckType (typeof(CheckButton));
		}

		public AppQuery RadioButton ()
		{
			return CheckType (typeof(RadioButton));
		}

		public AppQuery TreeView ()
		{
			return CheckType (typeof(TreeView));
		}

		public AppQuery Window ()
		{
			return CheckType (typeof(Window));
		}

		public AppQuery Text (string text)
		{
			operations.Add (new TextOperation (text));
			return this;
		}

		public AppQuery Contains (string text)
		{
			operations.Add (new TextOperation (text, false));
			return this;
		}

		public AppQuery Model (string column)
		{
			operations.Add (new ModelOperation (column));
			return this;
		}

		public AppQuery Sensitivity (bool sensitivity)
		{
			operations.Add (new PropertyOperation ("Sensitive", sensitivity));
			return this;
		}

		public AppQuery Visibility (bool visibility)
		{
			operations.Add (new PropertyOperation ("Visible", visibility));
			return this;
		}

		public AppQuery Property (string propertyName, object desiredValue)
		{
			operations.Add (new PropertyOperation (propertyName, desiredValue));
			return this;
		}

		public AppQuery Toggled (bool toggled)
		{
			operations.Add (new PropertyOperation ("Active", toggled));
			return this;
		}

		public AppQuery NextSiblings ()
		{
			operations.Add (new NextSiblingsOperation ());
			return this;
		}

		public AppQuery Index (int index)
		{
			operations.Add (new IndexOperation (index));
			return this;
		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder ();
			foreach (var subquery in operations) {
				builder.Append (subquery.ToString ());
			}

			return builder.ToString ();
		}		
	}
}
